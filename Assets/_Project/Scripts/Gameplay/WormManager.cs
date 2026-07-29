using Photon.Pun;
using Steamworks;
using UnityEngine;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Gameplay
{
public class WormManager : MonoBehaviourPunCallbacks
{

    public Animator animator;

    [SerializeField] private bool isHookWorm = false;

    [SerializeField] private PolygonCollider2D polygonCollider2D;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isHookWorm)
        {
            if (GS.Instance.isLan && GameManager.Instance.fisherManIsSpawned)
            {
                Debug.Log("hook is generated");
                //transform.localScale = Vector3.zero;
                GameManager.Instance.myFish.fishController_Mirror.allHookWorms.Add(this);
            }
        }
    }

    public void OnDanceAnimation()
    {
        animator.SetBool("isDance", true);
    }
}

}