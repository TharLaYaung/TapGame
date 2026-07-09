using UnityEngine;
using UnityEditor;

/// アセットインポート時の自動処理パイプライン
/// UIやアイテム用のテクスチャを自動的にSpriteとして設定する
public class UISpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        // UI用またはアイテム用のテクスチャがインポートされた場合のみ処理を適用
        if (assetPath.Contains("Resources/UI/") || assetPath.Contains("Resources/Items/"))
        {
            // インポート設定を書き換えるためTextureImporterを取得
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            // 2Dゲームで使用するため、テクスチャタイプをSpriteに変更
            textureImporter.textureType = TextureImporterType.Sprite;
            // 単一のスプライトとしてインポートする設定
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            // アルファチャンネルを透過情報として扱う
            textureImporter.alphaIsTransparency = true;
            // 2DのUIやスプライトではミップマップ不要なためオフにしてメモリを節約
            textureImporter.mipmapEnabled = false;
            
            // アイテム画像の場合の特殊処理
            if (assetPath.Contains("Resources/Items/"))
            {
                // 1024pxのテクスチャを1Unitの物理サイズにスケールダウンし、Prefab間のサイズ差異を防ぐ
                textureImporter.spritePixelsPerUnit = 1024;
            }
        }
    }
}
