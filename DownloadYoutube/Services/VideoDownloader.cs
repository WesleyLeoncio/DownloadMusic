using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DownloadYoutube.Services;

/// <summary>
/// Baixa o vídeo completo com uma trilha de áudio por idioma e junta tudo em um MKV.
/// </summary>
public class VideoDownloader : IDownloader
{
    private readonly YoutubeClient _youtube;

    public VideoDownloader(YoutubeClient youtube)
    {
        _youtube = youtube;
    }

    // Vídeo com várias trilhas é pesado
    public int MaxParalelo => 2;

    public async Task BaixarAsync(Video video, string outputDirectory)
    {
        StreamManifest manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);

        IVideoStreamInfo videoStream = manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
        List<AudioOnlyStreamInfo> audioStreams = SeletorDeTrilhas.Selecionar(manifest);

        if (audioStreams.Count == 0)
            throw new InvalidOperationException("Nenhuma trilha de áudio encontrada.");

        string outputPath = Path.Combine(outputDirectory, $"{ArquivoUtil.NomeSeguro(video.Title)}.mkv");

        string tempDir = ArquivoUtil.CriarPastaTemporaria();
        try
        {
            string videoTemp = Path.Combine(tempDir, $"video.{videoStream.Container.Name}");
            await _youtube.Videos.Streams.DownloadAsync(videoStream, videoTemp);

            var faixas = new List<FaixaAudio>();
            for (int i = 0; i < audioStreams.Count; i++)
            {
                AudioOnlyStreamInfo audio = audioStreams[i];
                string audioTemp = Path.Combine(tempDir, $"audio{i}.{audio.Container.Name}");
                await _youtube.Videos.Streams.DownloadAsync(audio, audioTemp);

                faixas.Add(new FaixaAudio(
                    audioTemp,
                    SeletorDeTrilhas.CodigoIso3(audio),
                    audio.AudioLanguage?.Name ?? "Desconhecido"));
            }

            await FfmpegService.JuntarAsync(videoTemp, faixas, outputPath);
        }
        finally
        {
            ArquivoUtil.LimparPastaTemporaria(tempDir);
        }
    }
}
