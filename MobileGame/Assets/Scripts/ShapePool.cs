using System.Collections.Generic;
using UnityEngine;

/// 実行時のInstantiate/DestroyによるGCスパイクとフレーム落ちを防ぐためのオブジェクトプール
public class ShapePool : MonoBehaviour
{
    // オブジェクトプールのグローバルアクセスポイント
    public static ShapePool Instance { get; private set; }

    // 通常の的となるプレハブ（Inspectorからは隠し、スクリプトから自動割当）
    [HideInInspector] public GameObject StandardShapePrefab;
    // ボーナス的となるプレハブ（Inspectorからは隠し、スクリプトから自動割当）
    [HideInInspector] public GameObject BonusShapePrefab;
    
    // 画面内に同時出現する最大数を想定した事前生成数（標準的な的のプールサイズ）
    public int PoolSize = 20;
    // ボーナス的の出現頻度低下に合わせてメモリ消費を抑えるための除数
    private const int StandardPoolDivider = 2;

    // 再利用可能な通常の的を格納するためのキュー（先入れ先出しのデータ構造）
    private Queue<GameObject> standardShapePool = new Queue<GameObject>();
    // 再利用可能なボーナス的を格納するためのキュー
    private Queue<GameObject> bonusShapePool = new Queue<GameObject>();

    private void Awake()
    {
        // 初期化済みのプールが破棄され、確保したメモリ領域がリークするのを防ぐ
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

    /// 入力: なし | 出力: なし | 副作用: 各オブジェクトをInstantiateし、非アクティブ状態でメモリに保持
    /// ゲーム中の生成スパイクを防ぐため、シーンロード直後に一度だけ呼ばれる
    public void Initialize()
    {
        // 通常の的を指定されたプールサイズ分だけ事前生成してキューに入れる
        PopulatePool(StandardShapePrefab, standardShapePool, PoolSize);
        // ボーナス的は頻度が低いため、指定サイズの半分だけ生成してキューに入れる
        PopulatePool(BonusShapePrefab, bonusShapePool, PoolSize / StandardPoolDivider);
    }
    
    // 指定されたプレハブを上限数まで生成し、非アクティブにしてキューに格納する内部処理
    private void PopulatePool(GameObject targetPrefab, Queue<GameObject> targetPool, int sizeLimit)
    {
        // プレハブが未割り当ての場合は処理を中断（NullReference例外の防止）
        if (targetPrefab == null) return;
        // 指定された上限数に達するまでループ処理
        for (int i = 0; i < sizeLimit; i++)
        {
            // オブジェクトを自身（ShapePool）の子要素として生成
            GameObject pooledObject = Instantiate(targetPrefab, transform);
            // 最初は画面に表示させないため非アクティブにする
            pooledObject.SetActive(false);
            // 生成したオブジェクトを再利用待機キューに追加
            targetPool.Enqueue(pooledObject);
        }
    }

    /// 入力: targetType(的の種類) | 出力: GameObject(アクティブ化済み) | 副作用: キューからの要素取り出し
    /// 枯渇時には進行停止を防ぐため動的生成を行う（GCスパイクの要因になり得る）
    public GameObject GetShape(ShapeType targetType)
    {
        // 取り出す対象となるキューの参照用変数
        Queue<GameObject> targetPool;
        // 不足時に動的生成するためのプレハブ参照用変数
        GameObject targetPrefab;
        
        // 要求された的の種類に応じて対象のキューとプレハブを振り分ける
        switch (targetType)
        {
            case ShapeType.Bonus: // ボーナス的の場合
                targetPool = bonusShapePool;
                targetPrefab = BonusShapePrefab;
                break;
            default: // 通常的の場合
                targetPool = standardShapePool;
                targetPrefab = StandardShapePrefab;
                break;
        }

        // キューに待機中のオブジェクトが1つ以上存在するか確認
        if (targetPool.Count > 0)
        {
            // キューの先頭からオブジェクトを取り出す
            GameObject pooledObject = targetPool.Dequeue();
            // 画面に表示させるためアクティブにする
            pooledObject.SetActive(true);
            // 取り出したオブジェクトを呼び出し元に返す
            return pooledObject;
        }
        else if (targetPrefab != null)
        {
            // メモリ枯渇によるゲーム進行停止を避けるためフォールバックとして動的生成
            GameObject pooledObject = Instantiate(targetPrefab, transform);
            // 生成した新規オブジェクトを呼び出し元に返す
            return pooledObject;
        }
        
        // プレハブも存在しない異常系の最終フォールバック（通常は到達しない）
        return null;
    }

    /// 入力: shapeObject(返却対象), targetType(的の種類) | 出力: なし | 副作用: オブジェクトの非アクティブ化、キューへの返却
    public void ReturnShape(GameObject shapeObject, ShapeType targetType)
    {
        // 画面上から消すために非アクティブにする
        shapeObject.SetActive(false);
        // 返却対象の的の種類に応じて、適切なキューに格納（再利用待機状態）する
        switch (targetType)
        {
            case ShapeType.Bonus: // ボーナス的の場合
                bonusShapePool.Enqueue(shapeObject); 
                break;
            default: // 通常的の場合
                standardShapePool.Enqueue(shapeObject); 
                break;
        }
    }
}
