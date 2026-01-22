using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace DiscordVocalOverlay.Services;

public class AudioRecordingService
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _waveWriter;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _recordingTask;
    private string? _currentFilePath;
    private readonly RecordingStorageService _storageService;
    private int? _selectedDeviceNumber;
    private float _recordingGain = 1.5f; // Boost recording volume by 50%
    private bool _enableHighPassFilter = true;
    private bool _enableNoiseGate = true;
    private float _highPassCutoffHz = 100.0f;
    private float _noiseGateThreshold = 0.02f;
    private float _noiseGateAttackSeconds = 0.005f;
    private float _noiseGateReleaseSeconds = 0.05f;
    private float _highPassAlpha;
    private float _highPassPrevInput;
    private float _highPassPrevOutput;
    private float _noiseGateGain;
    private float _noiseGateAttackCoeff;
    private float _noiseGateReleaseCoeff;

    public event Action<int>? RecordingProgress;
    public event Action<string>? RecordingCompleted;
    public event Action? RecordingStopped;
    public event Action<float>? AudioLevelChanged; // Audio level (0.0 to 1.0)

    private const int MaxDurationSeconds = 60;
    private const int SampleRate = 48000;
    private const int Channels = 1; // Mono
    private const int BitDepth = 16;
    private const float MeterFloorDb = -80.0f;
    private const float MeterResponse = 0.5f;

    public float RecordingGain
    {
        get => _recordingGain;
        set => _recordingGain = value;
    }

    public bool EnableHighPassFilter
    {
        get => _enableHighPassFilter;
        set => _enableHighPassFilter = value;
    }

    public bool EnableNoiseGate
    {
        get => _enableNoiseGate;
        set => _enableNoiseGate = value;
    }

    public float HighPassCutoffHz
    {
        get => _highPassCutoffHz;
        set
        {
            _highPassCutoffHz = Math.Clamp(value, 20.0f, 1000.0f);
            UpdateHighPassCoefficient();
        }
    }

    public float NoiseGateThreshold
    {
        get => _noiseGateThreshold;
        set => _noiseGateThreshold = Math.Clamp(value, 0.0f, 1.0f);
    }

    public float NoiseGateAttackSeconds
    {
        get => _noiseGateAttackSeconds;
        set
        {
            _noiseGateAttackSeconds = Math.Max(value, 0.001f);
            UpdateNoiseGateCoefficients();
        }
    }

    public float NoiseGateReleaseSeconds
    {
        get => _noiseGateReleaseSeconds;
        set
        {
            _noiseGateReleaseSeconds = Math.Max(value, 0.001f);
            UpdateNoiseGateCoefficients();
        }
    }

    public AudioRecordingService(RecordingStorageService storageService)
    {
        _storageService = storageService;
        UpdateHighPassCoefficient();
        UpdateNoiseGateCoefficients();
    }

    public List<string> GetAvailableInputDevices()
    {
        var devices = new List<string>();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            devices.Add(WaveIn.GetCapabilities(i).ProductName);
        }
        return devices;
    }

    public int SelectedDeviceNumber
    {
        get => _selectedDeviceNumber ?? -1; // -1 means default device
        set => _selectedDeviceNumber = value;
    }

    public void StartRecordingAsync()
    {
        if (_waveIn != null)
            return;

        _cancellationTokenSource = new CancellationTokenSource();
        _currentFilePath = _storageService.CreateNewRecordingPath();

        // Initialize audio capture
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, BitDepth, Channels),
            DeviceNumber = _selectedDeviceNumber ?? -1 // -1 means default device
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        // Create wave file writer
        _waveWriter = new WaveFileWriter(_currentFilePath, _waveIn.WaveFormat);

        ResetProcessingState();

        // Start recording
        _waveIn.StartRecording();

        // Start timer for progress updates
        _recordingTask = Task.Run(() => RecordingTimer(_cancellationTokenSource.Token));
    }

    public void StopRecording()
    {
        if (_waveIn == null)
            return;

        _cancellationTokenSource?.Cancel();

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
    }

    private async Task RecordingTimer(CancellationToken cancellationToken)
    {
        int elapsed = 0;
        
        System.Diagnostics.Debug.WriteLine("RecordingTimer started");
        
        while (!cancellationToken.IsCancellationRequested && elapsed < MaxDurationSeconds)
        {
            System.Diagnostics.Debug.WriteLine($"RecordingProgress: {elapsed}s");
            RecordingProgress?.Invoke(elapsed);
            await Task.Delay(1000, cancellationToken);
            elapsed++;
        }

        if (elapsed >= MaxDurationSeconds && _waveIn != null)
        {
            // Auto-stop when max duration reached
            System.Diagnostics.Debug.WriteLine("Auto-stopping recording at max duration");
            StopRecording();
        }
        
        System.Diagnostics.Debug.WriteLine("RecordingTimer ended");
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveWriter == null)
            return;

        int sampleCount = e.BytesRecorded / 2;
        double sumSquares = 0.0;

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short rawSample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
            float sample = rawSample / 32768.0f;

            if (_enableHighPassFilter)
                sample = ApplyHighPassFilter(sample);

            sample *= _recordingGain;

            if (_enableNoiseGate)
                sample = ApplyNoiseGate(sample);

            sample = Math.Clamp(sample, -1.0f, 1.0f);
            sumSquares += sample * sample;

            short outputSample = (short)(sample * 32767.0f);
            e.Buffer[i] = (byte)(outputSample & 0xFF);
            e.Buffer[i + 1] = (byte)((outputSample >> 8) & 0xFF);
        }

        float audioLevel = CalculateRmsLevel(sumSquares, sampleCount);
        System.Diagnostics.Debug.WriteLine($"AudioLevel: {audioLevel:F3}");
        AudioLevelChanged?.Invoke(audioLevel);

        _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
    }

    private static float CalculateRmsLevel(double sumSquares, int sampleCount)
    {
        if (sampleCount <= 0)
            return 0.0f;

        double rms = Math.Sqrt(sumSquares / sampleCount);
        if (rms <= 0.0)
            return 0.0f;

        double db = 20.0 * Math.Log10(rms);
        double normalized = (db - MeterFloorDb) / -MeterFloorDb;
        normalized = Math.Pow(Math.Clamp(normalized, 0.0, 1.0), MeterResponse);
        return (float)normalized;
    }

    private void ResetProcessingState()
    {
        _highPassPrevInput = 0.0f;
        _highPassPrevOutput = 0.0f;
        _noiseGateGain = 0.0f;
    }

    private void UpdateHighPassCoefficient()
    {
        float cutoff = Math.Clamp(_highPassCutoffHz, 20.0f, 1000.0f);
        float rc = 1.0f / (2.0f * (float)Math.PI * cutoff);
        float dt = 1.0f / SampleRate;
        _highPassAlpha = rc / (rc + dt);
    }

    private void UpdateNoiseGateCoefficients()
    {
        _noiseGateAttackCoeff = (float)Math.Exp(-1.0 / (SampleRate * _noiseGateAttackSeconds));
        _noiseGateReleaseCoeff = (float)Math.Exp(-1.0 / (SampleRate * _noiseGateReleaseSeconds));
    }

    private float ApplyHighPassFilter(float sample)
    {
        float output = _highPassAlpha * (_highPassPrevOutput + sample - _highPassPrevInput);
        _highPassPrevInput = sample;
        _highPassPrevOutput = output;
        return output;
    }

    private float ApplyNoiseGate(float sample)
    {
        float level = Math.Abs(sample);
        float target = level >= _noiseGateThreshold ? 1.0f : 0.0f;

        if (target > _noiseGateGain)
            _noiseGateGain = (1.0f - _noiseGateAttackCoeff) * target + _noiseGateAttackCoeff * _noiseGateGain;
        else
            _noiseGateGain = (1.0f - _noiseGateReleaseCoeff) * target + _noiseGateReleaseCoeff * _noiseGateGain;

        return sample * _noiseGateGain;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (_waveWriter != null)
        {
            _waveWriter.Dispose();
            _waveWriter = null;
        }

        RecordingStopped?.Invoke();

        if (!string.IsNullOrEmpty(_currentFilePath) && _storageService.FileExists(_currentFilePath))
        {
            RecordingCompleted?.Invoke(_currentFilePath!);
        }
        else
        {
            // Clean up partial file if recording was cancelled
            System.Diagnostics.Debug.WriteLine($"Recording cancelled, file may be incomplete: {_currentFilePath}");
        }
    }
}
