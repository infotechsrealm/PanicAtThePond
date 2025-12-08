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

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(Preloader.Instence == null)
            {
                CallBack();
            }
        }
    }

    // -------- Add a back button to stack -----------
    public void RegisterScreen(Button backButton)
    {
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
