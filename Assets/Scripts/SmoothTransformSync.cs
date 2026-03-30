using UnityEngine;
using Photon.Pun;

public class SmoothTransformSync : MonoBehaviourPun, IPunObservable
{
    [Header("Smoothing Settings")]
    public float lerpSpeed = 10f;   // Smoothness factor
    public bool syncRotation = true;

    private Vector3 networkPos;

    private void Awake()
    {
        networkPos = transform.position;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            // Smooth position
            transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * lerpSpeed);

            // Smooth rotation (optional)
        }
    }

    // Send & Receive position/rotation data
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Apna data bhejna
            stream.SendNext(transform.position);
            if (syncRotation)
                stream.SendNext(transform.rotation);
        }
        else
        {
            // Dusre players ka data lena
            networkPos = (Vector3)stream.ReceiveNext();
        }
    }
}
