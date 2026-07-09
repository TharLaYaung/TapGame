using UnityEngine;

public enum ShapeType { Standard, Bonus }

/// 個別の的オブジェクトに対するヒット判定とライフサイクルを管理。
/// オブジェクト指向設計による責務分散のため、マネージャーへの依存を最小限に抑える。
public class ShapeTarget : MonoBehaviour
{
    // メモリリーク防止のための自動消滅期限
    public float 生存時間 = 2.0f;

    public ShapeType 形状タイプ = ShapeType.Standard;
    private float 経過時間 = 0f;
    private Vector3 基本スケール;

    // パフォーマンス低下を防ぐため、共有メッシュを静的キャッシュ
    private static Mesh[] standardMeshes = null;

    private void Awake()
    {
        // 2Dコライダーが3D回転時にペラペラになり判定が消失する不具合を回避
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            // 次のAddComponentエラーを防ぐため即座に破棄
            DestroyImmediate(col2D);
        }
        
        if (GetComponent<Collider>() == null)
        {
            // モバイルの操作性を考慮し、的より少し大きめのタップ判定を付与
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.6f;
        }
    }

    /// 入力: タイプ(ShapeType) | 出力: なし | 副作用: メッシュの再割り当てと初期化
    /// プールから再利用される際の初期化処理
    public void Initialize(ShapeType タイプ)
    {
        形状タイプ = タイプ;
        経過時間 = 0f;
        基本スケール = transform.localScale;

        transform.localScale = Vector3.zero;

        if (タイプ == ShapeType.Standard)
        {
            if (standardMeshes == null)
            {
                // リソースロードのオーバーヘッドを避けるためプリミティブ等から動的生成
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                standardMeshes = new Mesh[] 
                { 
                    sphere.GetComponent<MeshFilter>().sharedMesh,
                    ShapeMeshGenerator.CreateDiamondMesh()
                };
                
                Destroy(sphere);
            }

            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null)
            {
                // 単調さを防ぐためのランダム形状割り当て
                filter.sharedMesh = standardMeshes[Random.Range(0, standardMeshes.Length)];
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            // ゲームオーバー時の残留を防ぐための強制縮小アニメーション
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 10f);
            transform.Rotate(new Vector3(30f, 60f, 90f) * Time.deltaTime * 3f);
            return;
        }

        経過時間 += Time.deltaTime;

        float 出現時間 = 0.25f;

        if (経過時間 < 出現時間)
        {
            // ポップアップ時のオーバーシュートを演出するイージング計算
            float t = 経過時間 / 出現時間;
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easedT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            transform.localScale = 基本スケール * easedT;
        }
        else
        {
            // 位相ジャンプを防ぐため、Time.timeではなく経過時間を基準に脈動を計算
            float 脈動スケール = Mathf.Sin((経過時間 - 出現時間) * 8f) * 0.05f;
            transform.localScale = 基本スケール + new Vector3(脈動スケール, 脈動スケール, 脈動スケール);
        }

        // 3Dライティングの陰影変化を強調するための定常回転
        transform.Rotate(new Vector3(15f, 30f, 45f) * Time.deltaTime);

        if (経過時間 >= 生存時間)
        {
            if (GameManager.Instance != null && (形状タイプ == ShapeType.Standard || 形状タイプ == ShapeType.Bonus))
            {
                // 見逃しペナルティとしてコンボを強制リセット
                if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterMiss();
            }
            プールへ返却();
        }
    }

    /// 入力: なし | 出力: なし | 副作用: ヒット処理の実行
    /// ボム等からの物理タップエミュレート用インターフェース
    public void 強制ヒット()
    {
        ヒット処理();
    }

    private void OnMouseDown()
    {
        ヒット処理();
    }

    /// 入力: なし | 出力: なし | 副作用: スコア加算、SE再生、エフェクト生成、オブジェクトのプーリング
    /// タップ判定完了後の共通リザルト処理
    private void ヒット処理()
    {
        // 意図しないスコア加算を防ぐためのゲーム状態チェック
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (GameManager.Instance != null || ScoreManager.Instance != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(形状タイプ == ShapeType.Bonus ? 1 : 0);

            // プレイヤーの反射神経への報酬として、早くタップするほど高得点にする
            float 速度ボーナス = Mathf.Clamp01(1f - (経過時間 / 生存時間));
            float サイズ倍率 = 基本スケール.x;

            if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterHit(速度ボーナス, 形状タイプ == ShapeType.Bonus, サイズ倍率);

            // 浮遊スコアとして表示するための逆算処理（ScoreManager仕様依存）
            int 獲得ポイント = Mathf.RoundToInt(100 * サイズ倍率);
            Color scoreColor = 形状タイプ == ShapeType.Bonus ? new Color(1f, 0.2f, 0.4f) : new Color(0f, 0.9f, 1f);
            スコアテキスト生成("+" + 獲得ポイント, scoreColor, 0.4f);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(0.05f, 0.2f);
        }

        パーティクル生成();
        プールへ返却();
    }

    /// 入力: 文字列, 色, 速度 | 出力: なし | 副作用: 浮遊UIの生成
    private void スコアテキスト生成(string 文字列, Color 色, float アニメ速度 = 0.4f)
    {
        GameObject テキストオブジェクト = new GameObject("FloatingScore");
        テキストオブジェクト.transform.position = transform.position;
        FloatingText テキストコンポーネント = テキストオブジェクト.AddComponent<FloatingText>();
        テキストコンポーネント.Setup(文字列, 色, アニメ速度);
    }

    /// 入力: なし | 出力: なし | 副作用: 3Dパーティクルエフェクトの生成
    private void パーティクル生成()
    {
        GameObject パーティクルオブジェクト = new GameObject("PopParticles");
        パーティクルオブジェクト.transform.position = transform.position;
        ParticleSystem パーティクルシステム = パーティクルオブジェクト.AddComponent<ParticleSystem>();
        
        // 再生前のパラメータ変更によるエラーを回避するための停止
        パーティクルシステム.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = パーティクルシステム.main;
        // 画面全体に広がらず、局所的に散るよう寿命を短く設定
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); 
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        // 火の粉のような浮遊感を出すための微小な重力
        main.gravityModifier = 0.5f;
        main.loop = false;
        main.playOnAwake = false;

        var sizeOverLifetime = パーティクルシステム.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, 0.0f);

        MeshRenderer レンダラー = GetComponent<MeshRenderer>();
        if (レンダラー != null) main.startColor = レンダラー.sharedMaterial.color;

        var emission = パーティクルシステム.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = パーティクルシステム.shape;
        // 立体的に全方位へ飛ばすためのSphere型指定
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var trails = パーティクルシステム.trails;
        trails.enabled = false;

        // 破片のランダムな揺らぎを演出
        var noise = パーティクルシステム.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 1.0f;

        var systemRenderer = パーティクルシステム.GetComponent<ParticleSystemRenderer>();
        systemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            // リアルな破壊表現のため、対象と同一のメッシュを適用
            systemRenderer.mesh = meshFilter.sharedMesh;
        }
        else
        {
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            systemRenderer.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempCube);
        }

        if (レンダラー != null)
        {
            // 破壊された質感を維持するためマテリアルを引き継ぐ
            systemRenderer.material = レンダラー.sharedMaterial;
        }

        // 自然な破片の振る舞いを模倣するための3D回転
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0, 360f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0, 360f);
        
        var rotOverTime = パーティクルシステム.rotationOverLifetime;
        rotOverTime.enabled = true;
        rotOverTime.separateAxes = true;
        rotOverTime.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
        rotOverTime.y = new ParticleSystem.MinMaxCurve(-3f, 3f);
        rotOverTime.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        パーティクルシステム.Play();
        Destroy(パーティクルオブジェクト, 1.5f);
    }

    /// 入力: なし | 出力: なし | 副作用: オブジェクトの非アクティブ化または破棄
    private void プールへ返却()
    {
        if (ShapePool.Instance != null)
        {
            // オブジェクト再生成コストを抑えるためのオブジェクトプール返却
            ShapePool.Instance.ReturnShape(gameObject, 形状タイプ);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}