using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Managers
{
public class BackManager : MonoBehaviour
{
    public static BackManager instance;

    [SerializeField]
    private List<Button> backList = new List<Button>();

    [SerializeField]
    public Stack<Button> backStack = new Stack<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnSceneLoad()
    {
        // This attribute fires ONCE per play session. The GameObject created below lives in whatever
        // scene is active at the time (Splash) and is deliberately not DontDestroyOnLoad, so it is
        // destroyed the moment that scene unloads. Without the hook here, `instance` stayed a dead
        // reference for the whole of the next scene, and the 15 sites that dereference
        // `BackManager.instance` directly (e.g. DashManager's "Play" case) threw a
        // NullReferenceException until some panel's OnEnable happened to call EnsureInstance().
        // Re-running per scene load keeps the existing "fresh back-stack per scene" behaviour.
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
    }

    public static BackManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        BackManager existing = FindFirstObjectByType<BackManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject managerObject = new GameObject(nameof(BackManager));
        return managerObject.AddComponent<BackManager>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Preloader.Instence == null)
            {
                CallBack(); 
            }
            else
            {
                if (!Preloader.Instence.gameObject.activeSelf)
                {
                    CallBack();
                }
            }
        }
    }

    // -------- Add a back button to stack -----------
    public void RegisterScreen(Button backButton)
    {
        if (backButton == null)
        {
            Debug.LogWarning("BackManager.RegisterScreen called with a missing back button.");
            return;
        }

        backStack.Push(backButton);
        backList.Add(backButton);
    }

    // -------- Remove top screen -----------
    public void UnregisterScreen()
    {
        if (backStack.Count > 0)
        {
            backStack.Pop();
            backList.RemoveAt(backList.Count - 1);
        }
    }

    // -------- ESC → call top back button ----------
    public void CallBack()
    {
        if (backStack.Count > 0)
        {
            Button top = backStack.Peek();
            top.onClick.Invoke();   // actual button click
        }
        else
        {
            if(InGameMenu.Instance != null)
            {
                InGameMenu.Instance.SettingEnable();
            }
        }
    }
}

}