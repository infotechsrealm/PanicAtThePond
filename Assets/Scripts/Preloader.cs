using Photon.Pun;
using UnityEngine;

public class Preloader : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 200f; // Rotation speed in degrees per second


    public static Preloader Instence;

    private void Awake()
    {
        Instence = this;
    }
   


}