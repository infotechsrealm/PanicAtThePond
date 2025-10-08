using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashManager : MonoBehaviour
{
    public Slider progressSlider; // Assign your UI Slider in Inspector
    private float fillDuration;   // Random time between 2 to 5 seconds
    private float timer = 0f;

    void Start()
    {
        fillDuration = Random.Range(2f, 5f); // Random between 2 and 5
        progressSlider.value = 0f;
    }

    void Update()
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
