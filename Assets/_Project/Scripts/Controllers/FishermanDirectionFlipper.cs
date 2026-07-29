using UnityEngine;

using PanicAtThePond.Managers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Controllers
{
public class FishermanDirectionFlipper : MonoBehaviour
{
    private FishermanController fishermanController;
    private Transform headTransform;
    private Vector3 initialScale;

    void Start()
    {
        fishermanController = GetComponent<FishermanController>();
        if (fishermanController == null)
        {
            fishermanController = GetComponentInParent<FishermanController>();
        }

        headTransform = transform.Find("Head");
        if (headTransform == null)
        {
            headTransform = transform.Find("head");
        }
        
        if (headTransform != null)
        {
            initialScale = headTransform.localScale;
        }
    }

    void Update()
    {
        // Direction and flipping are handled automatically by the animator states ("Left" / "Right")
        // and synchronized child animation clips. Manual scale flipping is no longer needed.
    }
}

}