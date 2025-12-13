using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenu : MonoBehaviourPunCallbacks
{
    public static InGameMenu Instance;

    public GameObject settingUI;       // Reference to the whole pause menu UI

    public List<GameObject> Objects = new List<GameObject>();
    public GameObject preloder;


    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        CallenableObjects();
    }

    public void CallenableObjects()
    {
        StartCoroutine(enableObjects());

    }

    public  IEnumerator enableObjects()
    {
        yield return new WaitForSeconds(2f);
        for (int i = 0; i < Objects.Count; i++)
        {
            if (!Objects[i].activeSelf)
            {
                Objects[i].SetActive(true);
            }
        }
        preloder.SetActive(false);
    }

    public void QuitToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Dash");
    }

    public void SettingEnable()
    {
        settingUI.SetActive(true);
        if (GS.Instance.isLan)
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.myFish != null)
                {
                    GameManager.Instance.myFish.fishController_Mirror.CallGamePause(true);
                }
            }
        }
        else
        {
            if (GameManager.Instance.myFish != null)
            {
                GameManager.Instance.myFish.CallGamePauseRPC(true);
            }
        }
    }
}
