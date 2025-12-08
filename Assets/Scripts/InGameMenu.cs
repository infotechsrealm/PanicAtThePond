using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenu : MonoBehaviourPunCallbacks
{
    public static InGameMenu Instance;
    public GameObject settingUI;       // Reference to the whole pause menu UI

    private const string VolumeKey = "MusicVolume";

    public Slider musicVolumeSlider;


    public List<GameObject> Objects = new List<GameObject>();
    public GameObject preloder;


    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        musicVolumeSlider.value = savedVolume;

        // Hook up listener
        musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        CallenableObjects();
       // BackManager.instance.RegisterScreen(ToggleMenu());

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

    void Update()
    {
    }

    public void QuitToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Dash");
    }

    private void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SettingEnable()
    {
        settingUI.SetActive(true);
        if (GS.Instance.isLan)
        {

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
