using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
public class HintsManager : MonoBehaviour
{
    public Button backButton;

    public Transform scaledObject, scaledObject2,goldfish;
    public CanvasGroup fadeCanvasGroup;


    public Transform fish;
    public Transform text;
    public Transform fishAndText;

    void Start()
    {
        ScaledAnimation(scaledObject);
        ScaledAnimation(scaledObject2);
        FadeAnimation(fadeCanvasGroup);
        MoveLoop(goldfish);

        AnimateFish();
        AnimateText();
        AnimateFloat(fishAndText);
    }

    public void ScaledAnimation(Transform transform)
    {
        transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // 1 → 0.75 in 0.75s
        seq.Append(transform.DOScale(0.75f, 0.75f).SetEase(Ease.OutQuad));

        // 0.75 → 1 in 0.5s
        seq.Append(transform.DOScale(1f, 0.5f).SetEase(Ease.OutQuad));

        // Loop forever
        seq.SetLoops(-1, LoopType.Restart);
    }

    public void FadeAnimation(CanvasGroup cg)
    {
        cg.alpha = 1f;

        Sequence seq = DOTween.Sequence();

        // Fade Out → alpha 1 → 0 in 0.75s
        seq.Append(cg.DOFade(0.25f, 0.5f).SetEase(Ease.OutQuad));

        // Fade In → alpha 0 → 1 in 0.5s
        seq.Append(cg.DOFade(1f, 0.5f).SetEase(Ease.OutQuad));

        // Infinite loop
        seq.SetLoops(-1, LoopType.Restart);
    }

    public void MoveLoop(Transform obj)
    {
        // Starting position = X = 600
        obj.localPosition = new Vector3(700f, obj.localPosition.y, obj.localPosition.z);

        Sequence seq = DOTween.Sequence();

        // Move 600 → -600
        seq.Append(obj.DOLocalMoveX(-700f, 10f).SetEase(Ease.Linear));

        // Reset instantly: -601 → 600
        seq.AppendCallback(() =>
        {
            obj.localPosition = new Vector3(700f, obj.localPosition.y, obj.localPosition.z);
        });

        // Infinite repeat
        seq.SetLoops(-1, LoopType.Restart);
    }

    void AnimateFish()
    {
        Sequence s = DOTween.Sequence();

        s.Append(fish.DORotate(new Vector3(0, 0, 5), 0.15f).SetEase(Ease.InOutSine));
        s.Append(fish.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));
        s.Append(fish.DORotate(new Vector3(0, 0, -5), 0.15f).SetEase(Ease.InOutSine));
        s.Append(fish.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));

        s.SetLoops(-1, LoopType.Restart);
    }

    void AnimateText()
    {
        Sequence s = DOTween.Sequence();

        s.Append(text.DORotate(new Vector3(0, 0, 7.5f), 0.25f).SetEase(Ease.InOutSine));
        s.Append(text.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));
        s.Append(text.DORotate(new Vector3(0, 0, -7.5f), 0.25f).SetEase(Ease.InOutSine));
        s.Append(text.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));

        s.SetLoops(-1, LoopType.Restart);
    }

    void AnimateFloat(Transform target)
    {
        Vector3 center = target.position;

        // Radius of the circle
        float r = 0.5f;  // jitna bada circle chahiye utna change karo

        target.position = center + new Vector3(r, 0, 0);
        // Create a circular path (8 points = smooth circle)
        Vector3[] path = new Vector3[]
        {
        center + new Vector3( r, 0, 0),
        center + new Vector3( 0, r, 0),
        center + new Vector3(-r, 0, 0),
        center + new Vector3( 0,-r, 0),
        center + new Vector3( r, 0, 0),
        };

        // Animate in circular loop
        target.DOPath(path, 2f, PathType.CatmullRom)
              .SetEase(Ease.Linear)
              .SetLoops(-1, LoopType.Restart);
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