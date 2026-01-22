using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DiscordVocalOverlay.ViewModels;
using DiscordVocalOverlay.Services;

namespace DiscordVocalOverlay;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DragDropService _dragDropService;
    private readonly SettingsService _settingsService;
    private readonly RecordingStorageService _storageService;

    public MainWindow()
    {
        System.Diagnostics.Debug.WriteLine("MainWindow constructor started");
        
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow constructor FAILED: {ex.Message}");
            MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK);
            throw;
        }

        // Create services
        _settingsService = new SettingsService();
        _storageService = new RecordingStorageService(_settingsService);
        var audioRecordingService = new AudioRecordingService(_storageService);
        var audioPlaybackService = new AudioPlaybackService();
        _dragDropService = new DragDropService();

        // Create and set ViewModel
        _viewModel = new MainViewModel(
            audioRecordingService,
            audioPlaybackService,
            _storageService,
            _dragDropService
        );
        DataContext = _viewModel;
        System.Diagnostics.Debug.WriteLine($"ViewModel created and DataContext set. State={_viewModel.State}, ElapsedSeconds={_viewModel.ElapsedSeconds}");
        
        // Configure window properties
        Title = "DiscordVocalOverlay";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        
        // Don't set WindowStartupLocation to prevent recentering
        // The window will use its default positioning from XAML

        // Subscribe to ViewModel events
        _viewModel.RequestClose += OnRequestClose;
        _viewModel.RequestDragStart += OnRequestDragStart;
        _viewModel.RequestCopyFile += OnRequestCopyFile;
        _viewModel.RequestShowDeviceSelection += OnRequestShowDeviceSelection;
        _viewModel.RequestShowSettings += OnRequestShowSettings;

        // Subscribe to ViewModel property changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            try
            {
                if (e.PropertyName == nameof(MainViewModel.State))
                {
                    UpdateUIState();
                }
                else if (e.PropertyName == nameof(MainViewModel.TimerText))
                {
                    TimerText.Text = _viewModel.TimerText;
                }
                else if (e.PropertyName == nameof(MainViewModel.AudioLevel))
                {
                    UpdateAudioLevelMeter();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PropertyChanged handler exception: {ex.Message}");
            }
        };

        // Set initial UI state
        UpdateUIState();
        System.Diagnostics.Debug.WriteLine("MainWindow constructor completed");
    }

    private void UpdateUIState()
    {
        System.Diagnostics.Debug.WriteLine($"UpdateUIState called: State={_viewModel.State}, ElapsedSeconds={_viewModel.ElapsedSeconds}");
        
        switch (_viewModel.State)
        {
            case MainViewModel.OverlayState.Idle:
                // Status bar
                StatusText.Text = "Ready to Record";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 101, 242)); // #5865F2
                
                // State icon
                StateIcon.Text = "🎤";
                
                // Timer
                TimerText.Text = "00:00";
                TimerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(185, 187, 190)); // #B9BBBE
                
                // Buttons
                RecordButton.IsEnabled = true;
                RecordButton.Style = (Style)FindResource("PrimaryButton");
                PlayButton.IsEnabled = false;
                PlayButton.Style = (Style)FindResource("PrimaryButton");
                PlayButtonIcon.Text = "▶";
                PlayButtonText.Text = "Play";
                DeleteButton.IsEnabled = false;
                BackButton.IsEnabled = false;
                CopyButton.IsEnabled = false;
                CopyButton.Visibility = System.Windows.Visibility.Hidden;

                // Audio level meter
                AudioLevelBorder.Visibility = System.Windows.Visibility.Hidden;
                break;
                
            case MainViewModel.OverlayState.Recording:
                // Status bar
                StatusText.Text = "Recording...";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 66, 69)); // #ED4245
                
                // State icon
                StateIcon.Text = "🔴";
                
                // Timer
                TimerText.Text = _viewModel.TimerText;
                TimerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 66, 69)); // #ED4245
                
                // Buttons
                RecordButton.IsEnabled = true;
                RecordButton.Style = (Style)FindResource("DangerButton");
                PlayButton.IsEnabled = false;
                PlayButton.Style = (Style)FindResource("PrimaryButton");
                PlayButtonIcon.Text = "▶";
                PlayButtonText.Text = "Play";
                DeleteButton.IsEnabled = false;
                BackButton.IsEnabled = false;
                CopyButton.IsEnabled = false;
                CopyButton.Visibility = System.Windows.Visibility.Hidden;

                // Audio level meter
                AudioLevelBorder.Visibility = System.Windows.Visibility.Visible;
                break;
                
            case MainViewModel.OverlayState.RecordedReady:
                // Status bar
                StatusText.Text = "Recording Ready";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(87, 242, 135)); // #57F287
                
                // State icon
                StateIcon.Text = "✅";
                
                // Timer
                TimerText.Text = _viewModel.TimerText;
                TimerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(185, 187, 190)); // #B9BBBE
                
                // Buttons
                RecordButton.IsEnabled = false;
                PlayButton.IsEnabled = true;
                PlayButton.Style = (Style)FindResource("PrimaryButton");
                PlayButtonIcon.Text = "▶";
                PlayButtonText.Text = "Play";
                DeleteButton.IsEnabled = true;
                BackButton.IsEnabled = true;
                CopyButton.IsEnabled = true;
                CopyButton.Visibility = System.Windows.Visibility.Visible;

                // Audio level meter
                AudioLevelBorder.Visibility = System.Windows.Visibility.Hidden;
                break;
                
            case MainViewModel.OverlayState.Playing:
                // Status bar
                StatusText.Text = "Playing...";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 101, 242)); // #5865F2
                
                // State icon
                StateIcon.Text = "🔊";
                
                // Timer
                TimerText.Text = _viewModel.TimerText;
                TimerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 101, 242)); // #5865F2
                
                // Buttons
                RecordButton.IsEnabled = false;
                PlayButton.IsEnabled = true;
                PlayButton.Style = (Style)FindResource("DangerButton");
                PlayButtonIcon.Text = "■";
                PlayButtonText.Text = "Stop";
                DeleteButton.IsEnabled = false;
                BackButton.IsEnabled = false;
                CopyButton.IsEnabled = false;
                CopyButton.Visibility = System.Windows.Visibility.Hidden;

                // Audio level meter
                AudioLevelBorder.Visibility = System.Windows.Visibility.Hidden;
                break;
        }
    }

    private void UpdateAudioLevelMeter()
    {
        var level = _viewModel.AudioLevel;
        var height = Math.Max(4, level * 40); // Minimum 4px, maximum 40px
        AudioLevelBar.Height = height;

        // Change color based on level
        if (level > 0.7f)
        {
            AudioLevelBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 66, 69)); // Red for high levels
        }
        else if (level > 0.4f)
        {
            AudioLevelBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 166, 26)); // Orange for medium levels
        }
        else
        {
            AudioLevelBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 101, 242)); // Blue for low levels
        }
    }

    // Dragging logic - use WPF's built-in DragMove for smooth dragging
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Only start dragging if not clicking on a button
        if (e.Source is Button)
            return;

        // Use WPF's built-in DragMove for smooth window dragging
        try
        {
            DragMove();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DragMove failed: {ex.Message}");
        }
    }

    // Event handlers from ViewModel
    private void OnRequestClose()
    {
        System.Diagnostics.Debug.WriteLine("OnRequestClose called");
        Close();
    }

    private void OnRequestDragStart(string filePath)
    {
        System.Diagnostics.Debug.WriteLine($"OnRequestDragStart called with file: {filePath}");
        _dragDropService.StartFileDropDrag(this, filePath);
    }

    private void OnRequestCopyFile(string filePath)
    {
        System.Diagnostics.Debug.WriteLine($"OnRequestCopyFile called with file: {filePath}");
        try
        {
            // Copy the file to clipboard as a file path
            var fileDropList = new System.Collections.Specialized.StringCollection();
            fileDropList.Add(filePath);
            System.Windows.Clipboard.SetFileDropList(fileDropList);

            // Show notification
            System.Windows.MessageBox.Show("File copied to clipboard!\n\nYou can now paste it in Discord (Ctrl+V).", "Copied to Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to copy file to clipboard: {ex.Message}");
            System.Windows.MessageBox.Show($"Failed to copy file to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnRequestShowDeviceSelection()
    {
        System.Diagnostics.Debug.WriteLine("OnRequestShowDeviceSelection called");
        ShowDeviceSelectionDialog();
    }

    private void OnRequestShowSettings()
    {
        System.Diagnostics.Debug.WriteLine("OnRequestShowSettings called");
        ShowSettingsDialog();
    }

    private void ShowSettingsDialog()
    {
        var dialog = new SettingsWindow(_storageService)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void ShowDeviceSelectionDialog()
    {
        var dialog = new Window
        {
            Title = "Select Audio Input Device",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.SingleBorderWindow
        };

        var grid = new Grid();
        dialog.Content = grid;

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20)
        };

        // Label
        var label = new TextBlock
        {
            Text = "Select Microphone:",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        stackPanel.Children.Add(label);

        // ComboBox
        var comboBox = new ComboBox
        {
            Width = 350,
            Height = 30,
            FontSize = 12
        };

        // Populate with available devices
        var devices = _viewModel.AvailableInputDevices;
        foreach (var device in devices)
        {
            comboBox.Items.Add(device);
        }

        // Set selected device
        if (!string.IsNullOrEmpty(_viewModel.SelectedInputDevice))
        {
            comboBox.SelectedItem = _viewModel.SelectedInputDevice;
        }
        else
        {
            // Default to first device
            comboBox.SelectedIndex = 0;
        }

        stackPanel.Children.Add(comboBox);

        // OK button
        var okButton = new Button
        {
            Content = "OK",
            Width = 100,
            Height = 30,
            Margin = new Thickness(0, 20, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        okButton.Click += (s, e) =>
        {
            _viewModel.SelectedInputDevice = comboBox.SelectedItem as string;
            dialog.Close();
        };

        stackPanel.Children.Add(okButton);

        grid.Children.Add(stackPanel);

        dialog.ShowDialog();
    }

    // Cleanup on window closing
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MainWindow closing");
        _viewModel.RequestClose -= OnRequestClose;
        _viewModel.RequestDragStart -= OnRequestDragStart;
        _viewModel.RequestShowDeviceSelection -= OnRequestShowDeviceSelection;
        _viewModel.RequestShowSettings -= OnRequestShowSettings;
        base.OnClosing(e);
    }
}
