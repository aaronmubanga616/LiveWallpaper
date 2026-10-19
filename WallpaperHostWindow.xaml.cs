using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace LiveWallpaper;

public partial class WallpaperHostWindow : Window
{
    private readonly string _videoPath;
    private bool _loop;
    private bool _isHosted;

    public WallpaperHostWindow(string videoPath, bool loop, bool muted, double volume, Stretch stretch)
    {
        InitializeComponent();

        _videoPath = videoPath;
        _loop = loop;

        SourceInitialized += WallpaperHostWindow_SourceInitialized;
        Closed += WallpaperHostWindow_Closed;
        WallpaperPlayer.IsMuted = muted;
        WallpaperPlayer.Volume = volume;
        WallpaperPlayer.Stretch = stretch;
    }

    public string? HostError { get; private set; }

    public event EventHandler<string>? PlaybackFailed;

    public void StartPlayback()
    {
        WallpaperPlayer.Source = new Uri(_videoPath, UriKind.Absolute);
    }

    public void UpdatePlaybackOptions(bool loop, bool muted, double volume, Stretch stretch)
    {
        _loop = loop;
        WallpaperPlayer.IsMuted = muted;
        WallpaperPlayer.Volume = volume;
        WallpaperPlayer.Stretch = stretch;
    }

    private void WallpaperHostWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _isHosted = DesktopWallpaperHost.TryAttach(handle, out var error);
        if (!_isHosted)
        {
            HostError = error;
        }
    }

    private void WallpaperPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        WallpaperPlayer.Play();
        Opacity = 1;
    }

    private void WallpaperPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (!_loop)
        {
            WallpaperPlayer.Stop();
            return;
        }

        WallpaperPlayer.Position = TimeSpan.Zero;
        WallpaperPlayer.Play();
    }

    private void WallpaperPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        PlaybackFailed?.Invoke(this, e.ErrorException?.Message ?? "Windows could not play this video.");
    }

    private void WallpaperHostWindow_Closed(object? sender, EventArgs e)
    {
        WallpaperPlayer.Stop();
        WallpaperPlayer.Close();
        _isHosted = false;
    }
}
