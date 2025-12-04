using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
public class SplashManager : MonoBehaviour
{

    public RectTransform logo;
    public RectTransform nameText;

    public CanvasGroup logoCG;
    public CanvasGroup nameCG;

    public CanvasGroup fadeLogo;

    public float startX = -800f;
    public float endX = 0f;

    public float duration = 1f;
    public float nameDelay = 0.25f;

    void Start()
    {
        // PlayAnimation();
     /*   fadeLogo.transform.DOScale(1f, 15f);
        fadeLogo.DOFade(1f, 1f).OnComplete(() =>
        {
             fadeLogo.DOFade(0f, 1.5f).SetDelay(2f).OnComplete(() =>
             {
             });
        });*/
                 PlayAnimation();
    }

    void PlayAnimation()
    {
        // Reset position & alpha
        logo.anchoredPosition = new Vector2(startX, logo.anchoredPosition.y);
        nameText.anchoredPosition = new Vector2(startX, nameText.anchoredPosition.y);

        logoCG.alpha = 0;
        nameCG.alpha = 0;

        Sequence seq = DOTween.Sequence();

        // 1️⃣ Logo move + fade (NO delay)
        seq.Join(logo.DOAnchorPosX(endX, duration).SetEase(Ease.OutQuad));
        seq.Join(logoCG.DOFade(1f, duration));

        // 2️⃣ Name move + fade (start after 0.25 sec)
        seq.Join(
            nameText.DOAnchorPosX(endX, duration)
            .SetEase(Ease.OutQuad)
            .SetDelay(nameDelay)
        );

        seq.Join(
            nameCG.DOFade(1f, duration)
            .SetDelay(nameDelay)
        );

        // 3️⃣ Load scene on complete
        seq.OnComplete(() =>
        {
            SceneManager.LoadScene("Dash");
        });
    }
}