using UnityEngine;


/// スコア獲得時などのポップアップテキスト演出
public class FloatingText : MonoBehaviour
{
    // --- 定数（演出設定） ---
    private const float DefaultLifetime = 1.0f;     // 表示が消えるまでの時間（秒）
    private const float DefaultMoveSpeedY = 2.0f;   // 浮かび上がる速度（Y軸）
    private const int TextSortingOrder = 100;       // 最前面に描画するための重なり順

    // --- 状態変数 ---
    // 実際に文字を描画する3Dテキストコンポーネント（動的にアタッチされる）
    private TextMesh textMesh;
    // 消滅するまでの残り寿命（セットアップ時に変更可能だがデフォルト値で初期化）
    private float lifetime = DefaultLifetime;
    // 生成されてからの経過時間（フェードアウトの進行度計算用）
    private float age = 0f;
    // 上方向にテキストが浮上していく速度ベクトル
    private Vector3 moveSpeed = new Vector3(0, DefaultMoveSpeedY, 0);

    /// ポップアップテキストの生成と初期化
    /// 入力: textContent (表示文字列), textColor (文字色), textSize (文字サイズ)
    /// 副作用: TextMeshコンポーネントを動的追加し、指定時間後に自身を破棄するよう設定
    public void Setup(string textContent, Color textColor, float textSize = 0.3f)
    {
        // オブジェクトにTextMeshコンポーネントを追加し、テキスト情報を割り当て
        textMesh = gameObject.AddComponent<TextMesh>();
        // 表示する文字列を設定
        textMesh.text = textContent;
        // 文字の色を設定
        textMesh.color = textColor;
        // 文字のサイズを設定
        textMesh.characterSize = textSize;
        // テキストの配置基準点を中央に設定
        textMesh.anchor = TextAnchor.MiddleCenter;
        // 複数行になった場合の中央揃えを設定
        textMesh.alignment = TextAlignment.Center;
        
        // クールな外部フォント（Audiowide等）を優先的に読み込み、無ければLegacyRuntimeへフォールバック
        Font fallbackFont = Resources.Load<Font>("Fonts/Audiowide") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // テキストメッシュにフォントを適用
        textMesh.font = fallbackFont;
        // レンダラーのマテリアルもフォントのデフォルトマテリアルに置き換え
        GetComponent<MeshRenderer>().material = fallbackFont.material;
        
        // ターゲットやエフェクトに埋もれないよう最前面のソーティングオーダーを強制
        GetComponent<MeshRenderer>().sortingOrder = TextSortingOrder;

        // 設定された寿命時間後にこのオブジェクト自体を自動的に破棄するよう登録
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // 前フレームからの経過時間を足して生存時間を更新
        age += Time.deltaTime;
        // 上昇速度に経過時間を掛けて、テキストの座標を上に移動させる
        transform.position += moveSpeed * Time.deltaTime;
        
        // テキストコンポーネントが正しく取得できているか確認
        if (textMesh != null)
        {
            // 現在のテキスト色を取得
            Color currentColor = textMesh.color;
            // 生存時間に比例して徐々に透明にするフェードアウト演出
            currentColor.a = 1f - (age / lifetime);
            // 計算された新しいアルファ値を持つ色をテキストに再適用
            textMesh.color = currentColor;

            // 出現時にテキストが一瞬跳ねる「ポップ」アニメーションの追加（より滑らかに）
            float popScale = 1f;
            if (age < 0.3f)
            {
                // 0.0秒〜0.3秒の間で、スケールが1.0 -> 1.2 -> 1.0と変化する緩やかなサインカーブ
                popScale = 1f + Mathf.Sin((age / 0.3f) * Mathf.PI) * 0.2f;
            }
            // 計算したスケール値を適用
            transform.localScale = new Vector3(popScale, popScale, 1f);
        }
    }
}
