using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// ゲームシーンのヒエラルキーおよび必要なオブジェクト群を自動生成するエディタ拡張
public class PopPulseSetup
{
    [MenuItem("Tools/Setup Pop Pulse Game")]
    public static void SetupPopPulse()
    {
        // 古いオブジェクトの残留による依存関係の破壊を防ぐため初期化時にクリーンアップ
        string[] oldNames = { "TapCanvas", "TapGameManager", "TargetSpawner", "ShapeSpawner", "ShapePool", "PopPulseCanvas", "PopPulseManager", "TargetPrefab", "GameManager", "ScoreManager", "UIManager" };
        foreach(string objectName in oldNames)
        {
            GameObject foundObject = GameObject.Find(objectName);
            if (foundObject != null) Object.DestroyImmediate(foundObject);
        }

        // メインカメラを取得
        Camera mainCamera = Camera.main;
        // メインカメラが存在しない場合は新規作成する
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
        }
        
        // 2D描画でのUIとオブジェクトの前後関係を正しく保つためZ座標を固定
        mainCamera.transform.position = new Vector3(0, 0, -10f);
        
        // 2Dゲーム向けに平行投影モードに設定
        mainCamera.orthographic = true;
        // 描画サイズを設定
        mainCamera.orthographicSize = 5f;
        // 背景を単色で塗りつぶすように設定
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        // 背景色をディープ・ミッドナイトブルー（ベースカラー：60%）に指定
        mainCamera.backgroundColor = new Color(0.043f, 0.059f, 0.098f); // #0B0F19
        
        // カメラシェイク用コンポーネントがなければ追加
        if (mainCamera.GetComponent<CameraShake>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraShake>();
        }

        // 音声を聞き取るためのAudioListenerがなければ追加
        if (mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
        
        // 背景エフェクト用コンポーネントがなければ追加
        if (mainCamera.GetComponent<BackgroundEffects>() == null)
        {
            mainCamera.gameObject.AddComponent<BackgroundEffects>();
        }

        // 3Dの陰影を表現するため、シーンにDirectional Lightが存在しなければ追加する
        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            // 立体感が綺麗に出るように斜め上からの光にする
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.color = new Color(1f, 0.95f, 0.9f);
            light.intensity = 1.2f;
        }

