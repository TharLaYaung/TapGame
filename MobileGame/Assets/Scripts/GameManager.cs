using UnityEngine;

/// 状態遷移の競合を防ぐためゲーム全体の進行を単一管理する
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    // ゲーム中の残り時間（秒）を管理する変数
    public float TimeLeft = 30f;
    // 二重のゲームオーバー処理（多重音再生やUI重複）を防ぐための排他ロック
    public bool IsGameOver = false;
    // ゲームがポーズ中かどうかを管理するフラグ
    public bool IsPaused = false;
    // UIのプログレスバー割合を動的計算するための分母値として初期の残り時間をキャッシュ
    private float maxTime = 30f;

    // プレイヤーに焦りを感じさせプレイングを急がせるための警告閾値（残り時間がこれを下回ると警告演出開始）
    private const float WarningTimeThreshold = 5f;
    // ゲームオーバー直後の不快な画面急遷移を防ぐためのUI確認猶予（フェード遷移時）
    private const float ResultFadeDelay = 1.5f;
    // ゲームオーバー直後の不快な画面急遷移を防ぐためのUI確認猶予（通常遷移時）
    private const float ResultLoadDelay = 2.0f;
    
    // タイマー警告音の連続再生を防ぐための直前の秒数キャッシュ
    private int lastWarningSeconds = -1;

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
            // 重複したインスタンスを破棄
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // プログレスバーの最大値計算用に初期時間を保存
        maxTime = TimeLeft;
        
        // 画面遷移演出用スクリプトが存在しない場合は動的生成（NullReference回避）
        if (FindObjectOfType<SceneFader>() == null)
        {
            new GameObject("SceneFader").AddComponent<SceneFader>();
        }

        // BGM/SE管理スクリプトが存在しない場合は動的生成（NullReference回避）
        if (FindObjectOfType<AudioManager>() == null)
        {
            GameObject audioObject = new GameObject("AudioManager");
            audioObject.AddComponent<AudioSource>();
            audioObject.AddComponent<AudioManager>();
        }

        // PC向けエイム操作用のカスタム照準コントローラーが存在しない場合は動的生成（NullReference回避）
        if (FindObjectOfType<AimController>() == null)
        {
            GameObject aimObject = new GameObject("AimController");
            aimObject.AddComponent<AimController>();
        }
    }

    private void Update()
    {
        // 既にゲームオーバー状態なら後続の更新処理をすべてスキップ
        if (IsGameOver) return;

        // Escキーでポーズ画面の切り替え
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // ポーズ中は時間の進行と後続処理をストップ
        if (IsPaused) return;

        // 前フレームからの経過時間を残り時間から減算
        TimeLeft -= Time.deltaTime;
        
        // 残り時間がゼロ以下になった場合の処理
        if (TimeLeft <= 0)
        {
            // マイナス表示を防ぐためタイマーを0に固定
            TimeLeft = 0;
            // ゲームオーバー処理を実行
            GameOver();
        }

        // 残り時間が閾値を下回ったかどうかの真偽値を計算
        bool isWarning = (TimeLeft < WarningTimeThreshold);

        // 警告状態で残り時間が1秒以上ある場合、毎秒（整数秒を跨いだ瞬間に）警告音を再生する
        if (isWarning)
        {
            int currentSeconds = Mathf.CeilToInt(TimeLeft);
            if (currentSeconds != lastWarningSeconds && currentSeconds > 0)
            {
                lastWarningSeconds = currentSeconds;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(4);
            }
        }

        // UIManagerが存在する場合はUIの更新を依頼
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimeUI(TimeLeft, maxTime, isWarning);
        }
    }

      /// 入力: なし | 出力: なし | 副作用: IsGameOverフラグの起立、他コンポーネントの停止、画面遷移の予約
    /// 時間切れ時に一度だけ呼ばれ、ゲーム進行を完全に停止させる 
    private void GameOver()
    {
        // 状態をゲームオーバーに固定し、Updateをロック
        IsGameOver = true;
        
        // タイムアップのブザー音を再生（ID=3）
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(3);
        
        // スコアの最終計算と保存をScoreManagerに依頼
        if (ScoreManager.Instance != null) ScoreManager.Instance.FinalizeScore();
        
        // ゲームオーバー時の「GAME OVER」テキスト表示などをUIManagerに依頼
        if (UIManager.Instance != null) UIManager.Instance.ShowGameOverUI();
        
        // 新規の的オブジェクトが生成されるのを防ぐためSpawnerを無効化
        ShapeSpawner shapeSpawner = FindObjectOfType<ShapeSpawner>();
        if (shapeSpawner != null) shapeSpawner.enabled = false;
        
        // シーン遷移用の遅延実行（フェードアウトが存在するかで分岐）
        if (SceneFader.Instance != null)
        {
            Invoke("FadeToResult", ResultFadeDelay);
        }
        else
        {
            Invoke("LoadResultScene", ResultLoadDelay);
        }
    }

    // 遅延実行用のフェードアウト呼び出し関数
    private void FadeToResult()
    {
        SceneFader.Instance.FadeAndLoadScene("Result");
    }

    // フェーダーが存在しない場合の直接遷移用関数
    private void LoadResultScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
    }

    /// 入力: なし | 出力: なし | 副作用: IsPausedの切り替え、Time.timeScaleの変更、UIManagerへの通知
    public void TogglePause()
    {
        // ゲームオーバー時はポーズ不可
        if (IsGameOver) return;

        IsPaused = !IsPaused;

        if (IsPaused)
        {
            Time.timeScale = 0f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPauseMenu(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(0);
        }
        else
        {
            Time.timeScale = 1f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPauseMenu(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(0);
        }
    }
}
