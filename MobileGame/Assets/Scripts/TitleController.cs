using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// 画面初期化漏れによるバグを防ぐため、UI要素を動的生成してライフサイクルを自己完結させる
public class TitleController : MonoBehaviour
{
    // 設定画面を開いたときに表示されるパネルオブジェクト
    private GameObject settingsPanel;

    // タイトル文字のフォントサイズ
    private const int MainTitleFontSize = 80;
    // 難易度選択などのメインボタンのフォントサイズ
    private const int MainButtonFontSize = 55;
    // 設定を開くボタンのフォントサイズ
    private const int SettingsButtonFontSize = 55;
    // 閉じるボタンのフォントサイズ
    private const int CloseButtonFontSize = 55;
    // BGM/SEなどを切り替えるトグルボタンのフォントサイズ
    private const int ToggleButtonFontSize = 50;
    // ハイスコアリセットボタンのフォントサイズ
    private const int ResetButtonFontSize = 45;

    // ボタンUIの横幅
    private const float ButtonWidth = 400f;
    // ボタンUIの縦幅
    private const float ButtonHeight = 100f;
    // トグルボタンUIの横幅（文字が長いため少し広め）
    private const float ToggleButtonWidth = 500f;
    
    // 設定パネルの背景となる黒の不透明度
    private const float MainPanelAlpha = 0.95f;
    // 各ボタンの背景となる半透明色の不透明度
    private const float ButtonBgAlpha = 0.8f;

    // ボタンが明滅（脈動）する際の拡大量
    private const float PulseAnimatorAmount = 0.03f;
    // ボタンが明滅（脈動）する際の速度
    private const float PulseAnimatorSpeed = 2f;

    private void Start()
    {
        // 画面解像度に合わせてUIをスケーリングするCanvasScalerを追加・設定
        UnityEngine.UI.CanvasScaler scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null) {
            scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        }
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1.0f; // 高さに合わせてスケーリング

        // タイトル画面ではマウス操作を許可するためカーソルロックを解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 保存された設定の読み込み
        // GameSettings.MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);

        // UIイベントを処理するため、シーン内にEventSystemが存在しない場合は動的生成
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 依存オブジェクトロード前のNullReferenceバグを防ぐための初期化保証
        if (FindObjectOfType<AudioManager>() == null)
        {
            GameObject audioObject = new GameObject("AudioManager");
            audioObject.AddComponent<AudioSource>();
            audioObject.AddComponent<AudioManager>();
        }

        // シーン内の静的なタイトル文字（手動配置された古いPOP PULSE等）を探して削除する
        Text[] allTexts = FindObjectsOfType<Text>();
        foreach (Text t in allTexts)
        {
            if (t.gameObject.name.Contains("Title") || t.text.Contains("POP") || t.text.Contains("PULSE"))
            {
                Destroy(t.gameObject);
            }
        }



        Transform promptTextTransform = transform.Find("PromptText");
        if (promptTextTransform != null) Destroy(promptTextTransform.gameObject);

        // フォント欠損によるエラーを防ぐためフォールバック指定
        Font uiFont = Resources.Load<Font>("Fonts/Consola") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // リッチなタイトルロゴを動的生成
        // リッチなタイトルロゴ（画像版）を動的生成
        CreateImageTitleLogo();

        CreateButton("EasyButton", "EASY", new Vector2(0, 50), Color.green, () => StartGame(GameDifficulty.Easy), uiFont);
        CreateButton("NormalButton", "NORMAL", new Vector2(0, -100), Color.cyan, () => StartGame(GameDifficulty.Normal), uiFont);
        CreateButton("HardButton", "HARD", new Vector2(0, -250), Color.red, () => StartGame(GameDifficulty.Hard), uiFont);

        CreateButton("SettingsButton", "⚙ SETTINGS", new Vector2(0, -450), Color.gray, OpenSettings, uiFont);

