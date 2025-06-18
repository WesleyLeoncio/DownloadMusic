using System.Windows;
using DownloadMusic.Others.Enums;

namespace DownloadMusic.Windows;

public partial class CustomMessageBox : Window
{
    private bool? _result = null;
  
    private CustomMessageBox(string title, string message, DialogType type)
    {
        InitializeComponent();

        TxtTitle.Text = title;
        TxtMessage.Text = message;

        switch (type)
        {
            case DialogType.Success:
                TxtIcon.Text = "✅";
                BtnOk.Visibility = Visibility.Visible;
                break;
            case DialogType.Error:
                TxtIcon.Text = "❌";
                BtnOk.Visibility = Visibility.Visible;
                break;
            case DialogType.Question:
                TxtIcon.Text = "❓";
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                break;
        }
    }

    public static bool? Show(string message, string title, DialogType type)
    {
        var dialog = new CustomMessageBox(title, message, type);
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
        return dialog._result;
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        _result = true;
        Close();
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        _result = false;
        Close();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        _result = null;
        Close();
    }
}