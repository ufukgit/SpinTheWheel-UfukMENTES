using System.Threading.Tasks;
using System;
using UnityEngine.Networking;
using UnityEngine;

public sealed class RandomNumberApiProvider : IRandomIndexProvider
{
    const string Url = "http://www.randomnumberapi.com/api/v1.0/random?min=0&max={0}&count=1";
    readonly int _timeoutSec;
    readonly int _retries;

    public RandomNumberApiProvider(int timeoutSec = 2, int retries = 1)
    {
        _timeoutSec = Mathf.Max(1, timeoutSec);
        _retries = Mathf.Max(0, retries);
    }

    public async Task<int> GetIndexAsync(int max)
    {
        max = Mathf.Max(1, max);
        string url = string.Format(Url, max);

        for (int attempt = 0; attempt <= _retries; attempt++)
        {
            try
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    req.timeout = _timeoutSec;
                    var op = req.SendWebRequest();
                    while (!op.isDone) await Task.Yield();
#if UNITY_2020_2_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
                        throw new Exception(req.error);
#else
                    if (req.isNetworkError || req.isHttpError)
                        throw new Exception(req.error);
#endif
                    var body = req.downloadHandler.text;
                    var idx = ParseIndex(body, max);
                    return idx;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Random API attempt {attempt + 1} failed: {e.Message}");
                await Task.Delay(150);
            }
        }

        return UnityEngine.Random.Range(0, max);
    }

    static int ParseIndex(string s, int maxExclusive)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsDigit(s[i]))
            {
                int j = i;
                while (j < s.Length && char.IsDigit(s[j])) j++;
                if (int.TryParse(s.Substring(i, j - i), out var v))
                    return Mathf.Clamp(v, 0, Mathf.Max(0, maxExclusive - 1));
                break;
            }
        }
        return 0;
    }
}