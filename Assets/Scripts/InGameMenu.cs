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
    public GameObject menuPanel;       // Reference to the whole pause menu UI
    private bool isMenuOpen = false;

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
    }

    public void CallenableObjects()
    {
        StartCoroutine(enableObjects());

    }

    public  IEnumerator enableObjects()
    {
        Debug.Log("called");
        yield return new WaitForSeconds(1f);
        Debug.Log("called1");

        for (int i = 0; i < Objects.Count; i++)
        {
            Debug.Log("called2");
            if (!Objects[i].activeSelf)
            {
                Objects[i].SetActive(true);
            }
        }
        preloder.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);
        if(GameManager.Instance.myFish!=null)
        {
            GameManager.Instance.myFish.CallGamePauseRPC(isMenuOpen);
        }
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
    
    public void OnBackPressed()
    {
        ToggleMenu();
    }
}
