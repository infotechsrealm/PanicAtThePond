using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class HintsManager : MonoBehaviour
{
    public Button backButton;

    public Transform scaledObject;

    void Start()
    {
        if (scaledObject == null)
            scaledObject = transform;

        scaledObject.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // 1 → 0.75 in 0.75s
        seq.Append(scaledObject.DOScale(0.75f, 0.75f).SetEase(Ease.OutQuad));

        // 0.75 → 1 in 0.5s
        seq.Append(scaledObject.DOScale(1f, 0.5f).SetEase(Ease.OutQuad));

        // Loop forever
        seq.SetLoops(-1, LoopType.Restart);
    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }

    public void Close()
    {
        BackManager.instance.UnregisterScreen();
        Destroy(gameObject);
    }
}