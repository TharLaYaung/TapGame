using UnityEngine;

/// プレイ画面の背景色補間とアンビエントパーティクルの生成・管理を行う
[RequireComponent(typeof(Camera))]
public class BackgroundEffects : MonoBehaviour
{
    private Camera cameraComponent; // 背景色を変更するための対象カメラ参照
    private ParticleSystem ambientParticleSystem; // 動的に速度を変更するためのパーティクル参照
    private float colorProgress = 0f; // スピード変化時に色が飛ばないようにするための進行度保持
    private float currentCycleSpeed = 0f; // 現在の色サイクル速度（Lerp用）
    private Vector3 originalCameraPos;    // カメラシェイクのための初期位置保存用

    [Header("Color Animation")]
    public Color ColorA = new Color(0.02f, 0.02f, 0.05f); // 色の遷移先A
    public Color ColorB = new Color(0.08f, 0.02f, 0.12f); // 色の遷移先B
    public float ColorCycleSpeed = 0.2f;            // 色が入れ替わるサイクルの速さ

    // --- パーティクル設定 ---
    private const float DustZPosition = 5f;         // UI・ターゲットと重ならないよう手前(Z=5)に配置
    private const float DustDuration = 5f;          // パーティクルシステムのループサイクル時間
    private const float DustMinLifetime = 5f;       // 各粒子の寿命（最小）
    private const float DustMaxLifetime = 10f;      // 各粒子の寿命（最大）
    private const float DustMinSpeed = 0.05f;       // 粒子の移動速度（最小）
    private const float DustMaxSpeed = 0.2f;        // 粒子の移動速度（最大）
    private const float DustMinSize = 0.05f;        // 粒子のサイズ（最小）
    private const float DustMaxSize = 0.15f;        // 粒子のサイズ（最大）
    private const int MaxDustParticles = 200;       // 処理負荷を抑えるための最大生成数
    private const float DustEmissionRate = 20f;     // 1秒間に放出する粒子の数
    private const float CameraSizeMultiplier = 2.5f;// 画面外まで生成するための範囲倍率
    private const int DustSortingOrder = -50;       // 最背面で描画するためのソート順序

    private void Start()
    {
        cameraComponent = GetComponent<Camera>();
        originalCameraPos = transform.localPosition;
        SpawnAmbientDust();
    }

    private void Update()
    {
        // 現在のコンボ数を取得（ScoreManagerが存在しない場合は0とする）
        int currentCombo = (ScoreManager.Instance != null) ? ScoreManager.Instance.Combo : 0;

        // 目標のサイクルスピードを計算
        float targetCycleSpeed = ColorCycleSpeed * (1f + (currentCombo * 0.05f));
        // Lerpを用いて現在のスピードを滑らかに目標値へ近づける
        currentCycleSpeed = Mathf.Lerp(currentCycleSpeed, targetCycleSpeed, Time.deltaTime * 2f);

        // 進行度を毎フレーム蓄積する方式に変更し、スピード変化時のカクつきを防ぐ
        colorProgress += Time.deltaTime * currentCycleSpeed;
        
        // 残り時間が10秒を切ったら、背景を赤と黒で高速に明滅（フラッシュ）させ、画面を揺らす
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver && GameManager.Instance.TimeLeft <= 10f)
        {
            // 時間経過に伴って明滅速度を上げる（最大20f）
            float flashSpeed = 10f + (10f - GameManager.Instance.TimeLeft) * 2f;
            float flashIntensity = Mathf.PingPong(Time.time * flashSpeed, 1f);
            // 赤が強すぎないように、目に優しいダークレッドの警告色にトーンダウン
            cameraComponent.backgroundColor = Color.Lerp(new Color(0.35f, 0f, 0f), new Color(0.05f, 0f, 0f), flashIntensity);

            // カメラシェイク：残り時間が少ないほど揺れを大きくする
            float shakeAmount = (10f - GameManager.Instance.TimeLeft) * 0.03f;
            transform.localPosition = originalCameraPos + (Vector3)UnityEngine.Random.insideUnitCircle * shakeAmount;
        }
        else
        {
            // ユーザーに単調さを感じさせないよう背景色をPingPong補間で常に遷移させる
            float lerpTime = Mathf.PingPong(colorProgress, 1f);
            cameraComponent.backgroundColor = Color.Lerp(ColorA, ColorB, lerpTime);
            // カメラ位置を元に戻す
            transform.localPosition = originalCameraPos;
        }

