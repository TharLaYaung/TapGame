using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// 全シーンのベースUIおよび設定を自動構築するエディタ拡張
public class AutoSceneBuilder
{
    [UnityEditor.Callbacks.DidReloadScripts]
    public static void AutoRunOnce()
    {
        // 意図せぬ無限ループを防ぐため、EditorPrefsを利用して1度だけ実行するよう制限
        if (EditorPrefs.GetBool("AutoSceneBuilder_RanOnce_ItemSprites", false)) return;
        EditorPrefs.SetBool("AutoSceneBuilder_RanOnce_ItemSprites", true);

        // 新規アイテムスプライトのインポート設定を強制的に適用させる
        AssetDatabase.ImportAsset("Assets/Resources/Items/icon_bomb.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Items/icon_freeze.png", ImportAssetOptions.ForceUpdate);

        EditorApplication.delayCall += BuildAllScenes;
    }

    [MenuItem("Tools/Build All Scenes")]
    public static void BuildAllScenes()
    {
        // プレイ中のシーン破壊を防ぐため、実行モード時は処理をブロック
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Debug.Log("[System] Cannot build scenes while in Play Mode! Please exit Play Mode and manually run 'Tools -> Build All Scenes'.");
            return;
        }

        // プロジェクト内にScenesフォルダが存在しない場合は自動作成
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // 各シーンファイルの保存先パスを定義
        string titlePath = "Assets/Scenes/Title.unity";
        string gamePath = "Assets/Scenes/Game.unity";
        string resultPath = "Assets/Scenes/Result.unity";

        // タイトルシーンを新規作成
        Scene titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // タイトル画面用のUIと設定を自動構築
        BuildTitleUI();
        // 構築したシーンを指定パスに保存
        EditorSceneManager.SaveScene(titleScene, titlePath);

        // リザルトシーンを新規作成
        Scene resultScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // リザルト画面用のUIと設定を自動構築
        BuildResultUI();
        // 構築したシーンを指定パスに保存
        EditorSceneManager.SaveScene(resultScene, resultPath);

        // ゲーム本編シーンを新規作成
        Scene gameScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // 別スクリプトを利用してゲーム本編の環境とUIを自動構築
        PopPulseSetup.SetupPopPulse();
        // 構築したシーンを指定パスに保存
        EditorSceneManager.SaveScene(gameScene, gamePath);

