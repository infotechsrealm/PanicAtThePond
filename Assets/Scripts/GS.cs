using Mirror;
using Mirror.Discovery;
using System.Threading;
using UnityEngine;

public static class UnityThread
{
    public static SynchronizationContext MainThread;
}

public class GS : MonoBehaviour  
{

    public static GS Instance;

    public NetworkDiscovery networkDiscovery;
    public NetworkManager networkManager;

    public GameObject createAndJoinPanel,
                      howToPlay,
                      preloder;


    [SerializeField]
    public GameObject passwordPopupPrefab; // Assign in Inspector

    public bool ClearWaters;
    public bool MurkyWaters;
    public bool DeepWaters;
    public bool ReflectiveWater;

    public bool isFullscreen;

    public bool dropDownChangeAvalable = false;

    public string nickName = "";

    internal bool isMasterClient;

    internal int totlePlayers;

    public bool isLan = false,IsMirrorMasterClient = false;


    public AudioSource BGMusic;
    private void Awake()
    {
        Instance = this;
        UnityThread.MainThread = SynchronizationContext.Current;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        nickName = "Player_" + UnityEngine.Random.Range(100, 999);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            // Toggle Fullscreen
            ChangeScreenMode();
        }
    }

    public void ChangeScreenMode()
    {
        if (Screen.fullScreen)
        {
            // Go to Windowed Mode
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
            isFullscreen = false;
        }
        else
        {
            // Go to Borderless Fullscreen (BEST just like games)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
            isFullscreen = true;
        }
    }

    public void SetSFXVolume(AudioSource audioSource)
    {
        float Volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (audioSource.volume != Volume)
        {
            audioSource.volume = Volume;
        }
    }

    public void SetMusicVolume()
    {
        float Volume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (BGMusic.volume != Volume)
        {
            BGMusic.volume = Volume;
        }
    }

    public void GeneratePreloder(Transform transform)
    {
        if (Preloader.Instence == null)
        {
            Debug.Log("Generating Preloader -------------");
            Instantiate(preloder, transform);
        }
    }

    public void DestroyPreloder()
    {
      //  Debug.Log("Trying to Destroy Preloader ............");
        if (Preloader.Instence != null)
        {
           // Debug.Log("Destroying Preloader ==============");
            Destroy(Preloader.Instence.gameObject);
        }
        else
        {
           // Debug.Log("No Preloader found to destroy.");
        }
    }

    public void rerfeshDropDown()
    {
      dropDownChangeAvalable = false;
        int index = 0;
        if (ClearWaters)
        {
            index = 0;
        }
        else if (MurkyWaters)
        {
            index = 1;
        }
        else if (DeepWaters)
        {
            index = 2;
        }
        else if (ReflectiveWater)
        {
            index = 3;
        }

        DropdownHandler dropdownHandler = DropdownHandler.Instance;
        dropdownHandler.OnDropdownChanged(index);
        dropdownHandler.waterDropdown.value = index;   // dropdown option index set karega
        dropdownHandler.waterDropdown.RefreshShownValue();   // UI ko update karega
    }

}