        // パーティクルのシミュレーション速度もコンボに連動させる
        if (ambientParticleSystem != null)
        {
            var mainModule = ambientParticleSystem.main;
            // 速度が上がりすぎないよう最大2.5倍速に制限しつつ、Lerpで滑らかに変化
            float targetSimSpeed = Mathf.Min(2.5f, 1f + (currentCombo * 0.03f));
            mainModule.simulationSpeed = Mathf.Lerp(mainModule.simulationSpeed, targetSimSpeed, Time.deltaTime * 2f);
        }
    }

    /// 入力: なし | 出力: なし | 副作用: 動的にParticleSystemを生成・設定して再生を開始する
    private void SpawnAmbientDust()
    {
        // 背景パーティクルを格納するための空のゲームオブジェクトを生成
        GameObject dustObject = new GameObject("AmbientDust");
        // 生成したオブジェクトを自身の小オブジェクトとしてヒエラルキーに配置
        dustObject.transform.SetParent(transform);
        
        // メインのゲームプレイオブジェクト（Z=0）の後ろに配置し、視認性の阻害を防ぐ
        dustObject.transform.localPosition = new Vector3(0, 0, DustZPosition);
        
        // オブジェクトにParticleSystemコンポーネントを追加してパーティクル機能を持たせる
        ambientParticleSystem = dustObject.AddComponent<ParticleSystem>();
        
        // 再生中にパラメータを変更するとUnity側でエラーを吐く仕様への回避策として一時停止
        ambientParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // メインモジュールの参照を取得し、パーティクルの基本寿命や速度などの基礎設定を行う
        var mainModule = ambientParticleSystem.main;
        // ループ１回分のシステム継続時間を指定
        mainModule.duration = DustDuration;
        // パーティクルの生成を継続してループさせる設定を有効化
        mainModule.loop = true;
        // パーティクルの生存時間（寿命）を最小値と最大値のランダム範囲に設定
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(DustMinLifetime, DustMaxLifetime);
        // パーティクルの初期速度を最小値と最大値のランダム範囲に設定
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(DustMinSpeed, DustMaxSpeed);
        // パーティクルの初期サイズを最小値と最大値のランダム範囲に設定
        mainModule.startSize = new ParticleSystem.MinMaxCurve(DustMinSize, DustMaxSize);
        // パーティクルの発生時の初期色を半透明のシアン（水色）に設定
        mainModule.startColor = new Color(0.2f, 1f, 1f, 0.2f);
        // パーティクルの最大同時生成数を設定し、処理落ちを防止
        mainModule.maxParticles = MaxDustParticles;
        // 実行直後から画面いっぱいにパーティクルを満たすための事前シミュレーション設定
        mainModule.prewarm = true;
        
        // エミッション（放出）モジュールの参照を取得し、生成ペースを設定
        var emissionModule = ambientParticleSystem.emission;
        // 1秒あたりのパーティクル生成数（放出量）を指定
        emissionModule.rateOverTime = DustEmissionRate;
        
        // シェイプ（発生範囲）モジュールの参照を取得し、画面全体に散らすための設定を行う
        var shapeModule = ambientParticleSystem.shape;
        // 発生領域の形状をボックス型（直方体）に指定
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        // 端末ごとの画面アスペクト比（縦横比）を計算して動的に取得
        float screenAspect = (float)Screen.width / Screen.height;
        // 画面端でのパーティクル消失（クリッピング）を見せないためカメラサイズより広めに確保
        float cameraHeight = cameraComponent.orthographicSize * CameraSizeMultiplier;
        // 高さにアスペクト比を掛けて、描画領域の幅を算出
        float cameraWidth = cameraHeight * screenAspect;
        // 算出した幅と高さを用いて、パーティクル発生ボックスのサイズ（スケール）を適用
        shapeModule.scale = new Vector3(cameraWidth, cameraHeight, 1f);
        
        // フォース（外部からかかる力）モジュールの参照を取得し、微風のような揺らぎを表現
        var forceModule = ambientParticleSystem.forceOverLifetime;
        // 寿命に沿って力を加える機能を有効化
        forceModule.enabled = true;
        // X軸方向（左右）にランダムな微弱な力を加えて揺らぎを表現
        forceModule.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        // Y軸方向（上下）に上向きの微弱な力を加えて上昇気流を表現
        forceModule.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        
        // カラーオーバーライフタイム（寿命に応じた色変化）モジュールの参照を取得
        var colorOverLifetimeModule = ambientParticleSystem.colorOverLifetime;
        // 寿命に応じた色のフェードアウト機能を有効化
        colorOverLifetimeModule.enabled = true;
        // フェード用のグラデーションデータを新規作成
        Gradient colorGradient = new Gradient();
        // グラデーションのキーを設定（色は白のまま、アルファ値でフェードイン・フェードアウトを実現）
        colorGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        // 設定したグラデーションをモジュールに適用
        colorOverLifetimeModule.color = colorGradient;

        // レンダラー（描画用）コンポーネントの参照を取得
        var particleRenderer = ambientParticleSystem.GetComponent<ParticleSystemRenderer>();
        // 背景として一番奥に描画されるようソートオーダーを設定
        particleRenderer.sortingOrder = DustSortingOrder;
        
        // パーティクルのマテリアルにリアルなソフトグロウマテリアルを適用
        particleRenderer.material = ParticleMaterialUtils.GetGlowMaterial();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // 全ての設定を終えたパーティクルシステムの再生を再開
        ambientParticleSystem.Play();
    }
}
