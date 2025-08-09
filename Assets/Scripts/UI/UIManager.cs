using Unity.VisualScripting;
using UnityEditor.VersionControl;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject _panelGo;
    [SerializeField] TMPro.TMP_Text _warningText;

    private void Start()
    {
        if (FirebaseServices.Instance != null)
            FirebaseServices.Instance.OnlineStateChanged += HandleState;
    }

    void OnDestroy()
    {
        if (FirebaseServices.Instance != null)
            FirebaseServices.Instance.OnlineStateChanged -= HandleState;
    }

    void HandleState(OnlineState state, string msg)
    {
        bool show = state == OnlineState.Offline || state == OnlineState.Error;
        _panelGo.SetActive(show);
        _warningText.text = show ? (msg ?? "Something went wrong.") : "";
    }
}