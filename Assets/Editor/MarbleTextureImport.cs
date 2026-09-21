using UnityEditor;
using UnityEngine;

// Matcap dokusunun import ayarlari. Wrap Clamp sart: Repeat olursa kurenin
// kenarinda dokunun karsi kenari sizar ve misketin siluetinde cizgi olusur.
public class MarbleTextureImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith("Mahalle/MarbleMatcap.png")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.maxTextureSize = 512;          // mobilde 512 fazlasiyla yeterli
        importer.textureCompression = TextureImporterCompression.Compressed;
    }
}
