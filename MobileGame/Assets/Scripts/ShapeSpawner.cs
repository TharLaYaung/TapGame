using UnityEngine;

/// 難易度や時間経過に応じた確率分布で的を動的生成し、ゲーム進行のペースを制御する
public class ShapeSpawner : MonoBehaviour
{
    [Header("難易度設定: Easy")]
    // 初心者が画面外の的を見落とすのを防ぐため間隔を長めに設定
    [SerializeField] private float 簡単生成間隔 = 0.9f;
    // 爽快感を維持するためボーナス的を多めに配分
    [SerializeField] private float 簡単ボーナス出現率 = 0.3f;

    [Header("難易度設定: Normal")]
    // 標準的なゲームプレイのための基本生成間隔
    [SerializeField] private float 通常生成間隔 = 0.7f;
    // 標準的なゲームプレイのためのボーナス的出現確率
    [SerializeField] private float 通常ボーナス出現率 = 0.2f;

    [Header("難易度設定: Hard")]
    // パフォーマンス低下（オブジェクト過多）を防ぐため、難易度上限でも極端な短縮は避ける
    [SerializeField] private float 難しい生成間隔 = 0.45f;
    // プレイヤーのスキル差を強調するためボーナス出現率を絞る
    [SerializeField] private float 難しいボーナス出現率 = 0.1f;

    [Header("動的スケーリング")]
    // 難易度上昇が早すぎてオブジェクトが重なり、タップ不能になるバグを防ぐ下限値
    [SerializeField] private float 最小生成間隔 = 0.3f;
    // 時間経過と共に徐々に難易度を上げるための短縮幅
    [SerializeField] private float 間隔短縮率 = 0.015f;

    [Header("生成設定")]
    // 現在適用されている的の生成間隔（時間経過で減少する）
    public float 現在の生成間隔 = 1.0f;
    // 現在適用されているボーナス的の出現確率
    [Range(0f, 1f)] public float ボーナス出現率 = 0.2f;

    // 的が小さすぎて指でタップ困難になるUIUX上の制約
    public float 最小スケール = 0.5f;
    // 的が大きすぎて画面を覆い尽くし、奥の的が見えなくなるのを防ぐ制約
    public float 最大スケール = 1.5f;

    // 次の的を生成するまでの残り時間を計るための内部タイマー
    private float タイマー;

    private void Start()
    {
        // Debug.Log("[System] スポナーを初期化しました。難易度: " + GameSettings.Difficulty);

        // ユーザーの選択難易度に合わせて出現テーブルと初期速度を上書き
        switch (GameSettings.Difficulty)
        {
            case GameDifficulty.Easy:
                現在の生成間隔 = 簡単生成間隔;
                ボーナス出現率 = 簡単ボーナス出現率;
                break;
            case GameDifficulty.Hard:
                現在の生成間隔 = 難しい生成間隔;
                ボーナス出現率 = 難しいボーナス出現率;
                break;
            default:
                現在の生成間隔 = 通常生成間隔;
                ボーナス出現率 = 通常ボーナス出現率;
                break;
        }

        // 開始直後の待ち時間を排除し、プレイヤーの離脱を防ぐハック
        タイマー = 0.1f;
    }

    private void Update()
    {
        // ゲームオーバー時は的の生成を完全に停止する
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // 前フレームからの経過時間をタイマーから減算
        タイマー -= Time.deltaTime;
        // タイマーがゼロ以下になったら生成処理を実行
        if (タイマー <= 0f)
        {
            // 次の生成に向けてタイマーを現在の生成間隔にリセット
            タイマー = 現在の生成間隔;
            // 実際の生成処理を呼び出す
            的を生成();

            // 後半の難易度曲線を上げるためスポーン間隔を動的に短縮（最小値の制限付き）
            現在の生成間隔 = Mathf.Max(最小生成間隔, 現在の生成間隔 - 間隔短縮率);
        }
    }

    /// プールから的を取得し、画面内にランダム配置する
    private void 的を生成()
    {
        // 0.0〜1.0の間でランダムな値を生成し、出現確率の判定に使用
        float 乱数 = Random.value;
        // デフォルトの生成タイプを標準の的に設定
        ShapeType 生成タイプ = ShapeType.Standard;

        // 生成した乱数がボーナス出現率を下回った場合、ボーナス的に変更
        if (乱数 < ボーナス出現率)
        {
            生成タイプ = ShapeType.Bonus;
        }

        // オブジェクトプールから要求した種類の的を取り出す
        GameObject 生成した的 = ShapePool.Instance.GetShape(生成タイプ);

        // 多様な端末アスペクト比で的が画面外に見切れるバグを防ぐため動的計算
        float 画面アスペクト比 = (float)Screen.width / Screen.height;
        // カメラの縦方向の描画サイズを取得
        float カメラ高さ = Camera.main.orthographicSize;
        // 高さから横方向の描画サイズを計算
        float カメラ幅 = カメラ高さ * 画面アスペクト比;

        // 最小〜最大の範囲内でランダムなスケール値を決定
        float ランダムスケール = Random.Range(最小スケール, 最大スケール);

        // オブジェクトの半分が画面外にはみ出してタップ不可になるのを防ぐパディング
        float パディング = ランダムスケール;
        // パディングを考慮してカメラ範囲内に収まるX座標をランダムに決定
        float 位置X = Random.Range(-カメラ幅 + パディング, カメラ幅 - パディング);
        // パディングを考慮してカメラ範囲内に収まるY座標をランダムに決定
        float 位置Y = Random.Range(-カメラ高さ + パディング, カメラ高さ - パディング);

        // 生成した的の座標を決定したランダム位置に適用
        生成した的.transform.position = new Vector2(位置X, 位置Y);
        // 生成した的の大きさを決定したランダムスケールに適用
        生成した的.transform.localScale = new Vector3(ランダムスケール, ランダムスケール, 1f);

        // 生成結果をシステムログに出力
        // Debug.Log($"[System] 的 {生成タイプ} を生成: X: {位置X:F1}, Y: {位置Y:F1}.");

        // 的にアタッチされているShapeTargetコンポーネントを取得
        ShapeTarget 的コンポーネント = 生成した的.GetComponent<ShapeTarget>();
        if (的コンポーネント != null)
        {
            // コンポーネントの初期化処理を呼び出し、タイプごとの色や挙動を適用
            的コンポーネント.Initialize(生成タイプ);
        }
    }
}