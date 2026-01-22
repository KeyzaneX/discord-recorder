using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using DiscordVocalOverlay.Services;

namespace DiscordVocalOverlay.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly RecordingStorageService _storageService;
        private string _recordingsFolder;

        public SettingsViewModel(RecordingStorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _recordingsFolder = _storageService.GetRecordingsFolder();

            BrowseFolderCommand = new RelayCommand(BrowseFolder);
        }

        public string RecordingsFolder
        {
            get => _recordingsFolder;
            set
            {
                if (_recordingsFolder != value)
                {
                    _recordingsFolder = value;
                    OnPropertyChanged(nameof(RecordingsFolder));
                    _storageService.SetRecordingsFolder(value);
                }
            }
        }

        public ICommand BrowseFolderCommand { get; }

        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Recordings Folder",
                InitialDirectory = RecordingsFolder
            };

            if (dialog.ShowDialog() == true)
            {
                RecordingsFolder = dialog.FolderName;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
