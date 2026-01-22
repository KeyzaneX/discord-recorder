using System.Windows;
using DiscordVocalOverlay.Services;

namespace DiscordVocalOverlay
{
    public partial class SettingsWindow : Window
    {
        private readonly RecordingStorageService _storageService;

        public SettingsWindow(RecordingStorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            InitializeComponent();
            DataContext = new ViewModels.SettingsViewModel(_storageService);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
