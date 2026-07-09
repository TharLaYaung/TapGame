using UnityEngine;

/// プログラムから動的にカスタムメッシュ（三角形やダイヤ型など）を生成するユーティリティクラス
public static class ShapeMeshGenerator
{
    /// 8面体（ダイヤ型）の3Dメッシュを生成する
    public static Mesh CreateDiamondMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "DiamondMesh";

        // 8面体の頂点定義（当たり判定のコライダーに収まるよう半径0.5にスケールダウン）
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0.5f, 0),    // 上
            new Vector3(0.5f, 0, 0),    // 右
            new Vector3(0, 0, -0.5f),   // 手前
            new Vector3(-0.5f, 0, 0),   // 左
            new Vector3(0, 0, 0.5f),    // 奥
            new Vector3(0, -0.5f, 0)    // 下
        };

        // 面（三角形）の定義（時計回りで表面）
        int[] triangles = new int[]
        {
            0, 1, 2, // 上半分の4面
            0, 2, 3,
            0, 3, 4,
            0, 4, 1,
            5, 2, 1, // 下半分の4面
            5, 3, 2,
            5, 4, 3,
            5, 1, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // ライティング用に法線を自動計算

        return mesh;
    }

    /// 4面体（ピラミッド/三角形）の3Dメッシュを生成する
    public static Mesh CreateTriangleMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "TriangleMesh";

        // 全体を0.5倍にスケールダウンしてコライダーに収める
        float sqrt3 = Mathf.Sqrt(3f) * 0.5f;
        
        // 4面体の頂点定義
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0.5f, 0),                       // 頂点
            new Vector3(-0.5f, -0.25f, -sqrt3 / 3f),        // 底面左前
            new Vector3(0.5f, -0.25f, -sqrt3 / 3f),         // 底面右前
            new Vector3(0, -0.25f, sqrt3 * 2f / 3f)      // 底面奥
        };

        // 面（三角形）の定義
        int[] triangles = new int[]
        {
            0, 1, 2, // 手前の面
            0, 2, 3, // 右奥の面
            0, 3, 1, // 左奥の面
            1, 3, 2  // 底面
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // ライティング用に法線を自動計算

        return mesh;
    }
}
