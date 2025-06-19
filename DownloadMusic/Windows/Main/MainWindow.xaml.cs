using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DownloadMusic.models;
using DownloadMusic.Others;
using DownloadMusic.Others.Enums;
using DownloadMusic.Windows.Message;
using Ookii.Dialogs.Wpf;
using WpfAnimatedGif;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DownloadMusic.Windows.Main;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public ObservableCollection<VideoItem> VideoList { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void BtnEscolherPasta_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog();

        if (dialog.ShowDialog() == true)
        {
            TxtSavePath.Text = dialog.SelectedPath;
        }
    }

    private async void BtnBuscarVideos_Click(object sender, RoutedEventArgs e)
    {
        string inputUrl = TxtUrl.Text.Trim();
        DesabilitarBotaoDownload();

        if (ChecarCampos(inputUrl))
            return;

        await ProcessarUrlAsync(inputUrl);
    }

    private async void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        int concluidos = 0;
        SemaphoreSlim semaphore = new SemaphoreSlim(SemaphoreSlimCont());
        IEnumerable<Task> tasks = VideoList.Select(async item =>
        {
            if (item.Video != null)
            {
                await semaphore.WaitAsync();

                try
                {
                    item.Status = "👇";
                    await ProcessVideoAsync(new YoutubeClient(), item.Video);
                    int valorAtual = Interlocked.Increment(ref concluidos);
                    await AtualizarProgressoDownloadAsync(valorAtual);
                    item.Status = "👌";
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Erro ao baixar '{item.Video.Title}': {ex.Message}",
                        "Erro", DialogType.Error);
                    item.Status = Emoji.Erro;
                }
                finally
                {
                    semaphore.Release();
                }
            }
        });
        await Task.WhenAll(tasks);
        DownloadFinalizado();
    }

    private async Task ProcessarUrlAsync(string inputUrl)
    {
        try
        {
            if (PlaylistUrl(inputUrl))
            {
                await CarregarPlaylistAsync(inputUrl);
                BtnDownload.IsEnabled = true;
            }
            else if (VideoUrl(inputUrl))
            {
                await CarregarVideoAsync(inputUrl);
                BtnDownload.IsEnabled = true;
            }
            else
            {
                CustomMessageBox.Show("URL inválida do YouTube.","Erro",DialogType.Error);
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Erro ao baixar: {ex.Message}",
                "Erro",DialogType.Error);
        }
    }


    private async Task AtualizarProgressoDownloadAsync(int valorAtual)
    {
        int total = VideoList.Count;
        double porcentagem = (double)valorAtual / total * 100;
        await Dispatcher.InvokeAsync(() => AtualizarProgresso(porcentagem));
    }

    private void AtualizarProgresso(double valor)
    {
        DownloadProgressBar.Value = valor;
        ProgressText.Text = $"{valor:0}%";
    }

    private void DownloadFinalizado()
    {
        CustomMessageBox.Show("Downloads finalizados!", "Finalizados", DialogType.Success);
        DesabilitarBotaoDownload();
        AtualizarProgresso(0);
        VideoList.Clear();
    }

    private async Task CarregarVideoAsync(string url)
    {
        await LoadMusica(url);
        HabilitarBotaoDownload();
    }

    private async Task CarregarPlaylistAsync(string url)
    {
        await LoadPlaylistAsync(url);
        HabilitarBotaoDownload();
    }

    private async Task ProcessVideoAsync(YoutubeClient youtube, Video video)
    {
        StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        IStreamInfo audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        string safeTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

        string outputDirectory = string.IsNullOrWhiteSpace(TxtSavePath.Text)
            ? AppDomain.CurrentDomain.BaseDirectory
            : TxtSavePath.Text;

        string audioFilePath = Path.Combine(outputDirectory, $"{safeTitle}.webm");
        string mp3FilePath = Path.Combine(outputDirectory, $"{safeTitle}.mp3");

        await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioFilePath);

        ConvertToMp3(audioFilePath, mp3FilePath);

        File.Delete(audioFilePath);
    }

    private void ConvertToMp3(string inputPath, string outputPath)
    {
        string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "FFmpeg",
            "ffmpeg-7.1.1-essentials_build", "bin",
            "ffmpeg.exe");

        string arguments = $"-i \"{inputPath}\" -vn -ab 192k -ar 44100 -y \"{outputPath}\"";

        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }

    private void BtnRemover_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is VideoItem item &&
            VideoList.Contains(item))
        {
            VideoList.Remove(item);
        }
    }

    private async Task LoadPlaylistAsync(string playlistUrl)
    {
        YoutubeClient youtube = new YoutubeClient();
        IReadOnlyList<PlaylistVideo> videos = await youtube.Playlists.GetVideosAsync(playlistUrl);

        VideoList.Clear();
        ShowLoading();
        foreach (var video in videos)
        {
            Video v = await youtube.Videos.GetAsync(video.Url);
            VideoList.Add(new VideoItem
            {
                Video = v,
                Status = "✋"
            });
        }
        HideLoading();
    }

    private async Task LoadMusica(string inputUrl)
    {
        VideoList.Clear();
        ShowLoading();
        
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(inputUrl);

        VideoList.Add(new VideoItem
        {
            Video = video,
            Status = "✋"
        });
        HideLoading();
    }

    private bool ChecarCampos(string inputUrl)
    {
        string inputSavePath = TxtSavePath.Text.Trim();

        if (string.IsNullOrWhiteSpace(inputSavePath))
        {
            CustomMessageBox.Show("Informe o caminho que deseja salvar.","Erro", DialogType.Error);
            return true;
        }

        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            CustomMessageBox.Show("Informe a URL da música ou playlist.","Erro", DialogType.Error);
            return true;
        }

        return false;
    }

    private void HabilitarBotaoDownload()
    {
        BtnDownload.IsEnabled = true;
    }

    private void DesabilitarBotaoDownload()
    {
        BtnDownload.IsEnabled = false;
    }

    private int SemaphoreSlimCont()
    {
        return Math.Min(5, VideoList.Count);
    }

    private static bool PlaylistUrl(string url)
    {
        return Regex.IsMatch(url, @"[?&]list=([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
    }

    private static bool VideoUrl(string url)
    {
        return Regex.IsMatch(url, @"(youtu\.be/|watch\?v=)[a-zA-Z0-9_-]{11}", RegexOptions.IgnoreCase);
    }
    
    private void ShowLoading()
    {
        OverlayLoading.Visibility = Visibility.Visible;

        string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IMG", "load.gif");

        var imageUri = new Uri(imagePath, UriKind.Absolute);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = imageUri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();

        ImageBehavior.SetAnimatedSource(ImgLoading, bitmap);
    }


    private void HideLoading()
    {
        OverlayLoading.Visibility = Visibility.Collapsed;
        ImageBehavior.SetAnimatedSource(ImgLoading, null);
    }


    private void AbrirLinkMusica_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is VideoItem video && !string.IsNullOrWhiteSpace(video.Url))
        {
            bool? resposta = CustomMessageBox.Show("Deseja abrir a musica no navegador?", "Escutar Música", DialogType.Question);
            if (resposta == true)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = video.Url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao abrir o link: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }


    
}