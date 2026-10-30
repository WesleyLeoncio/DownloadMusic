using System.Diagnostics;
using System.IO;

namespace DownloadMusic.Services;

/// <summary>
/// Uma trilha de áudio já baixada, pronta para entrar no arquivo final.
/// Idioma = código ISO de 3 letras (por, eng, spa, und).
/// </summary>
public record FaixaAudio(string Caminho, string Idioma, string Titulo);

/// <summary>
/// Tudo que envolve o ffmpeg.exe. Não conhece YouTube nem interface.
/// </summary>
public static class FfmpegService
{
    private static string ObterCaminho()
    {
        string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "FFmpeg",
            "ffmpeg-7.1.1-essentials_build", "bin", "ffmpeg.exe");

        if (!File.Exists(ffmpegPath))
            throw new FileNotFoundException("ffmpeg.exe não encontrado.", ffmpegPath);

        return ffmpegPath;
    }

    private static async Task ExecutarAsync(IEnumerable<string> args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ObterCaminho(),
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        // Ler o stderr até o fim antes de esperar a saída evita deadlock
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string resumo = stderr.Length > 500 ? stderr[^500..] : stderr;
            throw new InvalidOperationException($"ffmpeg falhou (código {process.ExitCode}): {resumo}");
        }
    }

    public static Task ConverterParaMp3Async(string inputPath, string outputPath)
    {
        return ExecutarAsync(new[]
        {
            "-i", inputPath, "-vn", "-ab", "192k", "-ar", "44100", "-y", outputPath
        });
    }

    public static Task JuntarAsync(string videoPath, IReadOnlyList<FaixaAudio> faixas, string outputPath)
    {
        var args = new List<string> { "-y", "-i", videoPath };

        foreach (var faixa in faixas)
        {
            args.Add("-i");
            args.Add(faixa.Caminho);
        }

        // Mapeia o vídeo e todas as trilhas de áudio
        args.AddRange(new[] { "-map", "0:v:0" });
        for (int i = 0; i < faixas.Count; i++)
        {
            args.Add("-map");
            args.Add($"{i + 1}:a:0");
        }

        // Copia sem recodificar
        args.AddRange(new[] { "-c", "copy" });

        // Metadados de idioma e trilha padrão (a primeira)
        for (int i = 0; i < faixas.Count; i++)
        {
            args.AddRange(new[]
            {
                $"-metadata:s:a:{i}", $"language={faixas[i].Idioma}",
                $"-metadata:s:a:{i}", $"title={faixas[i].Titulo}",
                $"-disposition:a:{i}", i == 0 ? "default" : "0"
            });
        }

        args.Add(outputPath);

        return ExecutarAsync(args);
    }
}
