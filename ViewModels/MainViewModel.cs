using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using DiscordVocalOverlay.Services;

namespace DiscordVocalOverlay.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // State enum (as per spec)
    public enum OverlayState
    {
        Idle,
        Recording,
        RecordedReady,
        Playing
    }

    // State property
    private OverlayState _state = OverlayState.Idle;
    public OverlayState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIdle));
                OnPropertyChanged(nameof(IsRecording));
                OnPropertyChanged(nameof(IsRecordedReady));
                OnPropertyChanged(nameof(IsPlaying));
                UpdateCommandStates();
            }
        }
    }

    // Helper properties for XAML binding
    public bool IsIdle => State == OverlayState.Idle;
    public bool IsRecording => State == OverlayState.Recording;
    public bool IsRecordedReady => State == OverlayState.RecordedReady;
    public bool IsPlaying => State == OverlayState.Playing;

    // Recording timer
    private int _elapsedSeconds = 0;
    public int ElapsedSeconds
    {
        get => _elapsedSeconds;
        private set
        {
            if (_elapsedSeconds != value)
            {
                _elapsedSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimerText));
            }
        }
    }

    // Timer text in mm:ss format
    public string TimerText => $"{ElapsedSeconds / 60:D2}:{ElapsedSeconds % 60:D2}";

    // Last recorded file path
    private string? _lastRecordedFilePath;
    public string? LastRecordedFilePath
    {
        get => _lastRecordedFilePath;
        private set
        {
            if (_lastRecordedFilePath != value)
            {
                _lastRecordedFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    // Audio input device selection
    private List<string>? _availableInputDevices;
    public List<string> AvailableInputDevices
    {
        get => _availableInputDevices ??= _audioRecordingService.GetAvailableInputDevices();
        private set
        {
            if (_availableInputDevices != value)
            {
                _availableInputDevices = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _selectedInputDevice;
    public string? SelectedInputDevice
    {
        get => _selectedInputDevice;
        set
        {
            if (_selectedInputDevice != value)
            {
                _selectedInputDevice = value;
                OnPropertyChanged();
                UpdateSelectedDevice();
            }
        }
    }

    // Audio level (0.0 to 1.0) for visualization
    private float _audioLevel = 0.0f;
    public float AudioLevel
    {
        get => _audioLevel;
        private set
        {
            if (_audioLevel != value)
            {
                _audioLevel = value;
                OnPropertyChanged();
            }
        }
    }

    // Commands
    public ICommand RecordToggleCommand { get; }
    public ICommand PlayToggleCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand BackToIdleCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand DragStartCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand ShowDeviceSelectionCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand CopyFileCommand { get; }

    // Services
    private readonly AudioRecordingService _audioRecordingService;
    private readonly AudioPlaybackService _audioPlaybackService;
    private readonly RecordingStorageService _storageService;
    private readonly DragDropService _dragDropService;

    // Dispatcher for UI thread marshaling
    private readonly Dispatcher _dispatcher;

    public MainViewModel(
        AudioRecordingService audioRecordingService,
        AudioPlaybackService audioPlaybackService,
        RecordingStorageService storageService,
        DragDropService dragDropService)
    {
        _audioRecordingService = audioRecordingService;
        _audioPlaybackService = audioPlaybackService;
        _storageService = storageService;
        _dragDropService = dragDropService;
        _dispatcher = Dispatcher.CurrentDispatcher;

        // Subscribe to audio recording events
        _audioRecordingService.RecordingProgress += OnRecordingProgress;
        _audioRecordingService.RecordingCompleted += OnRecordingCompleted;
        _audioRecordingService.RecordingStopped += OnRecordingStopped;
        _audioRecordingService.AudioLevelChanged += OnAudioLevelChanged;
        System.Diagnostics.Debug.WriteLine("AudioLevelChanged subscription added");

        // Subscribe to audio playback events
        _audioPlaybackService.PlaybackStarted += OnPlaybackStarted;
        _audioPlaybackService.PlaybackStopped += OnPlaybackStopped;
        _audioPlaybackService.Error += OnPlaybackError;

        // Initialize commands
        RecordToggleCommand = new RelayCommand(RecordToggle, CanRecordToggle);
        PlayToggleCommand = new RelayCommand(PlayToggle, CanPlayToggle);
        DeleteCommand = new RelayCommand(Delete, CanDelete);
        BackToIdleCommand = new RelayCommand(BackToIdle, CanBackToIdle);
        ShowDeviceSelectionCommand = new RelayCommand(ShowDeviceSelection, CanShowDeviceSelection);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        CloseCommand = new RelayCommand(Close, CanClose);
        DragStartCommand = new RelayCommand(DragStart, CanDrag);
        OpenFolderCommand = new RelayCommand(OpenFolder, CanOpenFolder);
        CopyFileCommand = new RelayCommand(CopyFile, CanCopyFile);
    }

    private void UpdateCommandStates()
    {
        ((RelayCommand)RecordToggleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PlayToggleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BackToIdleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CloseCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DragStartCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenFolderCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CopyFileCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowDeviceSelectionCommand).RaiseCanExecuteChanged();
    }

    // Command implementations
    private void RecordToggle()
    {
        if (State == OverlayState.Recording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private bool CanRecordToggle()
    {
        return State == OverlayState.Idle || State == OverlayState.Recording;
    }

    private void PlayToggle()
    {
        if (State == OverlayState.Playing)
        {
            StopPlayback();
        }
        else
        {
            StartPlayback();
        }
    }

    private bool CanPlayToggle()
    {
        return (State == OverlayState.RecordedReady || State == OverlayState.Playing) && 
               !string.IsNullOrEmpty(LastRecordedFilePath);
    }

    private void Delete()
    {
        if (!string.IsNullOrEmpty(LastRecordedFilePath))
        {
            _storageService.Delete(LastRecordedFilePath);
        }
        LastRecordedFilePath = null;
        ElapsedSeconds = 0;
        State = OverlayState.Idle;
    }

    private bool CanDelete()
    {
        return State == OverlayState.RecordedReady;
    }

    private void Close()
    {
        // Stop recording if active
        if (State == OverlayState.Recording)
        {
            _audioRecordingService.StopRecording();
        }

        // Stop playback if active
        if (State == OverlayState.Playing)
        {
            _audioPlaybackService.Stop();
        }

        // Clean up partial file if exists
        if (!string.IsNullOrEmpty(LastRecordedFilePath) && State == OverlayState.Recording)
        {
            _storageService.Delete(LastRecordedFilePath);
        }

        State = OverlayState.Idle;
        LastRecordedFilePath = null;
        ElapsedSeconds = 0;

        // Request window close (handled by code-behind)
        RequestClose?.Invoke();
    }

    private bool CanClose()
    {
        return true; // Can always close
    }

    private void DragStart()
    {
        if (!string.IsNullOrEmpty(LastRecordedFilePath))
        {
            RequestDragStart?.Invoke(LastRecordedFilePath);
        }
    }

    private bool CanDrag()
    {
        return State == OverlayState.RecordedReady && !string.IsNullOrEmpty(LastRecordedFilePath);
    }

    private void OpenFolder()
    {
        try
        {
            var folderPath = _storageService.GetOutputDirectory();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open folder: {ex.Message}");
        }
    }

    private bool CanOpenFolder()
    {
        return true; // Can always open folder
    }

    private void CopyFile()
    {
        if (!string.IsNullOrEmpty(LastRecordedFilePath))
        {
            RequestCopyFile?.Invoke(LastRecordedFilePath);
        }
    }

    private bool CanCopyFile()
    {
        return State == OverlayState.RecordedReady && !string.IsNullOrEmpty(LastRecordedFilePath);
    }

    private void ShowDeviceSelection()
    {
        RequestShowDeviceSelection?.Invoke();
    }

    private bool CanShowDeviceSelection()
    {
        return true; // Can always show device selection
    }

    private void ShowSettings()
    {
        RequestShowSettings?.Invoke();
    }

    private void BackToIdle()
    {
        // Transition back to Idle state without deleting the file
        LastRecordedFilePath = null;
        State = OverlayState.Idle;
    }

    private bool CanBackToIdle()
    {
        return State == OverlayState.RecordedReady;
    }

    // Event handlers
    private void OnRecordingProgress(int elapsed)
    {
        _dispatcher.Invoke(() =>
        {
            ElapsedSeconds = elapsed;
        });
    }

    private void OnRecordingCompleted(string filePath)
    {
        _dispatcher.Invoke(() =>
        {
            LastRecordedFilePath = filePath;
            State = OverlayState.RecordedReady;
        });
    }

    private void OnRecordingStopped()
    {
        _dispatcher.Invoke(() =>
        {
            if (State == OverlayState.Recording)
            {
                State = OverlayState.Idle;
            }
        });
    }

    private void OnPlaybackStarted()
    {
        _dispatcher.Invoke(() =>
        {
            State = OverlayState.Playing;
        });
    }

    private void OnPlaybackStopped()
    {
        _dispatcher.Invoke(() =>
        {
            if (State == OverlayState.Playing)
            {
                State = OverlayState.RecordedReady;
            }
        });
    }

    private void OnPlaybackError(Exception ex)
    {
        _dispatcher.Invoke(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
            if (State == OverlayState.Playing)
            {
                State = OverlayState.RecordedReady;
            }
        });
    }

    // Private methods

    private void OnAudioLevelChanged(float level)
    {
        _dispatcher.Invoke(() =>
        {
            System.Diagnostics.Debug.WriteLine($"OnAudioLevelChanged: {level:F3}");
            AudioLevel = level;
        });
    }
    private void StartRecording()
    {
        ElapsedSeconds = 0;
        State = OverlayState.Recording;
        _audioRecordingService.StartRecordingAsync();
    }

    private void StopRecording()
    {
        _audioRecordingService.StopRecording();
    }

    private void StartPlayback()
    {
        if (!string.IsNullOrEmpty(LastRecordedFilePath))
        {
            _audioPlaybackService.Play(LastRecordedFilePath);
        }
    }

    private void StopPlayback()
    {
        _audioPlaybackService.Stop();
    }

    private void UpdateSelectedDevice()
    {
        if (string.IsNullOrEmpty(_selectedInputDevice))
        {
            // Default device (Windows default)
            _audioRecordingService.SelectedDeviceNumber = -1;
        }
        else
        {
            // Find device number by name
            var devices = _audioRecordingService.GetAvailableInputDevices();
            var deviceIndex = devices.IndexOf(_selectedInputDevice);
            _audioRecordingService.SelectedDeviceNumber = deviceIndex >= 0 ? deviceIndex : -1;
        }
    }

    // Events for code-behind
    public event Action? RequestClose;
    public event Action<string>? RequestDragStart;
    public event Action<string>? RequestCopyFile;
    public event Action? RequestShowDeviceSelection;
    public event Action? RequestShowSettings;
}
