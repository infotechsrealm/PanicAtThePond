using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class FixUIEditor
{
    static FixUIEditor()
    {
        EditorApplication.delayCall += FixResetButton;
    }

    static void FixResetButton()
    {
        if (EditorPrefs.GetBool("FixResetButtonPosDone3", false)) return;
        
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Dash")
        {
            try {
                EditorSceneManager.OpenScene("Assets/Scenes/Dash.unity");
            } catch { return; }
        }

        bool changed = false;
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.scene.name == "Dash" && btn.name.ToLower().Contains("reset"))
            {
                if (btn.transform.parent != null && btn.transform.parent.name.ToLower().Contains("score"))
                {
                    RectTransform rect = btn.GetComponent<RectTransform>();
                    rect.localScale = new Vector3(4, 4, 4);
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    rect.anchoredPosition = new Vector2(-190, -105);
                    rect.sizeDelta = new Vector2(70, 20);
                    
                    EditorUtility.SetDirty(btn);
                    changed = true;
                    Debug.Log("Reset button moved permanently via Editor script!");
                }
            }
        }
        
        if (changed)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        
        EditorPrefs.SetBool("FixResetButtonPosDone3", true);
    }
}
