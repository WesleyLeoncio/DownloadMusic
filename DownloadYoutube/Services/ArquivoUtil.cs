using System.IO;

namespace DownloadYoutube.Services;

/// <summary>
/// Utilitários de arquivo compartilhados pelos downloaders.
/// </summary>
public static class ArquivoUtil
{
    public static string NomeSeguro(string titulo)
    {
        return string.Join("_", titulo.Split(Path.GetInvalidFileNameChars()));
    }

    // Pasta temporária única por vídeo (evita conflito nos downloads paralelos)
    public static string CriarPastaTemporaria()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DownloadYoutube", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    public static void LimparPastaTemporaria(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        catch
        {
            // Falha ao limpar temporários não deve derrubar o app
        }
    }
}
