using UnityEngine;

/// 光源用プロシージャルマテリアルのキャッシュ・提供管理。
/// 外部アセットへの依存をなくすための代替措置。
public static class ParticleMaterialUtils
{
    private static Material glowMaterial;

     /// 入力: なし | 出力: 発光マテリアル | 副作用: 初回呼び出し時にテクスチャとマテリアルを生成
    /// メモリリーク防止のため、アプリケーション全体で単一のインスタンスを使い回す
    public static Material GetGlowMaterial()
    {
        if (glowMaterial != null)
        {
            return glowMaterial;
        }

        // 実行時のテクスチャ生成負荷を最小限にするため低解像度で生成
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (distance / radius));
                // 物理的な光の減衰を模倣するための非線形カーブ適用
                alpha = Mathf.Pow(alpha, 1.5f);
                
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        // 端末のシェーダー対応状況による描画エラーを回避するためのフォールバック処理
        Shader particleShader = Shader.Find("Mobile/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

        glowMaterial = new Material(particleShader);
        glowMaterial.mainTexture = texture;
        
        return glowMaterial;
    }
}
