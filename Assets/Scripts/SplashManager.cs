using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashManager : MonoBehaviour
{
    public Slider progressSlider; // Assign your UI Slider in Inspector
    private float fillDuration;   // Random time between 2 to 5 seconds
    private float timer = 0f;

    public bool  istestMode = false;

    void Start()
    {
        if (istestMode)
        {
         
            SceneManager.LoadScene("Dash");
        }
        else
        {
            fillDuration = Random.Range(2f, 5f); // Random between 2 and 5
            progressSlider.value = 0f;
            // Directly load Dash scene in non-test mode
        }
      
    }

    void Update()
    {
        if (!istestMode)
        {
            if (progressSlider.value < 1f)
            {
                timer += Time.deltaTime;
                progressSlider.value = Mathf.Clamp01(timer / fillDuration);
            }
            else
            {
                // Once filled, load the Dash scene
                SceneManager.LoadScene("Dash");
            }
        }
    }
}