        // UI描画の基盤となるCanvasオブジェクトを生成
        GameObject canvasObject = new GameObject("PopPulseCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        // 画面の最前面にUIを描画するモード
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        
        // 多様な解像度のモバイル端末に対応するためスケールを画面サイズに追従させる
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // 基準となる解像度を設定
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        // 画面のアスペクト比に合わせて拡大縮小するモード
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // 縦横のどちらを基準にするかの中間値を設定
        canvasScaler.matchWidthOrHeight = 0.5f;
        // UIのクリック判定モジュールを追加
        canvasObject.AddComponent<GraphicRaycaster>();

        // タイトル用・UI用のフォントをロード
        Font titleFont = Resources.Load<Font>("Fonts/Impact") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font uiFont = Resources.Load<Font>("Fonts/Consola") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // スコア表示用テキストオブジェクトを生成
        GameObject scoreTextObject = new GameObject("ScoreText");
        scoreTextObject.transform.SetParent(canvasObject.transform, false);
        Text scoreTextComponent = scoreTextObject.AddComponent<Text>();
        scoreTextComponent.text = "SCORE\n<size=60>0</size>";
        scoreTextComponent.font = titleFont;
        scoreTextComponent.fontSize = 24;
        // スコアのラベルは色の休憩所（無彩色）としてアイスホワイトを指定
        scoreTextComponent.color = new Color(0.94f, 0.95f, 0.97f); // #F0F4F8
        scoreTextComponent.alignment = TextAnchor.UpperLeft;
        scoreTextComponent.supportRichText = true;
        // UIの不要なイベント判定を切りパフォーマンスを向上
        scoreTextComponent.raycastTarget = false;
        
        // テキストに黒い縁取りを追加
        Outline scoreOutline = scoreTextObject.AddComponent<Outline>();
        scoreOutline.effectColor = Color.black;
        scoreOutline.effectDistance = new Vector2(2f, -2f);

        // スコアテキストの微小な脈動アニメーションを設定
        UIAnimator scoreAnimator = scoreTextObject.AddComponent<UIAnimator>();
        scoreAnimator.PulseAmount = 0.02f;
        scoreAnimator.PulseSpeed = 1.5f;
        
        // スコアテキストの配置位置（左上）を設定
        RectTransform scoreRectTransform = scoreTextObject.GetComponent<RectTransform>();
        scoreRectTransform.anchorMin = new Vector2(0, 1);
        scoreRectTransform.anchorMax = new Vector2(0, 1);
        scoreRectTransform.pivot = new Vector2(0, 1);
        scoreRectTransform.anchoredPosition = new Vector2(30, -30);
        scoreRectTransform.sizeDelta = new Vector2(300, 150);

        // コンボ表示用テキストオブジェクトを生成
        GameObject comboTextObject = new GameObject("ComboText");
        comboTextObject.transform.SetParent(canvasObject.transform, false);
        Text comboTextComponent = comboTextObject.AddComponent<Text>();
        comboTextComponent.text = "";
        comboTextComponent.font = titleFont;
        comboTextComponent.fontSize = 30;
        comboTextComponent.color = new Color(1f, 0.2f, 0.4f, 0.5f); // #FF3366（ネオンピンク）
        comboTextComponent.alignment = TextAnchor.UpperRight;
        comboTextComponent.supportRichText = true;
        comboTextComponent.raycastTarget = false;
        
        // コンボテキストに縁取りとドロップシャドウを追加
        Outline comboOutline = comboTextObject.AddComponent<Outline>();
        comboOutline.effectColor = new Color(0.5f, 0f, 0f, 1f);
        comboOutline.effectDistance = new Vector2(4f, -4f);
        Shadow comboShadow = comboTextObject.AddComponent<Shadow>();
        comboShadow.effectColor = Color.black;
        comboShadow.effectDistance = new Vector2(6f, -6f);

        // コンボテキストの配置位置（右上）を設定
        RectTransform comboRectTransform = comboTextObject.GetComponent<RectTransform>();
        comboRectTransform.anchorMin = new Vector2(1, 1);
        comboRectTransform.anchorMax = new Vector2(1, 1);
        comboRectTransform.pivot = new Vector2(1, 1);
        comboRectTransform.anchoredPosition = new Vector2(-30, -30);
        comboRectTransform.sizeDelta = new Vector2(400, 200);

        // コンボテキストの激しい脈動アニメーションを設定
        UIAnimator comboAnimator = comboTextObject.AddComponent<UIAnimator>();
        comboAnimator.PulseAmount = 0.1f;
        comboAnimator.PulseSpeed = 6f;
        comboAnimator.ColorFade = true;
        comboAnimator.ColorFadeSpeed = 5f;
        comboAnimator.TargetColor = new Color(1f, 0.2f, 0.4f); // #FF3366

        // タイマー表示用の円形ゲージの背景オブジェクトを生成
        GameObject timerBgObject = new GameObject("TimerRadialBg");
        timerBgObject.transform.SetParent(canvasObject.transform, false);
        Image timerBgImage = timerBgObject.AddComponent<Image>();
        timerBgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        timerBgImage.color = new Color(0f, 0f, 0f, 0.5f); // 半透明の黒
        timerBgImage.raycastTarget = false;
        
        RectTransform timerBgRectTransform = timerBgObject.GetComponent<RectTransform>();
        timerBgRectTransform.anchorMin = new Vector2(0.5f, 1f);
        timerBgRectTransform.anchorMax = new Vector2(0.5f, 1f);
        timerBgRectTransform.pivot = new Vector2(0.5f, 1f);
        timerBgRectTransform.anchoredPosition = new Vector2(0, -20);
        // 背景は少し大きめにして縁取りのように見せる
        timerBgRectTransform.sizeDelta = new Vector2(110, 110);

        // タイマー表示用の円形ゲージオブジェクトを生成
        GameObject timerObject = new GameObject("TimerRadial");
        timerObject.transform.SetParent(canvasObject.transform, false);
        Image timerImage = timerObject.AddComponent<Image>();
        // 円形ゲージ用のスプライトを読み込み
        timerImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        // スクリプトから描画量を操作できるようにFilledタイプに設定
        timerImage.type = Image.Type.Filled;
        timerImage.fillMethod = Image.FillMethod.Radial360;
        timerImage.fillAmount = 1f;
        timerImage.color = new Color(0f, 0.9f, 1f); // #00E5FF（エレクトリックシアン）
        timerImage.raycastTarget = false;
        
        // タイマーの配置位置（上部中央）を設定
        RectTransform timerRectTransform = timerObject.GetComponent<RectTransform>();
        timerRectTransform.anchorMin = new Vector2(0.5f, 1f);
        timerRectTransform.anchorMax = new Vector2(0.5f, 1f);
        timerRectTransform.pivot = new Vector2(0.5f, 1f);
        timerRectTransform.anchoredPosition = new Vector2(0, -20);
        timerRectTransform.sizeDelta = new Vector2(100, 100);

        // ゲームの進行を管理するGameManagerの生成
        GameObject gameManagerObject = new GameObject("GameManager");
        GameManager gameManagerComponent = gameManagerObject.AddComponent<GameManager>();

        // スコアを管理するScoreManagerの生成
        GameObject scoreManagerObject = new GameObject("ScoreManager");
        ScoreManager scoreManagerComponent = scoreManagerObject.AddComponent<ScoreManager>();

        // UIを管理するUIManagerの生成と参照の紐付け
        GameObject uiManagerObject = new GameObject("UIManager");
        UIManager uiManagerComponent = uiManagerObject.AddComponent<UIManager>();
        uiManagerComponent.ScoreText = scoreTextComponent;
        uiManagerComponent.ComboText = comboTextComponent;
        uiManagerComponent.TimeProgressCircle = timerImage;

        // 的のプーリングを管理するShapePoolの生成
        GameObject poolObject = new GameObject("ShapePool");
        ShapePool poolComponent = poolObject.AddComponent<ShapePool>();

        // 通常の的のプレハブとして利用するSphereオブジェクトを生成
        GameObject standardShape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        standardShape.name = "StandardShapePrefab";
        standardShape.transform.SetParent(poolObject.transform);
        // 画面外に隠しておくための座標設定
        standardShape.transform.localPosition = new Vector3(1000, 0, 0);
        
        // 3Dシェーダー（Standard）を使用して陰影と光沢をつける
        Material standardMaterial = new Material(Shader.Find("Standard"));
        standardMaterial.color = new Color(0f, 0.9f, 1f); // メインカラー：エレクトリックシアン
        standardMaterial.SetFloat("_Glossiness", 0.8f); // 表面をツヤツヤにする
        standardShape.GetComponent<MeshRenderer>().sharedMaterial = standardMaterial;
        
        // 的のタップ判定には、3D回転しても面積が変わらないSphereColliderを維持する
        standardShape.GetComponent<SphereCollider>().radius = 0.6f;
        // 的のロジックコンポーネントを追加し、生存時間を設定
        standardShape.AddComponent<ShapeTarget>().生存時間 = 2.0f;
        
        // ボーナス的のプレハブとして利用するCubeオブジェクトを生成
        GameObject bonusShape = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bonusShape.name = "BonusShapePrefab";
        bonusShape.transform.SetParent(poolObject.transform);
        bonusShape.transform.localPosition = new Vector3(1000, 0, 0);
        
        // ボーナス的も同様に3Dシェーダーを適用
        Material bonusMaterial = new Material(Shader.Find("Standard"));
        bonusMaterial.color = new Color(1f, 0.2f, 0.4f); // アクセントカラー：ネオンピンク
        bonusMaterial.SetFloat("_Glossiness", 0.8f);
        bonusShape.GetComponent<MeshRenderer>().sharedMaterial = bonusMaterial;
        
        // ボーナス的も回転時のタップ抜けを防ぐため、BoxColliderではなくSphereColliderに置換する
        Object.DestroyImmediate(bonusShape.GetComponent<BoxCollider>());
        SphereCollider bonusCollider = bonusShape.AddComponent<SphereCollider>();
        bonusCollider.radius = 0.6f; // 四角形をすっぽり覆う少し大きめの判定
        // ボーナス的は消えるのが早いため生存時間を短く設定
        bonusShape.AddComponent<ShapeTarget>().生存時間 = 1.5f;

        // プールにプレハブを登録
        poolComponent.StandardShapePrefab = standardShape;
        poolComponent.BonusShapePrefab = bonusShape;
        // ゲームプレイ中のスパイクを防ぐためシーン生成時にメモリ確保を完了させる
        poolComponent.Initialize();

        // 的の生成を管理するShapeSpawnerの生成
        GameObject spawnerObject = new GameObject("ShapeSpawner");
        ShapeSpawner spawnerComponent = spawnerObject.AddComponent<ShapeSpawner>();
        // 生成間隔とボーナス的の出現確率を設定
        spawnerComponent.現在の生成間隔 = 0.8f;
        spawnerComponent.ボーナス出現率 = 0.2f;

        // 生成したシーンの変更をエディタに保存させるフラグを立てる
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        // Debug.Log("Pop Pulse Game Setup Complete!");
    }

    [MenuItem("Tools/Fix Broken Title Scene")]
    public static void FixTitleScene()
    {
        // Titleシーンのパス
        string titleScenePath = "Assets/Scenes/Title.unity";
        
        // もしTitleシーンが存在すれば開く
        if (System.IO.File.Exists(titleScenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(titleScenePath);
        }
        
        // Titleシーンに存在してはいけないGameシーン専用のオブジェクト群を自動検索して削除
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
        
        // Missing Scriptになっているオブジェクトを検知して削除 (CustomCursorなど)
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "CustomCursor")
            {
                Object.DestroyImmediate(obj);
                deletedAnything = true;
            }
        }

        // 変更があればシーンを上書き保存する
        if (deletedAnything)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            UnityEditor.EditorUtility.DisplayDialog("Fix Complete", "Titleシーンの修復が完了しました！\n余計なUIはすべて削除されました。", "OK");
        }
        else
        {
            UnityEditor.EditorUtility.DisplayDialog("Fix Complete", "Titleシーンは既に正常です。\n削除するべきUIは見つかりませんでした。", "OK");
        }
    }
}
