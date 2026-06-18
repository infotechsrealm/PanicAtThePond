using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
