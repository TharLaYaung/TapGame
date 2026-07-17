using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// 密結合を防ぐため、ロジックから独立してUI要素の描画・アニメーション状態のみを単一管理する
public class UIManager : MonoBehaviour
{
    // シーン内のUIを外部スクリプトから一元的に操作するためのシングルトンインスタンス
    public static UIManager Instance { get; private set; }

    [Header("UI Elements")]
    // 現在のスコアを表示するためのテキストUI
    public Text ScoreText;
    // 現在のコンボ数を表示するためのテキストUI
    public Text ComboText;
    // 残り時間を円環状のゲージで表示するための画像UI
    public Image TimeProgressCircle;

    // ゲーム中のポーズメニュー用パネル
    private GameObject pausePanel;

    // プレイヤーの注意を引くための警告明滅速度
    private const float WarningBlinkSpeed = 5f;
    // コンボテキストが拡大・縮小（ポップ）するアニメーションの速度
    private const float ComboAnimationSpeed = 6f;
    // 巨大化による画面中央のタップ阻害を防ぐためのスケール上限
    private const float ComboScaleMax = 1.5f;
    // アニメーション完了後の本来の（待機時）スケール値
    private const float ComboScaleMin = 1.0f;

    private void Awake()
    {
        // 複数生成によるステート破壊を防ぐためシングルトン化
        if (Instance == null)
        {
            // インスタンスを登録
            Instance = this;
        }
        else
        {
            // 重複オブジェクトを破棄
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // UI操作（ポーズメニューのボタン等）を受け付けるため、シーンにEventSystemがなければ動的生成
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 外部からダウンロードしたクールなSF・サイバーパンク風のフォントを動的に読み込み
        Font coolFont = Resources.Load<Font>("Fonts/Audiowide") ?? Resources.Load<Font>("Fonts/BlackOpsOne");
        if (coolFont != null)
        {
            if (ScoreText != null) ScoreText.font = coolFont;
            if (ComboText != null) ComboText.font = coolFont;
        }

        // ポーズ画面の動的生成
        BuildPausePanel(coolFont);
    }

    /// 入力: displayScore | 出力: なし | 副作用: ScoreTextの文字列更新
    public void UpdateScoreUI(float displayScore)
    {
        // テキストUIがアタッチされているか確認（NullReferenceException防止）
        if (ScoreText != null) 
        {
            // リッチテキストタグを使って数値部分だけを大きく表示し、小数点以下は切り捨てて表示
            ScoreText.text = "SCORE\n<size=60>" + Mathf.FloorToInt(displayScore) + "</size>";
        }
    }

    /// 入力: combo | 出力: なし | 副作用: ComboTextの文字列更新と表示状態切り替え
    public void UpdateComboUI(int combo)
    {
        // テキストUIがアタッチされているか確認
        if (ComboText != null) 
        {
            // コンボが2以上の場合のみ画面に表示してプレイヤーを煽る
            if (combo > 1) 
            {
                ComboText.text = "COMBO\n<size=80>x" + combo + "</size>";

                // コンボ数に応じた色の変化とアニメーション設定
                UIAnimator animator = ComboText.GetComponent<UIAnimator>();
                if (animator != null)
                {
                    // 色数制限ルールに従い、コンボテキストの色はすべてアクセントカラー（ネオンピンク）で統一
                    animator.RainbowColor = false;
                    animator.ColorFade = true;
                    animator.TargetColor = new Color(1f, 0.2f, 0.4f); // #FF3366

                    // 代わりに、20コンボ以上の場合はアニメーションの脈動を激しくして豪華さを表現
                    if (combo >= 20)
                    {
                        animator.PulseSpeed = 10f;
                    }
                    else
                    {
                        animator.PulseSpeed = 6f;
                    }
                }

                // 10コンボごとの節目で画面を揺らして爽快感を演出
                if (combo % 10 == 0)
                {
                    if (CameraShake.Instance != null)
                    {
                        // 通常のヒット時より少し強め程度のシェイク（滑らかさのため強度を抑える）
                        CameraShake.Instance.TriggerShake(0.08f, 0.2f);
                    }
                    // 【追加】コンボ達成時の3Dグロウスパーク花火エフェクトを生成
                    SpawnComboFireworks(combo);
                }
                
                // 【追加】毎回コンボ更新時にUIショックウェーブを発生させてクールな演出を追加
                SpawnComboShockwave(combo);
            }
            else 
            {
                // コンボが途切れた、または1回目の場合はテキストを空にして隠す
                ComboText.text = "";
                UIAnimator animator = ComboText.GetComponent<UIAnimator>();
                if (animator != null) 
                {
                    animator.RainbowColor = false;
                    animator.ColorFade = true;
                }
            }
        }
    }

    /// 入力: combo | 出力: なし | 副作用: 動的ParticleSystemの生成
    /// コンボ節目で画面中央からトレイル付きの3Dグロウスパーク花火を放射する
    private void SpawnComboFireworks(int combo)
    {
        // UIの裏（または奥）で再生する花火オブジェクト
        GameObject fireworkObj = new GameObject("ComboFireworks");
        fireworkObj.transform.position = new Vector3(0, 0, 5f); // 画面中央の奥
        ParticleSystem ps = fireworkObj.AddComponent<ParticleSystem>();
        
        // 再生中にパラメータを変更するとエラーになる仕様への回避策として一時停止
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.gravityModifier = 0.5f; // 微小な重力で火の粉のように漂わせる
        
        // トーン統一ルールに従い、アクセントカラー（ネオンピンク）とメインカラー（シアン）のグラデーションに限定
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.4f), new Color(0f, 0.9f, 1f));

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)Mathf.Min(combo * 2, 100)) }); // コンボ数に応じた数の火花

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere; // 立体的に放射
        shape.radius = 1f;

        var sizeOverTime = ps.sizeOverLifetime;
        sizeOverTime.enabled = true;
        sizeOverTime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // トレイル（直線エフェクト）は使用せず、純粋な光の粒（スパーク）にする
        var trails = ps.trails;
        trails.enabled = false;

        // ノイズを少し足して火の粉のようなランダムな揺らぎを与える
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 1.0f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // 光のテクスチャを割り当て
        Material glowMat = ParticleMaterialUtils.GetGlowMaterial();
        renderer.material = glowMat;
        renderer.trailMaterial = glowMat;

        ps.Play();
        Destroy(fireworkObj, 2f);
    }

    /// 入力: なし | 出力: なし | 副作用: アニメーション用コルーチンの起動
    /// 重複起動によるスケール異常はコルーチン側で上書きされるため許容
    public void TriggerComboAnimation()
    {
        // 非同期にアニメーションを実行するコルーチンを呼び出す
        StartCoroutine(BumpComboText());
    }

    /// 入力: なし | 出力: IEnumerator | 副作用: ComboTextのローカルスケールと回転の変更
    private IEnumerator BumpComboText()
    {
        // 対象のUIがなければ処理を即座に終了
        if (ComboText == null) yield break;
        
        float duration = 0.5f; // アニメーションにかける時間
        float time = 0;
        
        // 毎回ランダムな角度に少し傾けることで、型破りでダイナミックな印象を与える
        float randomZ = Random.Range(-15f, 15f);
        Quaternion startRot = Quaternion.Euler(0, 0, randomZ);
        
        while(time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            // Ease-Out-Elasticの計算式を利用し、単なる拡大縮小ではなく「ビヨンッ！」と跳ねるようなユニークな動きを実装
            float p = 0.3f;
            float scaleMultiplier = Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
            
            // 計算したスケールをテキストUIに適用
            float finalScale = ComboScaleMin + (ComboScaleMax - ComboScaleMin) * (scaleMultiplier - 1f);
            ComboText.transform.localScale = new Vector3(finalScale, finalScale, 1f);
            
            // 角度も徐々に元に戻す
            ComboText.transform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, t * 2f);
            
            // 1フレーム処理を待機
            yield return null;
        }
        
        // アニメーション完了時に、確実に元のサイズ・角度へ戻すためのセーフティ処理
        ComboText.transform.localScale = Vector3.one;
        ComboText.transform.localRotation = Quaternion.identity;
    }

    /// 入力: combo | 出力: なし | 副作用: UI上に広がる四角いショックウェーブ枠を生成
    private void SpawnComboShockwave(int combo)
    {
        if (ComboText == null) return;
        
        GameObject waveObj = new GameObject("ComboShockwave", typeof(RectTransform));
        waveObj.transform.SetParent(ComboText.transform.parent, false);
        waveObj.transform.position = ComboText.transform.position;
        
        Image img = waveObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // 中身は透明
        
        Outline outline = waveObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.9f, 1f, 1f); // エレクトリックシアン
        outline.effectDistance = new Vector2(5, -5);
        
        RectTransform rect = waveObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 200); // ComboTextを囲む初期サイズ
        
        StartCoroutine(AnimateShockwave(waveObj, combo));
    }

    /// ショックウェーブを拡大しながらフェードアウトさせるコルーチン
    private IEnumerator AnimateShockwave(GameObject waveObj, int combo)
    {
        float duration = 0.4f;
        float time = 0;
        RectTransform rect = waveObj.GetComponent<RectTransform>();
        Outline outline = waveObj.GetComponent<Outline>();
        
        // コンボ数が高いほどウェーブが大きく広がるダイナミック演出
        float endScale = 1.5f + (combo * 0.02f); 
        
        while(time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            // Ease-out (素早く広がり、ゆっくり消える)
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);
            
            // スケール拡大
            float scale = Mathf.Lerp(1f, endScale, easeOut);
            rect.localScale = new Vector3(scale, scale, 1f);
            
            // 透明度をフェードアウト
            Color c = outline.effectColor;
            c.a = 1f - easeOut;
            outline.effectColor = c;
            
            yield return null;
        }
        
        Destroy(waveObj);
    }

    /// 入力: timeLeft, maxTime, isWarning | 出力: なし | 副作用: TimeProgressCircleのfillAmountと色変更
    public void UpdateTimeUI(float timeLeft, float maxTime, bool isWarning)
    {
        // ゲージUIがアタッチされていなければ何もしない
        if (TimeProgressCircle == null) return;

        // 最大時間に対する残り時間の割合（0.0〜1.0）を計算し、円形ゲージの表示量に適用
        float ratio = timeLeft / maxTime;
        TimeProgressCircle.fillAmount = ratio;

        // 無用な色（イエローなど）を排除し、メインカラー（シアン）とアクセントカラー（ピンク）のみで状態を表現
        if (ratio <= 0.2f)
        {
            // 20%以下：ネオンピンクで激しく点滅（滑らかなサイン波を使用）
            float currentBlinkSpeed = WarningBlinkSpeed * (1f + (0.2f - ratio) * 5f);
            float pingPong = (Mathf.Sin(Time.time * currentBlinkSpeed) + 1f) * 0.5f;
            Color neonPink = new Color(1f, 0.2f, 0.4f);
            Color darkPink = new Color(0.5f, 0.1f, 0.2f); // 色の休憩所として少し暗いトーンを挟む
            TimeProgressCircle.color = Color.Lerp(darkPink, neonPink, pingPong);
        }
        else if (ratio <= 0.5f)
        {
            // 50%以下：シアンからネオンピンクへ徐々に色が変化
            float colorLerp = (ratio - 0.2f) / 0.3f; // 0.2〜0.5を0〜1に正規化
            Color electricCyan = new Color(0f, 0.9f, 1f);
            Color neonPink = new Color(1f, 0.2f, 0.4f);
            TimeProgressCircle.color = Color.Lerp(neonPink, electricCyan, colorLerp);
        }
        else
        {
            // 通常時はメインカラー（エレクトリックシアン）に固定
            TimeProgressCircle.color = new Color(0f, 0.9f, 1f);
        }
    }

    /// 入力: なし | 出力: なし | 副作用: ゲームオーバー用テキストの生成とアニメーション開始
    public void ShowGameOverUI()
    {
        // 終了時には無用なコンボ表示やスコアを隠す
        if (ScoreText != null) ScoreText.text = "";
        if (ComboText != null) ComboText.text = "";
        if (TimeProgressCircle != null) TimeProgressCircle.gameObject.SetActive(false);

        // 画面中央に大きく「TIME UP!」と表示するアニメーションUIを動的生成
        GameObject timeUpObj = new GameObject("TimeUpText");
        timeUpObj.transform.SetParent(ScoreText.transform.parent, false); // Canvasの子要素として配置
        
        Text timeUpText = timeUpObj.AddComponent<Text>();
        timeUpText.text = "TIME UP!";
        timeUpText.font = ScoreText.font; // スコアと同じフォント（Impact）を使用
        timeUpText.fontSize = 150;
        timeUpText.color = new Color(1f, 0.2f, 0.4f); // アクセントカラー：ネオンピンク
        timeUpText.alignment = TextAnchor.MiddleCenter;
        timeUpText.supportRichText = true;

        // 文字の視認性を高めるための太い縁取りと影
        Outline outline = timeUpObj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(5f, -5f);
        Shadow shadow = timeUpObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(10f, -10f);

        // 中央に配置
        RectTransform rect = timeUpObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1000, 400);

        // 初期状態はスケール0（見えない状態）
        timeUpObj.transform.localScale = Vector3.zero;

        // コルーチンでポップアップアニメーションを実行
        StartCoroutine(AnimateTimeUpText(timeUpObj.transform));
    }

    /// ポップアップ用のイージングアニメーション（Ease-Out-Back）
    private IEnumerator AnimateTimeUpText(Transform targetTransform)
    {
        float time = 0f;
        float duration = 0.5f; // 0.5秒かけてポップアップ

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            
            // Ease-Out-Backの計算式（少し大きくなってから戻る、弾力のある動き）
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            if (t >= 1f) scale = 1f; // 最終的に1.0で固定

            targetTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // ポップアップ完了後、ゆっくりと脈動させる
        UIAnimator animator = targetTransform.gameObject.AddComponent<UIAnimator>();
        animator.PulseAmount = 0.05f;
        animator.PulseSpeed = 2f;
    }

    /// 入力: show | 出力: なし | 副作用: ポーズパネルのアクティブ切り替えとカーソル表示
    public void ShowPauseMenu(bool show)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(show);
        }

        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// 入力: font | 出力: なし | 副作用: ポーズメニューUIの動的生成
    private void BuildPausePanel(Font font)
    {
        pausePanel = new GameObject("PausePanel", typeof(RectTransform));
        pausePanel.transform.SetParent(ScoreText != null ? ScoreText.transform.parent : transform, false);
        
        Image bgImg = pausePanel.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.9f);
        
        RectTransform rect = pausePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        // タイトル文字
        GameObject titleObj = new GameObject("PauseTitle", typeof(RectTransform));
        titleObj.transform.SetParent(pausePanel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "PAUSED";
        titleText.font = font;
        titleText.fontSize = 100;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        Outline outline = titleObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);

        titleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);
        titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 200);

        // 感度スライダー
        CreateSliderField("MouseSensitivity", "AIM SENSITIVITY", new Vector2(0, 0), font);

        // RESUMEボタン
        GameObject btnObj = new GameObject("ResumeButton", typeof(RectTransform));
        btnObj.transform.SetParent(pausePanel.transform, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        Button resumeBtn = btnObj.AddComponent<Button>();
        SetButtonHoverState(resumeBtn);
        resumeBtn.onClick.AddListener(() => {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        });
        
        btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);

        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "RESUME";
        btnText.font = font;
        btnText.fontSize = 50;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        
        Outline btnOutline = btnTextObj.AddComponent<Outline>();
        btnOutline.effectColor = Color.black;
        btnOutline.effectDistance = new Vector2(2f, -2f);
        
        btnTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);
        btnTextObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // 初期状態は非表示
        pausePanel.SetActive(false);
    }

    private void CreateSliderField(string name, string label, Vector2 position, Font font)
    {
        GameObject sensPanelObj = new GameObject(name, typeof(RectTransform));
        sensPanelObj.transform.SetParent(pausePanel.transform, false);
        sensPanelObj.GetComponent<RectTransform>().anchoredPosition = position;
        sensPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);

        GameObject sensTextObj = new GameObject("Label", typeof(RectTransform));
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
        
        sensTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 50);
        sensTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);

        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform));
        sliderObj.transform.SetParent(sensPanelObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 20);
        sliderObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
        
        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        bgObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.25f);
        bgObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.75f);
        bgObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject fillAreaObj = new GameObject("FillArea", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.cyan;
        fillObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject handleAreaObj = new GameObject("HandleSlideArea", typeof(RectTransform));
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleAreaObj.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform));
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = Color.white;
        handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 0);

        slider.targetGraphic = handleImg;
        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.handleRect = handleObj.GetComponent<RectTransform>();
        slider.minValue = 0.05f;
        slider.maxValue = 10.0f;
        slider.value = GameSettings.MouseSensitivity;

        sensText.text = label + ": " + slider.value.ToString("F2");

        slider.onValueChanged.AddListener((val) => {
            GameSettings.MouseSensitivity = val;
            PlayerPrefs.SetFloat("MouseSensitivity", val);
            PlayerPrefs.Save(); // 確実な保存のためにSave()を追加
            sensText.text = label + ": " + val.ToString("F2");
        });
    }

    private void SetButtonHoverState(Button button)
    {
        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colorBlock.highlightedColor = new Color(2f, 2f, 2f, 1f);
        colorBlock.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colorBlock.selectedColor = new Color(2f, 2f, 2f, 1f);
        button.colors = colorBlock;
    }
}
