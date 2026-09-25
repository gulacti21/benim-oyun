using UnityEditor;
using UnityEngine;

// Müzik dosyaları bellekte açılmasın, akış (streaming) olarak çalsın.
public class MusicImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/Resources/Mahalle/Music/")) return;
        var importer = (AudioImporter)assetImporter;
        var s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = .6f;
        importer.defaultSampleSettings = s;
        importer.loadInBackground = true;
    }
}
