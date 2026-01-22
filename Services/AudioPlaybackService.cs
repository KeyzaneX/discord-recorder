using System;
using NAudio.Wave;

namespace DiscordVocalOverlay.Services;

public class AudioPlaybackService
{
    private AudioFileReader? _audioFileReader;
    private IWavePlayer? _wavePlayer;
    private bool _isPlaying;

    public event Action? PlaybackStarted;
    public event Action? PlaybackStopped;
    public event Action<Exception>? Error;

    public bool IsPlaying => _isPlaying;

    public void Play(string filePath)
    {
        if (_isPlaying)
        {
            Stop();
        }

        try
        {
            _audioFileReader = new AudioFileReader(filePath);
            // Use WaveOut instead of WaveOutEvent to avoid opening new window
            _wavePlayer = new WaveOut();
            _wavePlayer.Init(_audioFileReader);
            _wavePlayer.PlaybackStopped += OnPlaybackStopped;
            _wavePlayer.Play();
            _isPlaying = true;
            PlaybackStarted?.Invoke();
        }
        catch (Exception ex)
        {
            Dispose();
            Error?.Invoke(ex);
        }
    }

    public void Stop()
    {
        if (_wavePlayer != null)
        {
            _wavePlayer.Stop();
        }
        // Don't dispose here - let OnPlaybackStopped handle it
        // This ensures the PlaybackStopped event is raised
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _isPlaying = false;
        PlaybackStopped?.Invoke();
        Dispose();
    }

    private void Dispose()
    {
        _wavePlayer?.Dispose();
        _wavePlayer = null;
        _audioFileReader?.Dispose();
        _audioFileReader = null;
        _isPlaying = false;
    }
}
