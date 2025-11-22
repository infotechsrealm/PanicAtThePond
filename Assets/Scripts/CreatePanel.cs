using Photon.Pun;
using UnityEngine;

public class CreatePanel : MonoBehaviour
{
    public void Close()
    {
        if (GS.Instance.isLan)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
        gameObject.SetActive(false);
    }
}
