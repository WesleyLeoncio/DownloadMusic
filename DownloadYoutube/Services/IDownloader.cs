using YoutubeExplode.Videos;

namespace DownloadYoutube.Services;

public interface IDownloader
{
    int MaxParalelo { get; }

    Task BaixarAsync(Video video, string outputDirectory);
}
