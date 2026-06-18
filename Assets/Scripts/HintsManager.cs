using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HintsManager : MonoBehaviour
{
    private const float MediumTextScale = 1.35f;
    private const float MediumTextPulseScale = 0.88f;
    private const float PulseDownDuration = 0.75f;
    private const float PulseUpDuration = 0.5f;

    public Button backButton;

    public Transform scaledObject, scaledObject2,goldfish;
    public CanvasGroup fadeCanvasGroup;


    public Transform fish;
    public Transform text;
    public Transform fishAndText;

    void Start()
    {
        ApplyMediumScale(scaledObject);
        ApplyMediumScale(scaledObject2);
        ApplyMediumScale(fadeCanvasGroup != null ? fadeCanvasGroup.transform : null);

        ScaledAnimation(scaledObject);
        ScaledAnimation(scaledObject2);
        FadeAnimation(fadeCanvasGroup);
        MoveLoop(goldfish);

        AnimateFish();
        AnimateText();
        AnimateFloat(fishAndText);
        backButton.onClick.AddListener(OnBackPressed);
    }
    private void OnEnable()
    {
        BackManager.EnsureInstance().RegisterScreen(backButton);
    }
    private void OnBackPressed()
    {
        BackManager.EnsureInstance().UnregisterScreen();
        gameObject.SetActive(false);
    }

    private void ApplyMediumScale(Transform target)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = Vector3.one * MediumTextScale;
    }

    public void ScaledAnimation(Transform transform)
    {
        if (transform == null)
        {
            return;
        }

        Sequence seq = DOTween.Sequence();
        Vector3 baseScale = transform.localScale;

        seq.Append(transform.DOScale(baseScale * MediumTextPulseScale, PulseDownDuration).SetEase(Ease.OutQuad));

        seq.Append(transform.DOScale(baseScale, PulseUpDuration).SetEase(Ease.OutQuad));

        seq.SetLoops(-1, LoopType.Restart);
    }

    public void FadeAnimation(CanvasGroup cg)
    {
        if (cg == null)
        {
            return;
        }

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
        if (obj == null)
        {
            return;
        }

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
        if (fish == null)
        {
            return;
        }

        Sequence s = DOTween.Sequence();

        s.Append(fish.DORotate(new Vector3(0, 0, 5), 0.15f).SetEase(Ease.InOutSine));
        s.Append(fish.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));
        s.Append(fish.DORotate(new Vector3(0, 0, -5), 0.15f).SetEase(Ease.InOutSine));
        s.Append(fish.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));

        s.SetLoops(-1, LoopType.Restart);
    }

    void AnimateText()
    {
        if (text == null)
        {
            return;
        }

        Sequence s = DOTween.Sequence();

        s.Append(text.DORotate(new Vector3(0, 0, 7.5f), 0.25f).SetEase(Ease.InOutSine));
        s.Append(text.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));
        s.Append(text.DORotate(new Vector3(0, 0, -7.5f), 0.25f).SetEase(Ease.InOutSine));
        s.Append(text.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutSine));

        s.SetLoops(-1, LoopType.Restart);
    }

    void AnimateFloat(Transform target)
    {
        if (target == null)
        {
            return;
        }

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
}
