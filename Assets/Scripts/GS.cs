using UnityEngine;

public class GS : MonoBehaviour  
{
    public static GS Instance;
    public GameObject createAndJoinPanel,
                      howToPlay,
                      preloder;


    [SerializeField]
    public GameObject passwordPopupPrefab; // Assign in Inspector

    public bool AllVisible;
    public bool DeepWaters;
    public bool MurkyWaters;
    public bool ClearWaters;

    public string nickName = "";

    internal bool isMasterClient;

    public bool isLan = false,IsMirrorMasterClient = false;
    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        nickName = "Player_" + Random.Range(100, 999);
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

}