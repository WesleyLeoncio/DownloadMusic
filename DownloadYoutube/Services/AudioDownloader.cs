using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DownloadYoutube.Services;

/// <summary>
/// Baixa só o áudio e converte para MP3.
/// </summary>
public class AudioDownloader : IDownloader
{
    private readonly YoutubeClient _youtube;

    public AudioDownloader(YoutubeClient youtube)
    {
        _youtube = youtube;
    }

    // Áudio é leve
    public int MaxParalelo => 5;

    public async Task BaixarAsync(Video video, string outputDirectory)
    {
        StreamManifest manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);
        IStreamInfo audioStreamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        string mp3FilePath = Path.Combine(outputDirectory, $"{ArquivoUtil.NomeSeguro(video.Title)}.mp3");

        string tempDir = ArquivoUtil.CriarPastaTemporaria();
        try
        {
            string audioTemp = Path.Combine(tempDir, $"audio.{audioStreamInfo.Container.Name}");
            await _youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioTemp);
            await FfmpegService.ConverterParaMp3Async(audioTemp, mp3FilePath);
        }
        finally
        {
            ArquivoUtil.LimparPastaTemporaria(tempDir);
        }
    }
}
