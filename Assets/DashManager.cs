using UnityEngine;


public class DashManager : MonoBehaviour
{
    public bool coustomCreate;
    public GameObject coustomButtons, randomButtons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (coustomCreate)
        {
            coustomButtons.SetActive(true);
            randomButtons.SetActive(false);
            PhotonLauncher.Instance.buttons = coustomButtons;
        }
        else
        {
            randomButtons.SetActive(true);
            coustomButtons.SetActive(false);
            PhotonLauncher.Instance.buttons = randomButtons;

        }
    }

   
}