        BuildSettingsPanel(uiFont);
    }

    /// Unityの標準UIコンポーネントを複数重ねることで、リッチな2Dロゴ風のタイトルを生成する
    private void CreateRichTitleLogo(Font font)
    {
        // ロゴの親オブジェクト
        GameObject logoRoot = new GameObject("RichTitleLogo");
        logoRoot.transform.SetParent(transform, false);
        RectTransform rootRect = logoRoot.AddComponent<RectTransform>();
        rootRect.anchoredPosition = new Vector2(0, 350); // 画面上部に配置

        // アニメーション（ゆっくりと脈動する）
        UIAnimator animator = logoRoot.AddComponent<UIAnimator>();
        animator.PulseScale = true;
        animator.PulseAmount = 0.05f;
        animator.PulseSpeed = 1.5f;

        // レイヤー1: 深い影（ドロップシャドウ）
        CreateLogoTextLayer(logoRoot, "ShadowLayer", font, new Color(0, 0, 0, 0.7f), new Vector2(10, -10), 120);

        // レイヤー2: 極太のアウトライン（白枠）
        GameObject outlineLayer = CreateLogoTextLayer(logoRoot, "OutlineLayer", font, Color.white, Vector2.zero, 120);
        Outline thickOutline = outlineLayer.AddComponent<Outline>();
        thickOutline.effectColor = Color.white;
        thickOutline.effectDistance = new Vector2(6, -6);
        Outline thickOutline2 = outlineLayer.AddComponent<Outline>();
        thickOutline2.effectColor = Color.white;
        thickOutline2.effectDistance = new Vector2(-6, 6);

    }

    /// AI生成したクリーンで高品質な2Dロゴ画像を使ってタイトルを生成する
    private void CreateImageTitleLogo()
    {
        // ロゴの親オブジェクト
        GameObject logoRoot = new GameObject("ImageTitleLogo");
        logoRoot.transform.SetParent(transform, false);
        RectTransform rootRect = logoRoot.AddComponent<RectTransform>();
        rootRect.anchoredPosition = new Vector2(0, 350); // 画面上部に配置
        
        // 元の画像の比率を維持しながら、より大きく・見やすいサイズに調整
        rootRect.sizeDelta = new Vector2(1000, 450);

        // 画像を表示するImageコンポーネント
        Image logoImage = logoRoot.AddComponent<Image>();
        
        // Resourcesフォルダから画像をロード
        Texture2D tex = Resources.Load<Texture2D>("pop_pulse_logo");
        if (tex != null)
        {
            // Texture2Dから動的にSpriteを生成してImageにセットする
            Sprite logoSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            logoImage.sprite = logoSprite;
            // 画像の縦横比を保持する
            logoImage.preserveAspect = true;
            
            // 黒背景を透明にして光らせるため、加算合成（Additive）マテリアルを適用する
            Shader additiveShader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Particles/Standard Unlit");
            if (additiveShader != null)
            {
                Material additiveMat = new Material(additiveShader);
                logoImage.material = additiveMat;
            }
        }

        // アニメーション（ゆっくりと脈動する）
        UIAnimator animator = logoRoot.AddComponent<UIAnimator>();
        animator.PulseScale = true;
        animator.PulseAmount = 0.05f;
        animator.PulseSpeed = 1.5f;
    }

    private GameObject CreateLogoTextLayer(GameObject parent, string name, Font font, Color color, Vector2 offset, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        Text textComp = textObj.AddComponent<Text>();
        textComp.text = "POP PULSE";
        textComp.font = font;
        textComp.fontStyle = FontStyle.Bold;
        textComp.fontSize = fontSize;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = color;
        // 文字がはみ出さないようにオーバーフローを許可
        textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComp.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 200);
        rect.anchoredPosition = offset;
        return textObj;
    }

    /// 入力: name, text, position, color, action, font | 出力: なし | 副作用: Canvas配下にボタンオブジェクト生成、リスナー登録
    private void CreateButton(string name, string text, Vector2 position, Color color, UnityEngine.Events.UnityAction action, Font font)
    {
        // ボタンの親となる空のゲームオブジェクトを作成
        GameObject buttonObject = new GameObject(name);
        // 現在のオブジェクト（Canvas）の子要素として配置し、スケールを維持
        buttonObject.transform.SetParent(transform, false);
        // 背景を描画するためのImageコンポーネントを追加
        Image buttonImage = buttonObject.AddComponent<Image>();
        // 背景色を指定色＋半透明に設定
        buttonImage.color = new Color(color.r, color.g, color.b, ButtonBgAlpha);
        
        // インタラクションを受け付けるButtonコンポーネントを追加
        Button button = buttonObject.AddComponent<Button>();
        // クリック時に実行される処理（action）を登録
        button.onClick.AddListener(action);
        // ボタンのホバー・クリック時の色変化を設定
        SetButtonHoverState(button);

        // ボタンの配置とサイズを設定するためのRectTransformを取得
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        // 指定された座標に配置
        rectTransform.anchoredPosition = position;
        // 定数で定義した幅と高さを適用
        rectTransform.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        // ボタン上のテキストを表示するための子オブジェクトを作成
        GameObject textObject = new GameObject("TextLabel");
        // テキストオブジェクトをボタンの子要素として配置
        textObject.transform.SetParent(buttonObject.transform, false);
        // テキスト描画用のTextコンポーネントを追加
        Text textComponent = textObject.AddComponent<Text>();
        // 表示する文字列を設定
        textComponent.text = text;
        // 指定されたフォントを設定
        textComponent.font = font;
        // フォントを太字にする
        textComponent.fontStyle = FontStyle.Bold;
        // メインボタン用のフォントサイズを適用
        textComponent.fontSize = MainButtonFontSize;
        // 文字を中央揃えに設定
        textComponent.alignment = TextAnchor.MiddleCenter;
        // 視認性を高めるため、文字色は白で固定する（ボタン背景色と同化するのを防ぐ）
        textComponent.color = Color.white;

        // 視認性向上のために黒い縁取り（アウトライン）を追加
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        // テキストオブジェクトの配置とサイズを設定するためのRectTransformを取得
        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
        // テキスト領域をボタンと同じサイズにする
        textRectTransform.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        // ボタンの中央に配置
        textRectTransform.anchoredPosition = Vector2.zero;

        // ボタンの視認性を高めるため微細なスケールアニメーションを適用する自作コンポーネントを追加
        UIAnimator animator = buttonObject.AddComponent<UIAnimator>();
        // 脈動アニメーションを有効化
        animator.PulseScale = true;
        // 定数で定義した脈動の拡大量を適用
        animator.PulseAmount = PulseAnimatorAmount;
        // 定数で定義した脈動の速度を適用
        animator.PulseSpeed = PulseAnimatorSpeed;
    }

    /// 入力: font | 出力: なし | 副作用: 非表示設定パネルの生成
    private void BuildSettingsPanel(Font font)
    {
        // パネルのルートオブジェクトを作成
        settingsPanel = new GameObject("SettingsPanel");
        // Canvas（transform）の子要素として配置
        settingsPanel.transform.SetParent(transform, false);
        // 背景用のImageコンポーネントを追加
        Image backgroundImage = settingsPanel.AddComponent<Image>();
        // 背景色を半透明の黒に設定
        backgroundImage.color = new Color(0, 0, 0, MainPanelAlpha);
        
        // パネルが画面全体を覆うようにアンカーとサイズを設定
        RectTransform rectTransform = settingsPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        
        // パネル内のタイトルテキスト用オブジェクトを作成
        GameObject titleObject = new GameObject("SettingsTitle");
        titleObject.transform.SetParent(settingsPanel.transform, false);
        Text titleText = titleObject.AddComponent<Text>();
        titleText.text = "SETTINGS";
        titleText.font = font;
        titleText.fontStyle = FontStyle.Bold;
        titleText.fontSize = MainTitleFontSize;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        
        Outline titleOutline = titleObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        titleOutline.effectDistance = new Vector2(3f, -3f);
        
        // 上部に配置
        titleObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 400);

        // BGM切り替え用のトグルボタンを生成
        CreateToggleButton("BgmToggle", "BGM", new Vector2(0, 100), font, 
            () => GameSettings.BgmEnabled, 
            () => { AudioManager.Instance.SetBGMEnabled(!GameSettings.BgmEnabled); });

        // SE切り替え用のトグルボタンを生成
        CreateToggleButton("SeToggle", "SOUND EFFECTS", new Vector2(0, -50), font, 
            () => GameSettings.SeEnabled, 
            () => { AudioManager.Instance.SetSEEnabled(!GameSettings.SeEnabled); });

        // マウス感度設定スライダーを生成
        CreateSliderField("MouseSensitivity", "AIM SENSITIVITY", new Vector2(0, -180), font);

        // ハイスコアリセット用のボタンオブジェクトを作成
        GameObject resetButtonObject = new GameObject("ResetButton");
        resetButtonObject.transform.SetParent(settingsPanel.transform, false);
        Image resetBackgroundImage = resetButtonObject.AddComponent<Image>();
        resetBackgroundImage.color = new Color(0.2f, 0, 0); // 暗い赤色
        Button resetButton = resetButtonObject.AddComponent<Button>();
        SetButtonHoverState(resetButton);
        // リセット処理をリスナーに登録
        resetButton.onClick.AddListener(() => {
            PlayerPrefs.SetInt("HighScore", 0); // 保存されたスコアを0で上書き
            // Debug.Log("High Score Reset!");
            AudioManager.Instance.PlaySE(2); // 特殊なSEを再生
        });
        resetButtonObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -320);
        resetButtonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        GameObject resetTextObject = new GameObject("TextLabel");
        resetTextObject.transform.SetParent(resetButtonObject.transform, false);
        Text resetText = resetTextObject.AddComponent<Text>();
        resetText.text = "RESET HIGH SCORE";
        resetText.font = font;
        resetText.fontStyle = FontStyle.Bold;
        resetText.fontSize = ResetButtonFontSize;
        resetText.alignment = TextAnchor.MiddleCenter;
        resetText.color = Color.red;
        
        Outline resetOutline = resetTextObject.AddComponent<Outline>();
        resetOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        resetOutline.effectDistance = new Vector2(2f, -2f);
        
        resetTextObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        GameObject closeButtonObject = new GameObject("CloseButton");
        closeButtonObject.transform.SetParent(settingsPanel.transform, false);
        Image closeBackgroundImage = closeButtonObject.AddComponent<Image>();
        closeBackgroundImage.color = new Color(0.2f, 0.2f, 0.2f);
        Button closeButton = closeButtonObject.AddComponent<Button>();
        SetButtonHoverState(closeButton);
        closeButton.onClick.AddListener(() => {
            AudioManager.Instance.PlaySE(0);
            settingsPanel.SetActive(false);
        });
        closeButtonObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -450);
        closeButtonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        GameObject closeTextObject = new GameObject("TextLabel");
        closeTextObject.transform.SetParent(closeButtonObject.transform, false);
        Text closeText = closeTextObject.AddComponent<Text>();
        closeText.text = "CLOSE";
        closeText.font = font;
        closeText.fontStyle = FontStyle.Bold;
        closeText.fontSize = CloseButtonFontSize;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;
        
        Outline closeOutline = closeTextObject.AddComponent<Outline>();
        closeOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        closeOutline.effectDistance = new Vector2(2f, -2f);
        
        closeTextObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        settingsPanel.SetActive(false);
    }

    /// 入力: name, label, position, font, getState, onClick | 出力: なし | 副作用: トグルボタンの生成とコールバック登録
    private void CreateToggleButton(string name, string label, Vector2 position, Font font, System.Func<bool> getState, System.Action onClick)
    {
        // トグルボタンの親となる空のゲームオブジェクトを作成
        GameObject buttonObject = new GameObject(name);
        // 設定パネルの子要素として配置
        buttonObject.transform.SetParent(settingsPanel.transform, false);
        // 背景を描画するためのImageコンポーネントを追加
        Image backgroundImage = buttonObject.AddComponent<Image>();
        // 背景色を暗いグレーに設定
        backgroundImage.color = new Color(0.1f, 0.1f, 0.1f);
        
        // ボタン上のテキストを表示するための子オブジェクトを作成
        GameObject textObject = new GameObject("TextLabel");
        // テキストオブジェクトをボタンの子要素として配置
        textObject.transform.SetParent(buttonObject.transform, false);
        // テキスト描画用のTextコンポーネントを追加
        Text textComponent = textObject.AddComponent<Text>();
        // 指定されたフォントを設定
        textComponent.font = font;
        // フォントを太字にする
        textComponent.fontStyle = FontStyle.Bold;
        // トグルボタン用のフォントサイズを適用
        textComponent.fontSize = ToggleButtonFontSize;
        // 文字を中央揃えに設定
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        // 視認性向上のために黒い縁取り（アウトライン）を追加
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        
        // インタラクションを受け付けるButtonコンポーネントを追加
        Button button = buttonObject.AddComponent<Button>();
        // ボタンのホバー・クリック時の色変化を設定
        SetButtonHoverState(button);
        // クリック時の処理を登録
        button.onClick.AddListener(() => {
            // 外部から渡された切り替え処理を実行
            onClick();
            // 決定音を再生
            AudioManager.Instance.PlaySE(0);
            // 現在の状態を取得し、ON/OFFの文字列を更新
            textComponent.text = label + ": " + (getState() ? "ON" : "OFF");
            // ONなら緑、OFFなら赤に文字色を変更
            textComponent.color = getState() ? Color.green : Color.red;
        });

        // ボタンの配置位置を設定
        buttonObject.GetComponent<RectTransform>().anchoredPosition = position;
        // トグルボタン用のサイズを適用
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ToggleButtonWidth, ButtonHeight);
        // テキストのサイズをボタンに合わせる
        textObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ToggleButtonWidth, ButtonHeight);

        // 初期状態のテキストと色を設定
        textComponent.text = label + ": " + (getState() ? "ON" : "OFF");
        textComponent.color = getState() ? Color.green : Color.red;
    }

    /// 入力: button | 出力: なし | 副作用: button.colors構造体への色設定の上書き
    private void SetButtonHoverState(Button button)
    {
        // Buttonの色設定構造体を取得
        ColorBlock colorBlock = button.colors;
        // 通常時の色（やや暗めの白）
        colorBlock.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        // マウスホバー時の色（明るく発光するような白）
        colorBlock.highlightedColor = new Color(2f, 2f, 2f, 1f);
        // クリック/タップ時の色（暗いグレー）
        colorBlock.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        // 選択状態時の色（ゲームパッド等でのナビゲーション時もホバーと同じく明るく発光させる）
        colorBlock.selectedColor = new Color(2f, 2f, 2f, 1f);
        // 変更した色設定をButtonに適用
        button.colors = colorBlock;
    }

    /// 入力: name, label, position, font | 出力: なし | 副作用: スライダーUIの動的生成
    private void CreateSliderField(string name, string label, Vector2 position, Font font)
    {
        GameObject sensPanelObj = new GameObject(name, typeof(RectTransform));
        sensPanelObj.transform.SetParent(settingsPanel.transform, false);
        sensPanelObj.GetComponent<RectTransform>().anchoredPosition = position;
        sensPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ToggleButtonWidth, 100);

        GameObject sensTextObj = new GameObject("Label");
        sensTextObj.transform.SetParent(sensPanelObj.transform, false);
        Text sensText = sensTextObj.AddComponent<Text>();
        sensText.font = font;
        sensText.fontStyle = FontStyle.Bold;
        sensText.fontSize = 35;
        sensText.alignment = TextAnchor.UpperCenter;
        sensText.color = Color.white;
        
        Outline sensOutline = sensTextObj.AddComponent<Outline>();
        sensOutline.effectColor = new Color(0, 0, 0, 0.8f);
        sensOutline.effectDistance = new Vector2(2f, -2f);
        
        sensTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ToggleButtonWidth, 50);
        sensTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);

        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform));
        sliderObj.transform.SetParent(sensPanelObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);
        sliderObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
        
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        bgObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.25f);
        bgObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.75f);
        bgObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject fillAreaObj = new GameObject("FillArea");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.cyan;
        fillObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject handleAreaObj = new GameObject("HandleSlideArea");
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = Color.white;
        handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 0);

        slider.targetGraphic = handleImg;
        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.handleRect = handleObj.GetComponent<RectTransform>();
        // 感度の設定幅を広げて、さまざまなマウスDPIに対応できるようにする
        slider.minValue = 0.05f;
        slider.maxValue = 10.0f;
        slider.value = GameSettings.MouseSensitivity;

        sensText.text = label + ": " + slider.value.ToString("F2");

        slider.onValueChanged.AddListener((val) => {
            GameSettings.MouseSensitivity = val;
            // PlayerPrefs.SetFloat("MouseSensitivity", val);
            sensText.text = label + ": " + val.ToString("F2");
        });
    }

    private void OpenSettings()
    {
        // 決定音を再生
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(0);
        // 設定パネルをアクティブにして表示
        settingsPanel.SetActive(true);
    }

    /// 入力: difficulty | 出力: なし | 副作用: GameSettings更新、シーン遷移の開始
    private void StartGame(GameDifficulty difficulty)
    {
        // ゲーム開始音を再生
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(1);
        // 選択された難易度を静的クラスのGameSettingsに保存
        GameSettings.Difficulty = difficulty;
        
        // ローディングの不快感を減らすためフェードアウトを利用
        if (SceneFader.Instance != null)
        {
            // フェードアウト演出を挟んでからGameシーンをロード
            SceneFader.Instance.FadeAndLoadScene("Game");
        }
        else
        {
            // フェーダーが存在しない場合は直接Gameシーンをロード（フォールバック）
            SceneManager.LoadScene("Game");
        }
    }
}
