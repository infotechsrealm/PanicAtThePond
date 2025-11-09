using UnityEngine;

public class GS : MonoBehaviour  
{
    public static GS Instance;
    public GameObject createAndJoinPanel,
                      howToPlay,
                      preloder;

    public bool AllVisible;
    public bool DeepWaters;
    public bool MurkyWaters;
    public bool ClearWaters;

    public string nickName = "";

    internal bool isMasterClient;

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
        if (Preloader.instance == null)
        {
            Debug.Log("Generating Preloader -------------");
            Instantiate(preloder, transform);
        }
    }

    public void DestroyPreloder()
    {
        if (Preloader.instance != null)
        {
            Debug.Log("Destroying Preloader ==============");
            Destroy(Preloader.instance.gameObject);
        }
    }

}