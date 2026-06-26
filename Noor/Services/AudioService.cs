using System.IO;
using NAudio.Wave;

namespace Noor.Services;

public class AudioService
{
    private string _azanFilePath;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private bool _isPlaying;

    public AudioService(string azanFilePath = "")
    {
        _azanFilePath = azanFilePath;
    }

    public void UpdateFilePath(string filePath)
    {
        _azanFilePath = filePath;
    }

    public void PlayAzan(int volume = 80)
    {
        if (_isPlaying || string.IsNullOrWhiteSpace(_azanFilePath) || !File.Exists(_azanFilePath))
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Cannot play: exists={File.Exists(_azanFilePath)}, path={_azanFilePath}");
            return;
        }

        try
        {
            Stop();

            _audioFile = new AudioFileReader(_azanFilePath)
            {
                Volume = volume / 100f
            };

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFile);
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Play();

            _isPlaying = true;
            System.Diagnostics.Debug.WriteLine("[AudioService] Azan playback started");
        }
        catch (Exception ex)
        {
            _isPlaying = false;
            System.Diagnostics.Debug.WriteLine($"[AudioService] Error playing azan: {ex.Message}");
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _isPlaying = false;
        System.Diagnostics.Debug.WriteLine("[AudioService] Azan playback completed");

        if (e.Exception != null)
            System.Diagnostics.Debug.WriteLine($"[AudioService] Playback error: {e.Exception.Message}");
    }

    public void Stop()
    {
        try
        {
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            if (_audioFile != null)
            {
                _audioFile.Dispose();
                _audioFile = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Error stopping: {ex.Message}");
        }

        _isPlaying = false;
    }

    public bool IsPlaying => _isPlaying;
}
