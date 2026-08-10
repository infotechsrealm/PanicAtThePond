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
            // Counter the parent's mirror so the text stays readable, and mirror the
            // offset back so it keeps sitting on the same side of the fish.
            label.localScale = new Vector3(labelBaseScale.x * sign, labelBaseScale.y, labelBaseScale.z);
            label.anchoredPosition = new Vector2(labelBasePos.x * sign, labelBasePos.y);
        }
    }

    void Update()
    {
        if (rect == null || leftPoint == null || rightPoint == null)
        {
            return;
        }

        if (movingRight)
        {
            rect.anchoredPosition += Vector2.right * speed * Time.deltaTime;
            if (rect.anchoredPosition.x >= rightPoint.anchoredPosition.x)
            {
                movingRight = false;
                SetFacing(1f);
            }
        }
        else
        {
            rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;
            if (rect.anchoredPosition.x <= leftPoint.anchoredPosition.x)
            {
                movingRight = true;
                SetFacing(-1f);
            }
        }
    }

}

}