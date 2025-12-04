using Mirror;
using Mirror.Discovery;
using System;
using System.Threading;
using UnityEngine;

public static class UnityThread
{
    public static SynchronizationContext MainThread;
}

public class GS : MonoBehaviour  
{
    public event Action OnVisibilityChanged;

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

        GS.Instance.OnVisibilityChanged += rerfeshDropDown;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            // Toggle Fullscreen
            if (Screen.fullScreen)
            {
                // Go to Windowed Mode
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
            }
            else
            {
                // Go to Borderless Fullscreen (BEST just like games)
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
            }
        }
    }
    public void SetVolume(AudioSource audioSource)
    {
        float GlobleVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (audioSource.volume != GlobleVolume)
        {
            audioSource.volume = GlobleVolume;
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
        Debug.Log("Trying to Destroy Preloader ............");
        if (Preloader.Instence != null)
        {
            Debug.Log("Destroying Preloader ==============");
            Destroy(Preloader.Instence.gameObject);
        }
        else
        {
            Debug.Log("No Preloader found to destroy.");
        }
    }

    public void rerfeshDropDown()
    {
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
        if (dropdownHandler != null)
        {
          
            dropdownHandler.OnDropdownChanged(index);
            dropdownHandler.waterDropdown.value = index;   // dropdown option index set karega
            dropdownHandler.waterDropdown.RefreshShownValue();   // UI ko update karega
        }
    }

}