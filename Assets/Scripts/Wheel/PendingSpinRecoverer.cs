using System.Collections;
using UnityEngine;

public class PendingSpinRecoverer : SingletonBehaviour<PendingSpinRecoverer>
{
    [SerializeField] bool _retryWhenOffline = true;

    public bool IsRecovering { get; private set; }
    public event System.Action RecoveryStarted;
    public event System.Action<bool> RecoveryFinished;

    IEnumerator Start()
    {
        yield return WaitForOnlineAndUser();

        if (!PendingSpinStore.TryLoad(out var p))
            yield break;

        var svc = FirebaseServices.Instance;
        if (svc == null || svc.SpinService == null || string.IsNullOrEmpty(svc.UserId))
            yield break;

        IsRecovering = true;
        RecoveryStarted?.Invoke();

        if (p.Stage == PendingStage.Created)
        {
            var task = svc.SpinService.GrantAsync(
                svc.UserId,
                "Money",
                p.AmountUnits,
                p.Index,
                p.SpinId
            );

            yield return CoroutineTasks.Wait(task);

            IsRecovering = false;
            RecoveryFinished?.Invoke(false);

            if (task.IsCompleted && !task.IsFaulted)
            {
                PendingSpinStore.Clear();
                yield break;
            }

            Debug.LogWarning("[Recover] Pending spin is at Created stage. Clearing.");
            PendingSpinStore.Clear();
            yield break;
        }

        if (p.Stage != PendingStage.Decided)
        {
            IsRecovering = false;
            RecoveryFinished?.Invoke(false);

            Debug.LogWarning("[Recover] Unknown pending stage. Clearing.");
            PendingSpinStore.Clear();
            yield break;
        }

        Debug.LogWarning("[Recover] Could not apply pending spin now. Will keep it for next session.");
    }

    IEnumerator WaitForOnlineAndUser()
    {
        while (FirebaseServices.Instance == null)
            yield return null;

        if (_retryWhenOffline)
        {
            while (!FirebaseServices.Instance.OnlineMode)
                yield return null;
        }

        while (string.IsNullOrEmpty(FirebaseServices.Instance.UserId))
            yield return null;
    }
}