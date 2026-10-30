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
using DownloadMusic.Services;
using DownloadMusic.Windows.Message;
using Ookii.Dialogs.Wpf;
using WpfAnimatedGif;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace DownloadMusic.Windows.Main;

/// <summary>
/// Só interface: lê os controles, mostra mensagens e delega o download para um IDownloader.
/// </summary>
public partial class MainWindow
{
    public ObservableCollection<VideoItem> VideoList { get; } = new();
    private readonly YoutubeClient _youtube = new();

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
        // Lê formato e pasta UMA vez, antes do loop, para valer para todos os itens
        FormatoSaida formato = ObterFormatoSelecionado();
        string outputDirectory = ObterPastaDestino();
        IDownloader downloader = DownloaderFactory.Criar(_youtube, formato);

        int concluido = 0;
        DesabilitarBotaoDownload();
        DefinirSelecaoFormatoHabilitada(false);

        var semaphore = new SemaphoreSlim(Math.Max(1, Math.Min(downloader.MaxParalelo, VideoList.Count)));
        IEnumerable<Task> tasks = VideoList.ToList().Select(async item =>
        {
            if (item.Video == null)
                return;

            await semaphore.WaitAsync();

            try
            {
                item.Status = Emoji.Baixando;
                await downloader.BaixarAsync(item.Video, outputDirectory);
                int valorAtual = Interlocked.Increment(ref concluido);
                await AtualizarProgressoDownloadAsync(valorAtual);
                item.Status = Emoji.Concluido;
            }
            catch (Exception ex)
            {
                LogErro($"Erro ao baixar '{item.Video.Title}'", ex);
                CustomMessageBox.Show($"Erro ao baixar '{item.Video.Title}': {ex.Message}",
                    "Erro", DialogType.Error);
                item.Status = Emoji.Erro;
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        DownloadFinalizado();
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

    private void AbrirLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is VideoItem video && !string.IsNullOrWhiteSpace(video.Url))
        {
            bool? resposta = CustomMessageBox.Show("Deseja abrir o vídeo no navegador?", "Abrir no navegador",
                DialogType.Question);
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
                    MessageBox.Show("Erro ao abrir o link: " + ex.Message, "Erro", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
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
                CustomMessageBox.Show("URL inválida do YouTube.", "Erro", DialogType.Error);
            }
        }
        catch (Exception ex)
        {
            HideLoading();
            LogErro($"Erro ao buscar '{inputUrl}'", ex);
            CustomMessageBox.Show($"Erro ao buscar: {ex.Message}",
                "Erro", DialogType.Error);
        }
    }

    private async Task CarregarVideoAsync(string url)
    {
        await LoadConteudo(url);
        HabilitarBotaoDownload();
    }

    private async Task CarregarPlaylistAsync(string url)
    {
        await LoadPlaylistAsync(url);
        HabilitarBotaoDownload();
    }

    private async Task LoadPlaylistAsync(string playlistUrl)
    {
        YoutubeClient youtube = _youtube;
        IReadOnlyList<PlaylistVideo> videos = await youtube.Playlists.GetVideosAsync(playlistUrl);

        VideoList.Clear();
        ShowLoading();
        foreach (var video in videos)
        {
            Video v = await youtube.Videos.GetAsync(video.Url);
            VideoList.Add(new VideoItem
            {
                Video = v,
                Status = Emoji.Pendente
            });
        }

        HideLoading();
    }

    private async Task LoadConteudo(string inputUrl)
    {
        VideoList.Clear();
        ShowLoading();

        var youtube = _youtube;
        var video = await youtube.Videos.GetAsync(inputUrl);

        VideoList.Add(new VideoItem
        {
            Video = video,
            Status = Emoji.Pendente
        });
        HideLoading();
    }

    private FormatoSaida ObterFormatoSelecionado()
    {
        return RbVideo.IsChecked == true ? FormatoSaida.VideoMkv : FormatoSaida.AudioMp3;
    }

    private string ObterPastaDestino()
    {
        return string.IsNullOrWhiteSpace(TxtSavePath.Text)
            ? AppDomain.CurrentDomain.BaseDirectory
            : TxtSavePath.Text;
    }

    private void DefinirSelecaoFormatoHabilitada(bool habilitada)
    {
        RbAudio.IsEnabled = habilitada;
        RbVideo.IsEnabled = habilitada;
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
        CustomMessageBox.Show("Download Finalizado!", "Finalizado", DialogType.Success);
        DesabilitarBotaoDownload();
        DefinirSelecaoFormatoHabilitada(true);
        AtualizarProgresso(0);
        VideoList.Clear();
    }

    private bool ChecarCampos(string inputUrl)
    {
        string inputSavePath = TxtSavePath.Text.Trim();

        if (string.IsNullOrWhiteSpace(inputSavePath))
        {
            CustomMessageBox.Show("Informe o caminho que deseja salvar.", "Erro", DialogType.Error);
            return true;
        }

        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            CustomMessageBox.Show("Informe a url do vídeo ou playlist.", "Erro", DialogType.Error);
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

    private static void LogErro(string contexto, Exception ex)
    {
        string texto =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexto}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";

        Debug.WriteLine(texto);

        try
        {
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "erros.log"), texto);
        }
        catch
        {
            // Falha ao gravar o log não deve derrubar o app
        }
    }
}
