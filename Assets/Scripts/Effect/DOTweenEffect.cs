using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DOTweenEffect : MonoBehaviour
{
    [SerializeField] RectTransform _parent;

    [SerializeField] int _count = 10;
    [SerializeField] float _radius = 50, _dur = 1f;


    public void Play(Vector2 center, Image sparklePrefabOverride)
    {
        if (_parent == null) return;

        for (int i = 0; i < _count; i++)
        {
            var sp = Instantiate(sparklePrefabOverride, _parent);

            var tmp = sp.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.fontSize = tmp.fontSize / 4;
            }

            sp.color = new Color(1, 1, 1, 1);
            var rt = sp.rectTransform;
            rt.anchoredPosition = center;
            rt.localScale = Vector3.one * Random.Range(0.6f, 1.2f);

            var dir = Random.insideUnitCircle.normalized;
            var target = center + dir * Random.Range(_radius * 0.5f, _radius);

            var seq = DOTween.Sequence();
            seq.Join(rt.DOAnchorPos(target, _dur).SetEase(Ease.OutCubic));
            seq.Join(sp.DOFade(0f, _dur).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(sp.gameObject));
        }
    }
}