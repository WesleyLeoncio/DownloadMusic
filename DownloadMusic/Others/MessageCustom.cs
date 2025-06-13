using System.Windows.Forms;

namespace DownloadMusic.Others;

public static class MessageCustom
{
    public static void Info(string message, string? title = null)
    {
        MessageBox.Show(
            message,
            title ?? "Informação",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    public static void Erro(string message, string? title = null)
    {
        MessageBox.Show(
            message,
            title ?? "Erro",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
    }

    public static void Sucesso(string message, string? title = null)
    {
        MessageBox.Show(
            message,
            title ?? "Sucesso",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    public static void Aviso(string message, string? title = null)
    {
        MessageBox.Show(
            message,
            title ?? "Aviso",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
    }
}