using System.ComponentModel;
using YoutubeExplode.Videos;

namespace DownloadMusic.models;

public class VideoItem : INotifyPropertyChanged
{
    private string _status = string.Empty;
    public string Titulo => Video?.Title ?? "";
    public TimeSpan Duracao => Video?.Duration ?? TimeSpan.Zero;
    public Video? Video { get; set; }
    
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}