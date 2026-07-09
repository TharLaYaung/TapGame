using UnityEngine;

/// 状態の不整合を防ぐため、スコア計算やコンボ管理をUI・システムから分離して単一管理する
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score State")]
    // 現在の獲得スコア（実数値）
    public int Score = 0;
    // UIのスコア文字が急増するのを防ぎ、ユーザーに爽快感を与えるための内部補間用変数（表示上のスコア）
    private float displayScore = 0f;
    // 現在継続しているコンボ数
    public int Combo = 0;

    // スコア表示が実スコアに追いつくための1秒あたりの変動速度
    private const float ScoreLerpSpeed = 2000f;
    // スコアの天文学的なインフレやオーバーフローによるバグを防ぐため上限を設定（最大10倍）
    private const int MaxComboMultiplier = 10;
    // 的を1つタップした時に得られる基礎スコア
    private const int BaseScorePerHit = 100;
    // 出現からの経過時間に応じて加算されるボーナス係数
    private const int SpeedBonusMultiplier = 50;

    private void Awake()
    {
        // 複数生成によるステート破壊を防ぐためシングルトン化
        if (Instance == null)
        {
            // 最初のインスタンスを登録
            Instance = this;
        }
        else
        {
            // 既に存在する場合は破棄
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // UIManagerが存在する場合はUIの初期化を行う
        if (UIManager.Instance != null)
        {
            // スコアUIを0(displayScore)で更新
            UIManager.Instance.UpdateScoreUI(displayScore);
            // コンボUIを0(Combo)で更新
            UIManager.Instance.UpdateComboUI(Combo);
        }
    }

    private void Update()
    {
        // ゲームオーバー時はスコアの加算・更新を停止する
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // 視覚的なスコア増加演出のため、実スコア(Score)へ徐々に追従させる
        if (displayScore < Score)
        {
            // 表示スコアを実スコアを超えない範囲で増加させる
            displayScore = Mathf.Min(Score, displayScore + (Time.deltaTime * ScoreLerpSpeed));
            // 増えた表示スコアをUIに反映
            if (UIManager.Instance != null) UIManager.Instance.UpdateScoreUI(displayScore);
        }
        else if (displayScore > Score)
        {
            // 減算時の対応（現状仕様にはないがマイナス処理時のための防衛的コード）
            displayScore = Mathf.Max(Score, displayScore - (Time.deltaTime * ScoreLerpSpeed));
            // 減った表示スコアをUIに反映
            if (UIManager.Instance != null) UIManager.Instance.UpdateScoreUI(displayScore);
        }
    }

    /// 入力: speedBonus(速度ボーナス), isBonusShape(ボーナス的か), sizeMultiplier(サイズ倍率) | 出力: なし | 副作用: スコア加算、コンボ更新、UIアニメーショントリガー
    /// 的タップ時に呼び出され、コンボ数や的のサイズに応じたスコア計算を行う
    public void RegisterHit(float speedBonus, bool isBonusShape, float sizeMultiplier = 1.0f)
    {
        // ゲームオーバー時は得点加算処理を無効化
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // タップ成功なのでコンボ数を1増やす
        Combo++;
        
        // コンボ倍率や速度ボーナスを排除し、的のサイズ（サイズ倍率）のみに比例する一定スコアを計算
        // 大きな的ほどスコアが高くなり、小さな的ほどスコアが低くなる
        int points = Mathf.RoundToInt(BaseScorePerHit * sizeMultiplier);
        
        // 計算したポイントを実スコアに加算
        Score += points;

        // ボーナス的なら特別なログを出す（拡張用）
        if (isBonusShape)
        {
            // デバッグログを出力
            // Debug.Log("[System] Bonus shape hit.");
        }

        // UIManagerが存在する場合はコンボUIの更新とアニメーションを実行
        if (UIManager.Instance != null)
        {
            // 新しいコンボ数をUIに反映
            UIManager.Instance.UpdateComboUI(Combo);
            // コンボ増加時のポップアップアニメーションを発火
            UIManager.Instance.TriggerComboAnimation();
        }
    }

    /// 入力: なし | 出力: なし | 副作用: コンボの0リセット、UI更新
    /// 的の時間切れ消滅時にペナルティとしてコンボを遮断する
    public void RegisterMiss()
    {
        // ゲームオーバー時はミス判定を無視する
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        
        // コンボが途切れたことをプレイヤーに伝えるため、警告音（SE 2）を再生する
        if (Combo > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(2);
        }

        // ミスしたのでコンボをリセットする
        Combo = 0;
        // UIManagerが存在する場合はリセットされたコンボ数をUIに反映
        if (UIManager.Instance != null) UIManager.Instance.UpdateComboUI(Combo);
    }

    /// 入力: なし | 出力: なし | 副作用: 永続化データ(ScoreData)への書き込み
    public void FinalizeScore()
    {
        // 静的クラスのScoreDataに最終スコアを代入し、リザルト画面に引き継ぐ
        ScoreData.FinalScore = Score;
    }
}