        // プロジェクトのビルド設定に全シーンを一括登録
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(titlePath, true),
            new EditorBuildSettingsScene(gamePath, true),
            new EditorBuildSettingsScene(resultPath, true)
        };

        // すぐにプレイアブルな状態にするためタイトルシーンを開く
        EditorSceneManager.OpenScene(titlePath);

        // Debug.Log("[System] Scene Generation Complete! All 3 scenes created and added to Build Settings. Press Play!");
    }

    private static void BuildTitleUI()
    {
        // メインカメラ用のゲームオブジェクトを生成してCameraコンポーネントを追加
        Camera cameraComponent = new GameObject("Main Camera").AddComponent<Camera>();
        // カメラの背景塗りつぶし設定を単色に設定
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        // 背景色を暗い紺色に設定
        cameraComponent.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        // 2Dゲーム用の平行投影カメラに設定
        cameraComponent.orthographic = true;
        // カメラの描画サイズを指定
        cameraComponent.orthographicSize = 5f;
        // メインカメラとして認識させるためのタグ付け
        cameraComponent.gameObject.tag = "MainCamera";
        // 音声を聞き取るためのAudioListenerコンポーネントを追加
        cameraComponent.gameObject.AddComponent<AudioListener>();
        // 背景の装飾エフェクトを制御するスクリプトを追加
        cameraComponent.gameObject.AddComponent<BackgroundEffects>();

        // Canvasのルートオブジェクトを生成
        GameObject canvasObject = new GameObject("TitleCanvas");
        // Canvasコンポーネントを追加
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        // 画面全体にオーバーレイ表示するモードに設定
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 画面解像度に合わせてUIサイズを自動調整するコンポーネントを追加
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        // 画面サイズにスケールを合わせるモード
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // 基準となる解像度（縦長）を指定
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        // 幅と高さのどちらを基準に拡縮するかの中間値を指定
        canvasScaler.matchWidthOrHeight = 0.5f;
        // UIのクリック判定を行うレイキャスターを追加
        canvasObject.AddComponent<GraphicRaycaster>();

        // タイトルロゴ用のフォントをロード（見つからない場合は組み込みフォントを使用）
        Font titleFont = Resources.Load<Font>("Fonts/Impact") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // 一般UI用のフォントをロード
        Font uiFont = Resources.Load<Font>("Fonts/Consola") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // タイトルテキストのゲームオブジェクトを生成
        GameObject titleTextObject = new GameObject("TitleText");
        titleTextObject.transform.SetParent(canvasObject.transform, false);
        Text titleTextComponent = titleTextObject.AddComponent<Text>();
        titleTextComponent.text = "POP PULSE";
        titleTextComponent.font = titleFont;
        titleTextComponent.fontSize = 150;
        titleTextComponent.alignment = TextAnchor.MiddleCenter;
        titleTextComponent.color = Color.cyan;
        // ロゴを際立たせるためのアウトライン（縁取り）エフェクトを追加
        Outline titleOutline = titleTextObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0.5f, 1f);
        titleOutline.effectDistance = new Vector2(4f, -4f);
        // ロゴにドロップシャドウを追加して立体感を出す
        Shadow titleShadow = titleTextObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0, 0, 0.8f);
        titleShadow.effectDistance = new Vector2(8f, -8f);

        // タイトルロゴの配置座標とサイズを設定
        RectTransform titleRectTransform = titleTextObject.GetComponent<RectTransform>();
        titleRectTransform.anchoredPosition = new Vector2(0, 200);
        titleRectTransform.sizeDelta = new Vector2(1000, 300);

        // タイトルロゴにアニメーション（浮遊、色フェード）を適用
        UIAnimator titleAnimator = titleTextObject.AddComponent<UIAnimator>();
        titleAnimator.FloatPosition = true;
        titleAnimator.FloatAmount = 20f;
        titleAnimator.FloatSpeed = 2f;
        titleAnimator.ColorFade = true;
        titleAnimator.ColorFadeSpeed = 1.5f;
        titleAnimator.TargetColor = Color.green;

        // プレイヤーへ操作を促すテキストオブジェクトを生成
        GameObject promptTextObject = new GameObject("PromptText");
        promptTextObject.transform.SetParent(canvasObject.transform, false);
        Text promptTextComponent = promptTextObject.AddComponent<Text>();
        promptTextComponent.text = "TAP TO START";
        promptTextComponent.font = uiFont;
        promptTextComponent.fontSize = 60;
        promptTextComponent.alignment = TextAnchor.MiddleCenter;
        promptTextComponent.color = Color.magenta;
        RectTransform promptRectTransform = promptTextObject.GetComponent<RectTransform>();
        promptRectTransform.anchoredPosition = new Vector2(0, -200);
        promptRectTransform.sizeDelta = new Vector2(800, 100);

        // タイトル画面のロジック制御を行うコントローラーをCanvasに追加
        canvasObject.AddComponent<TitleController>();
        // シーン遷移時のフェード演出を担うオブジェクトを生成
        new GameObject("SceneFader").AddComponent<SceneFader>();
    }

    private static void BuildResultUI()
    {
        // リザルトシーン用のメインカメラを生成し設定
        Camera cameraComponent = new GameObject("Main Camera").AddComponent<Camera>();
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = 5f;
        cameraComponent.gameObject.tag = "MainCamera";
        cameraComponent.gameObject.AddComponent<AudioListener>();
        cameraComponent.gameObject.AddComponent<BackgroundEffects>();

        // リザルト画面用のCanvasを生成
        GameObject canvasObject = new GameObject("ResultCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // フォントの読み込み
        Font titleFont = Resources.Load<Font>("Fonts/Impact") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font uiFont = Resources.Load<Font>("Fonts/Consola") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ゲームオーバーテキストの生成と設定
        GameObject gameOverTextObject = new GameObject("GameOverText");
        gameOverTextObject.transform.SetParent(canvasObject.transform, false);
        Text gameOverText = gameOverTextObject.AddComponent<Text>();
        gameOverText.text = "GAME OVER";
        gameOverText.font = titleFont;
        gameOverText.fontSize = 120;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.color = Color.red;
        // 視認性を高める黒い縁取りエフェクト
        Outline goOutline = gameOverTextObject.AddComponent<Outline>();
        goOutline.effectColor = Color.black;
        goOutline.effectDistance = new Vector2(5f, -5f);

        RectTransform gameOverRectTransform = gameOverTextObject.GetComponent<RectTransform>();
        gameOverRectTransform.anchoredPosition = new Vector2(0, 300);
        gameOverRectTransform.sizeDelta = new Vector2(800, 200);

        // ゲームオーバーテキスト用のアニメーション
        UIAnimator gameOverAnimator = gameOverTextObject.AddComponent<UIAnimator>();
        gameOverAnimator.PulseScale = true;
        gameOverAnimator.PulseAmount = 0.05f;
        gameOverAnimator.FloatPosition = true;
        gameOverAnimator.FloatAmount = 15f;
        gameOverAnimator.ColorFade = true;
        gameOverAnimator.TargetColor = new Color(1f, 0.5f, 0f);

        // 最終スコアを表示するテキストの生成と設定
        GameObject scoreTextObject = new GameObject("ScoreText");
        scoreTextObject.transform.SetParent(canvasObject.transform, false);
        Text scoreTextComponent = scoreTextObject.AddComponent<Text>();
        scoreTextComponent.text = "FINAL SCORE\n0";
        scoreTextComponent.font = uiFont;
        scoreTextComponent.fontSize = 90;
        scoreTextComponent.alignment = TextAnchor.MiddleCenter;
        scoreTextComponent.color = Color.yellow;
        Outline scoreOutline = scoreTextObject.AddComponent<Outline>();
        scoreOutline.effectColor = Color.black;
        scoreOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform scoreRectTransform = scoreTextObject.GetComponent<RectTransform>();
        scoreRectTransform.anchoredPosition = new Vector2(0, 0);
        scoreRectTransform.sizeDelta = new Vector2(800, 300);

        // スコアテキスト用のアニメーション
        UIAnimator scoreAnimator = scoreTextObject.AddComponent<UIAnimator>();
        scoreAnimator.PulseScale = true;
        scoreAnimator.PulseSpeed = 1f;
        scoreAnimator.PulseAmount = 0.02f;
        scoreAnimator.ColorFade = true;
        scoreAnimator.ColorFadeSpeed = 3f;
        scoreAnimator.TargetColor = Color.white;

        // リスタートを促すテキストの生成
        GameObject promptTextObject = new GameObject("PromptText");
        promptTextObject.transform.SetParent(canvasObject.transform, false);
        Text promptTextComponent = promptTextObject.AddComponent<Text>();
        promptTextComponent.text = "TAP TO RESTART";
        promptTextComponent.font = uiFont;
        promptTextComponent.fontSize = 50;
        promptTextComponent.alignment = TextAnchor.MiddleCenter;
        promptTextComponent.color = Color.white;
        RectTransform promptRectTransform = promptTextObject.GetComponent<RectTransform>();
        promptRectTransform.anchoredPosition = new Vector2(0, -300);
        promptRectTransform.sizeDelta = new Vector2(800, 100);

        UIAnimator promptAnimator = promptTextObject.AddComponent<UIAnimator>();
        promptAnimator.PulseScale = true;
        promptAnimator.PulseSpeed = 3f;

        // リザルト画面の操作を管理するコントローラーを追加
        ResultController resultController = canvasObject.AddComponent<ResultController>();
        resultController.ScoreText = scoreTextComponent;

        // シーン遷移時のフェード用オブジェクトを生成
        new GameObject("SceneFader").AddComponent<SceneFader>();
    }

}
