using DownloadYoutube.Others.Enums;
using YoutubeExplode;

namespace DownloadYoutube.Services;

/// <summary>
/// Único lugar que sabe qual classe corresponde a cada formato.
/// Para adicionar um formato novo: criar a classe + uma linha aqui.
/// </summary>
public static class DownloaderFactory
{
    public static IDownloader Criar(YoutubeClient youtube, FormatoSaida formato)
    {
        return formato switch
        {
            FormatoSaida.AudioMp3 => new AudioDownloader(youtube),
            FormatoSaida.VideoMkv => new VideoDownloader(youtube),
            _ => throw new ArgumentOutOfRangeException(nameof(formato), formato, "Formato não suportado.")
        };
    }
}
