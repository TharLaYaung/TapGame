using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AutoFixTitleScene
{
    static AutoFixTitleScene()
    {
        // エディタのコンパイル完了時に1度だけ実行する
        EditorApplication.delayCall += DoCleanup;
    }

    private static void DoCleanup()
    {
        // プレイモード中は実行しない
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // アクティブなシーンがTitleの場合のみ実行
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Title")
        {
            return;
        }

        string[] badNames = { "PopPulseCanvas", "GameManager", "ScoreManager", "UIManager", "ShapePool", "ShapeSpawner", "CustomCursor", "Directional Light", "TapCanvas", "TapGameManager" };
        bool deletedAnything = false;
        
        foreach(string objectName in badNames)
        {
            GameObject foundObject = GameObject.Find(objectName);
            if (foundObject != null)
            {
                Object.DestroyImmediate(foundObject);
                deletedAnything = true;
            }
        }
        
        // CustomCursorなどのゴミオブジェクトを検知して削除
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "CustomCursor" || obj.name == "PausePanel")
            {
                Object.DestroyImmediate(obj);
                deletedAnything = true;
            }
        }

        if (deletedAnything)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            // Debug.Log("<color=green>【自動修復完了】Titleシーンに混入していたGameシーンのオブジェクトを自動で削除しました！</color>");
        }
    }
}
