using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.UI
{
public class Preloader : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 200f;
    [SerializeField] private RectTransform leftPoint;
    public RectTransform rightPoint;

    private bool movingRight = false;
    public RectTransform rect;

    // The "Loading..." label is a child of rect, so flipping rect to face the travel
    // direction would mirror the glyphs and swing the label to the fish's other side.
    // Keep a handle on it so the flip can be undone for the label only.
    [SerializeField] private RectTransform label;
    private Vector2 labelBasePos;
    private Vector3 labelBaseScale = Vector3.one;
    private float labelBaseAnchorMinX;
    private float labelBaseAnchorMaxX;

    public static Preloader Instence;
    private void Awake()
    {
        Instence = this;

        if (label == null && rect != null)
        {
            Text text = rect.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                label = text.rectTransform;
            }
        }

        if (label != null)
        {
            labelBasePos = label.anchoredPosition;
            labelBaseScale = label.localScale;
            labelBaseAnchorMinX = label.anchorMin.x;
            labelBaseAnchorMaxX = label.anchorMax.x;
        }

        SetFacing(1f);
    }

    private void SetFacing(float sign)
    {
        if (rect != null)
        {
            rect.localScale = new Vector3(sign, 1f, 1f);
        }

        if (label != null)
        {
            // Counter the parent's mirror so the text stays readable.
            label.localScale = new Vector3(labelBaseScale.x * sign, labelBaseScale.y, labelBaseScale.z);

            // The label's horizontal placement has to be mirrored back as well, or it swings to the
            // fish's other side. It can come from either the anchor or the anchored position and the
            // two disagree between the prefab and the Play scene instance: the prefab centres the
            // label on 0.5 and offsets it by 195, while the scene override anchors it at 1.8 with a
            // zero offset. Mirroring the anchor about 0.5 and negating the offset covers both.
            bool flipped = sign < 0f;
            float minX = flipped ? 1f - labelBaseAnchorMinX : labelBaseAnchorMinX;
            float maxX = flipped ? 1f - labelBaseAnchorMaxX : labelBaseAnchorMaxX;
            label.anchorMin = new Vector2(Mathf.Min(minX, maxX), label.anchorMin.y);
            label.anchorMax = new Vector2(Mathf.Max(minX, maxX), label.anchorMax.y);
            label.anchoredPosition = new Vector2(labelBasePos.x * sign, labelBasePos.y);
        }
    }

    // The two end markers do not always express their position the same way as the fish does.
    // The PreloderUI prefab puts all three on a 0.5 anchor and offsets them by anchoredPosition
    // (-512 / +512), so comparing anchoredPosition works. The Play scene instance instead anchors
    // them to the canvas edges (-0.054 and 1.054) and leaves anchoredPosition at zero, so both
    // markers report an anchoredPosition.x of 0. Comparing that made the fish reach "the end"
    // on the very first frame in both directions, so it stood still and flipped every frame.
    // localPosition is resolved from anchor + offset, so it is correct for either authoring style.
    private static float TrackX(RectTransform t)
    {
        return t.localPosition.x;
    }

    void Update()
    {
        if (rect == null || leftPoint == null || rightPoint == null)
        {
            return;
        }

        float leftX = TrackX(leftPoint);
        float rightX = TrackX(rightPoint);

        // A zero-or-inverted span cannot be travelled; flipping on it is the fast-spin bug.
        if (rightX - leftX < 1f)
        {
            return;
        }

        if (movingRight)
        {
            rect.anchoredPosition += Vector2.right * speed * Time.deltaTime;
            if (TrackX(rect) >= rightX)
            {
                movingRight = false;
                SetFacing(1f);
            }
        }
        else
        {
            rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;
            if (TrackX(rect) <= leftX)
            {
                movingRight = true;
                SetFacing(-1f);
            }
        }
    }

}

}