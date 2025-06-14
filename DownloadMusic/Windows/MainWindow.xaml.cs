﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using DownloadMusic.models;
using DownloadMusic.Others;
using Ookii.Dialogs.Wpf;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DownloadMusic.Windows;

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
                    item.Status = Emoji.Baixando;
                    await ProcessVideoAsync(new YoutubeClient(), item.Video);
                    int valorAtual = Interlocked.Increment(ref concluidos);
                    await AtualizarProgressoDownloadAsync(valorAtual);
                    item.Status = Emoji.Concluido;
                }
                catch (Exception ex)
                {
                    MessageCustom.Erro($"Erro ao baixar '{item.Video.Title}': {ex.Message}");
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
                MessageCustom.Erro("URL inválida do YouTube.");
            }
        }
        catch (Exception ex)
        {
            MessageCustom.Erro($"Erro ao baixar: {ex.Message}");
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

        ProgressText.Foreground = valor >= 50 ? Brushes.White : Brushes.Black;
    }

    private void DownloadFinalizado()
    {
        MessageCustom.Sucesso("Downloads finalizados!");
        DesabilitarBotaoDownload();
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
        if (sender is System.Windows.Controls.Button button &&
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

        foreach (var video in videos)
        {
            Video v = await youtube.Videos.GetAsync(video.Url);
            VideoList.Add(new VideoItem
            {
                Video = v,
                Status = "⏳"
            });
        }
    }

    private async Task LoadMusica(string inputUrl)
    {
        VideoList.Clear();

        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(inputUrl);

        VideoList.Add(new VideoItem
        {
            Video = video,
            Status = "⏳ Pendente"
        });
    }

    private bool ChecarCampos(string inputUrl)
    {
        string inputSavePath = TxtSavePath.Text.Trim();

        if (string.IsNullOrWhiteSpace(inputSavePath))
        {
            MessageCustom.Aviso("Informe o caminho que deseja salvar.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            MessageCustom.Aviso("Informe a URL da música ou playlist.");
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
}