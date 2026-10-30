using System.Globalization;
using YoutubeExplode.Videos.Streams;

namespace DownloadMusic.Services;

/// <summary>
/// Regras para escolher as trilhas de áudio de um vídeo (uma por idioma).
/// </summary>
public static class SeletorDeTrilhas
{
    /// <summary>
    /// Escolhe uma trilha por idioma (a de maior bitrate), sem repetir.
    /// A trilha padrão/original do vídeo vem primeiro.
    /// </summary>
    public static List<AudioOnlyStreamInfo> Selecionar(StreamManifest manifest)
    {
        return manifest.GetAudioOnlyStreams()
            .GroupBy(IdiomaBase)
            .Select(g => g.OrderByDescending(s => s.Bitrate).First())
            .OrderByDescending(s => s.IsAudioLanguageDefault == true)
            .ToList();
    }

    // "pt-BR" e "pt" viram "pt"; sem idioma informado vira "und"
    public static string IdiomaBase(AudioOnlyStreamInfo stream)
    {
        string? code = stream.AudioLanguage?.Code;
        if (string.IsNullOrWhiteSpace(code))
            return "und";

        return code.Split('-')[0].ToLowerInvariant();
    }

    public static string CodigoIso3(AudioOnlyStreamInfo stream)
    {
        string baseCode = IdiomaBase(stream);
        if (baseCode == "und")
            return "und";

        try
        {
            return new CultureInfo(baseCode).ThreeLetterISOLanguageName; // pt -> por, en -> eng, es -> spa
        }
        catch
        {
            return "und";
        }
    }
}
