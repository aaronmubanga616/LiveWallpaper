using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace LiveWallpaper;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _progressTimer;
    private WallpaperHostWindow? _wallpaperWindow;
    private string? _selectedVideoPath;
    private TimeSpan _duration = TimeSpan.Zero;
    private bool _isMediaReady;
    private bool _isSeeking;
    private bool _isUpdatingSeekSlider;
    private bool _isPreviewPlaying;
    private bool _previewEnded;

    public MainWindow()
    {
        InitializeComponent();

        SeekSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(SeekSlider_DragStarted));
        SeekSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(SeekSlider_DragCompleted));
        SeekSlider.ValueChanged += SeekSlider_ValueChanged;

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _progressTimer.Tick += ProgressTimer_Tick;

        var bundledVideo = Path.Combine(AppContext.BaseDirectory, "Assets", "855432-hd_1840_1034_25fps.mp4");
        if (File.Exists(bundledVideo))
        {
            LoadVideo(bundledVideo);
        }
        else
        {
            SetStatus("CHOOSE A VIDEO", "#8190A3");
        }
    }

    private void ChooseVideoButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a wallpaper video",
            Filter = "Video files|*.mp4;*.wmv;*.avi;*.mov;*.m4v;*.mpg;*.mpeg|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadVideo(dialog.FileName);
        }
    }

    private void LoadVideo(string path)
    {
        StopWallpaper();
        _isMediaReady = false;
        _isPreviewPlaying = false;
        _previewEnded = false;
        _duration = TimeSpan.Zero;
        _selectedVideoPath = Path.GetFullPath(path);
        PreviewPlayer.Stop();
        PreviewPlayer.Close();
        PreviewPlayer.Source = new Uri(_selectedVideoPath, UriKind.Absolute);
        PreviewPlayer.Volume = VolumeSlider.Value;
        PreviewPlayer.IsMuted = false;

        VideoNameText.Text = Path.GetFileName(_selectedVideoPath);
        VideoPathText.Text = _selectedVideoPath;
        PreviewMessage.Text = "Opening video preview…";
        PreviewPlaceholder.Visibility = Visibility.Visible;
        PlayPauseButton.IsEnabled = false;
        SeekSlider.IsEnabled = false;
        ApplyButton.IsEnabled = false;
        TimeText.Text = "00:00 / 00:00";
        SetStatus("LOADING VIDEO", "#F2C879");
    }

    private void PreviewPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        _isMediaReady = true;
        _duration = PreviewPlayer.NaturalDuration.HasTimeSpan ? PreviewPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero;
        SeekSlider.Maximum = Math.Max(_duration.TotalSeconds, 1);
        SeekSlider.IsEnabled = _duration > TimeSpan.Zero;
        PlayPauseButton.IsEnabled = true;
        ApplyButton.IsEnabled = !string.IsNullOrWhiteSpace(_selectedVideoPath);
        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        PreviewPlayer.Play();
        UpdatePlayPauseButton(isPlaying: true);
        _progressTimer.Start();
        UpdateTimeText(TimeSpan.Zero);
        SetStatus("VIDEO READY", "#6EE7C2");
    }

    private void PreviewPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (LoopCheckBox.IsChecked == true)
        {
            _previewEnded = false;
            PreviewPlayer.Position = TimeSpan.Zero;
            PreviewPlayer.Play();
            UpdatePlayPauseButton(isPlaying: true);
        }
        else
        {
            _previewEnded = true;
            UpdatePlayPauseButton(isPlaying: false);
        }
    }

    private void PreviewPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _isMediaReady = false;
        _isPreviewPlaying = false;
        _previewEnded = false;
        _progressTimer.Stop();
        var message = e.ErrorException?.Message ?? "Windows could not decode this video.";
        PreviewMessage.Text = "This video could not be played";
        PreviewPlaceholder.Visibility = Visibility.Visible;
        ApplyButton.IsEnabled = false;
        PlayPauseButton.IsEnabled = false;
        SeekSlider.IsEnabled = false;
        SetStatus("VIDEO ERROR", "#F07C78");
        MessageBox.Show(this,
            "Windows could not play this file. Try an MP4 encoded with H.264 video and AAC audio.\n\n" + message,
            "Video playback error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMediaReady)
        {
            return;
        }

        if (_isPreviewPlaying)
        {
            PreviewPlayer.Pause();
            UpdatePlayPauseButton(isPlaying: false);
        }
        else
        {
            if (_previewEnded)
            {
                PreviewPlayer.Position = TimeSpan.Zero;
                _previewEnded = false;
            }

            PreviewPlayer.Play();
            UpdatePlayPauseButton(isPlaying: true);
        }
    }

    private void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isMediaReady || _isSeeking)
        {
            return;
        }

        var position = PreviewPlayer.Position;
        if (_duration > TimeSpan.Zero)
        {
            _isUpdatingSeekSlider = true;
            SeekSlider.Value = Math.Clamp(position.TotalSeconds, 0, _duration.TotalSeconds);
            _isUpdatingSeekSlider = false;
        }

        UpdateTimeText(position);
    }

    private void SeekSlider_DragStarted(object sender, DragStartedEventArgs e)
    {
        _isSeeking = true;
    }

    private void SeekSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        SeekToSliderPosition();
        _isSeeking = false;
    }

    private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSeeking)
        {
            SeekToSliderPosition();
        }
        else if (!_isUpdatingSeekSlider)
        {
            SeekToSliderPosition();
        }
    }

    private void SeekToSliderPosition()
    {
        if (_isMediaReady && _duration > TimeSpan.Zero)
        {
            PreviewPlayer.Position = TimeSpan.FromSeconds(Math.Clamp(SeekSlider.Value, 0, _duration.TotalSeconds));
            UpdateTimeText(PreviewPlayer.Position);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PreviewPlayer is not null)
        {
            PreviewPlayer.Volume = VolumeSlider.Value;
        }

        UpdateWallpaperOptions();
    }

    private void DisplayStyleComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateWallpaperOptions();
    }

    private void WallpaperOptions_Changed(object sender, RoutedEventArgs e)
    {
        UpdateWallpaperOptions();
    }

    private void UpdateWallpaperOptions()
    {
        _wallpaperWindow?.UpdatePlaybackOptions(
            LoopCheckBox.IsChecked == true,
            MuteWallpaperCheckBox.IsChecked == true,
            VolumeSlider.Value,
            GetSelectedStretch());
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMediaReady || string.IsNullOrWhiteSpace(_selectedVideoPath))
        {
            return;
        }

        PreviewPlayer.Pause();
        UpdatePlayPauseButton(isPlaying: false);
        StopWallpaper();

        var host = new WallpaperHostWindow(
            _selectedVideoPath,
            LoopCheckBox.IsChecked == true,
            MuteWallpaperCheckBox.IsChecked == true,
            VolumeSlider.Value,
            GetSelectedStretch());

        host.PlaybackFailed += WallpaperWindow_PlaybackFailed;
        try
        {
            host.Show();
            if (!string.IsNullOrEmpty(host.HostError))
            {
                host.Close();
                MessageBox.Show(this, host.HostError, "Could not apply wallpaper", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStatus("WALLPAPER NOT APPLIED", "#F07C78");
                return;
            }

            host.StartPlayback();
            _wallpaperWindow = host;
            StopButton.IsEnabled = true;
            SetStatus("WALLPAPER ACTIVE", "#6EE7C2");
            WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            if (host.IsVisible)
            {
                host.Close();
            }

            MessageBox.Show(this, "Windows could not start the desktop wallpaper.\n\n" + ex.Message,
                "Could not apply wallpaper", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatus("WALLPAPER NOT APPLIED", "#F07C78");
        }
    }

    private void WallpaperWindow_PlaybackFailed(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            StopWallpaper();
            SetStatus("WALLPAPER ERROR", "#F07C78");
            MessageBox.Show(this, "The desktop video stopped because Windows could not play it.\n\n" + message,
                "Wallpaper playback error", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    private Stretch GetSelectedStretch() => DisplayStyleComboBox.SelectedIndex switch
    {
        1 => Stretch.Uniform,
        2 => Stretch.Fill,
        _ => Stretch.UniformToFill
    };

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopWallpaper();
    }

    private void StopWallpaper()
    {
        if (_wallpaperWindow is not null)
        {
            _wallpaperWindow.PlaybackFailed -= WallpaperWindow_PlaybackFailed;
            _wallpaperWindow.Close();
            _wallpaperWindow = null;
        }

        StopButton.IsEnabled = false;
        if (_isMediaReady)
        {
            SetStatus("VIDEO READY", "#6EE7C2");
        }
        else if (_selectedVideoPath is null)
        {
            SetStatus("CHOOSE A VIDEO", "#8190A3");
        }
    }

    private void UpdatePlayPauseButton(bool isPlaying)
    {
        _isPreviewPlaying = isPlaying;
        PlayPauseButton.Content = isPlaying ? "Ⅱ" : "▶";
        PlayPauseButton.ToolTip = isPlaying ? "Pause preview" : "Play preview";
    }

    private void UpdateTimeText(TimeSpan position)
    {
        TimeText.Text = $"{FormatTime(position)} / {FormatTime(_duration)}";
    }

    private static string FormatTime(TimeSpan time) => time.TotalHours >= 1
        ? time.ToString(@"h\:mm\:ss")
        : time.ToString(@"m\:ss");

    private void SetStatus(string message, string color)
    {
        StatusText.Text = message;
        StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _progressTimer.Stop();
        StopWallpaper();
        PreviewPlayer.Stop();
        PreviewPlayer.Close();
    }
}
