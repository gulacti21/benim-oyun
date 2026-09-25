using UnityEditor;
using UnityEngine;

// Matcap dokusunun import ayarlari. Wrap Clamp sart: Repeat olursa kurenin
// kenarinda dokunun karsi kenari sizar ve misketin siluetinde cizgi olusur.
public class MarbleTextureImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        // Harfe duyarli karsilastirma yapmiyoruz: macOS buyuk/kucuk harfi
        // ayirmadigi icin dosya "logo.png" diye kaydedilebiliyor.
        var yol = assetPath.ToLowerInvariant();
        // Harita arka planlari: dikeyde tekrar ettigi icin Repeat, genis oldugu icin 2048.
        if (yol.Contains("resources/mahalle/map/"))
        {
            var mp = (TextureImporter)assetImporter;
            mp.textureType = TextureImporterType.Default;
            mp.wrapMode = TextureWrapMode.Clamp;
            mp.filterMode = FilterMode.Bilinear;
            // Mipmap yok: harita ekranda 1:1'e yakin gosteriliyor, mipmap sadece
            // bulaniklastirir ve bellegi %33 artirir.
            mp.mipmapEnabled = false;
            mp.sRGBTexture = true;
            mp.alphaSource = TextureImporterAlphaSource.None;
            // 4096 sinirinda Unity 4476'lik gorseli kuculturdu, sonra ekranda
            // tekrar buyurdu. Sinir yukseltildi, tek yeniden ornekleme kalmadi.
            mp.maxTextureSize = 8192;
            mp.textureCompression = TextureImporterCompression.CompressedHQ;
            return;
        }

        // Mahalle zemin fotograflari: doseniyor, o yuzden Repeat ve mipmap sart.
        if (yol.Contains("resources/mahalle/ground/"))
        {
            var gr = (TextureImporter)assetImporter;
            gr.textureType = TextureImporterType.Default;
            gr.wrapMode = TextureWrapMode.Repeat;
            gr.filterMode = FilterMode.Bilinear;
            gr.mipmapEnabled = true;
            gr.sRGBTexture = true;
            gr.alphaSource = TextureImporterAlphaSource.None;
            gr.maxTextureSize = 1024;
            gr.textureCompression = TextureImporterCompression.Compressed;
            return;
        }

        // Menu simgeleri ve logo da Sprite olmali.
        if (yol.Contains("resources/mahalle/icons/") || yol.EndsWith("mahalle/logo.png"))
        {
            var ic = (TextureImporter)assetImporter;
            ic.textureType = TextureImporterType.Sprite;
            ic.spriteImportMode = SpriteImportMode.Single;
            ic.alphaIsTransparency = true;
            ic.wrapMode = TextureWrapMode.Clamp;
            ic.mipmapEnabled = false;
            ic.maxTextureSize = yol.EndsWith("logo.png") ? 1024 : 256;
            ic.textureCompression = TextureImporterCompression.Compressed;
            return;
        }

        // Ana menu arka plani: Sprite olarak gelmeli, yoksa kod bulamaz.
        if (yol.EndsWith("mahalle/menubackground.png"))
        {
            var bg = (TextureImporter)assetImporter;
            bg.textureType = TextureImporterType.Sprite;
            bg.spriteImportMode = SpriteImportMode.Single;
            bg.wrapMode = TextureWrapMode.Clamp;
            bg.filterMode = FilterMode.Bilinear;
            bg.mipmapEnabled = false;
            bg.sRGBTexture = true;
            bg.alphaSource = TextureImporterAlphaSource.None;
            bg.maxTextureSize = 2048;
            bg.textureCompression = TextureImporterCompression.Compressed;
            return;
        }

        if (!yol.EndsWith("mahalle/marblematcap.png")) return;
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
