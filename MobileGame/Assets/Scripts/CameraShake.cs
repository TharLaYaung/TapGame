using UnityEngine;


/// タップ成功/失敗時のフィードバック用カメラシェイクエフェクト
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; } // 他スクリプトからの呼び出し用インスタンス

    // --- 定数（揺れの標準設定） ---
    private const float DefaultShakeDuration = 0.1f;   // デフォルトの揺れ継続時間
    private const float DefaultShakeMagnitude = 0.15f; // デフォルトの揺れの大きさ（振幅）

    // --- 状態変数 ---
    private Vector3 originalPosition;                  // 揺れる前のカメラの本来の座標
    private float shakeDuration = 0f;                  // 現在の揺れ残り時間
    private float shakeMagnitude = 0.1f;               // 現在の揺れの大きさ

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
        // 初期座標を記憶し、シェイク終了後に戻せるようにする
        originalPosition = transform.localPosition;
    }

    private void Update()
    {
        // 揺れ残り時間が0より大きい場合はシェイク処理を継続
        if (shakeDuration > 0)
        {
            // 半径1の球体内のランダムな座標に揺れの大きさを掛けて現在位置を加算
            transform.localPosition = originalPosition + Random.insideUnitSphere * shakeMagnitude;
            // 2D用Orthographicカメラの描画クリッピングを防ぐためZ軸のみ元座標に固定する
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, originalPosition.z);
            
            // 前のフレームからの経過時間を残り時間から減算
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            // 揺れ時間が終わった場合はタイマーを0に固定
            shakeDuration = 0f;
            // カメラの位置を元の座標に正確に戻す
            transform.localPosition = originalPosition;
        }
    }
    
    /// カメラシェイクの開始
    /// 入力: duration (継続時間), magnitude (振幅)
    /// 副作用: 内部タイマーを上書きし、Updateループ内で座標をランダムに揺らす
    public void TriggerShake(float duration = DefaultShakeDuration, float magnitude = DefaultShakeMagnitude)
    {
        // 要求された揺れの時間を設定
        shakeDuration = duration;
        // 要求された揺れの大きさを設定
        shakeMagnitude = magnitude;
    }
}
