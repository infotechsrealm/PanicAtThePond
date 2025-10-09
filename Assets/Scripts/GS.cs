using Unity.VisualScripting;
using UnityEngine;

public class GS : MonoBehaviour
{

    public static GS instance;
    public GameObject createAndJoinPanel,
                        howToPlay,
                      preloder;


    private void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
