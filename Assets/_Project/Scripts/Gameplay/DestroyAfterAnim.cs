using UnityEngine;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Gameplay
{
public class DestroyAfterAnim : MonoBehaviour
{
    public void DestroyAfterAnimation()
    {
        Destroy(gameObject);
    }
}
}