using YoutubeExplode.Videos;

namespace DownloadMusic.Services;

public interface IDownloader
{
    int MaxParalelo { get; }

    Task BaixarAsync(Video video, string outputDirectory);
}
