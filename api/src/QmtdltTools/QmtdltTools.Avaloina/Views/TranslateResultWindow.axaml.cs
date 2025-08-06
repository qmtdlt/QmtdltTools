using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QmtdltTools.Avaloina.Services;
using QmtdltTools.Avaloina.ViewModels;
using QmtdltTools.Domain.Entitys;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class TranslateResultWindow : Window,ITransientDependency
{
    private readonly TransRestService _transService;
    private MiniAudioEngine? _audioEngine;
    private AudioPlaybackDevice? _playbackDevice;
    private SoundPlayer? _player;
    public TranslateResultWindow(TransRestService transService)
    {
        InitializeComponent();
        _transService = transService;
        this.Closing += (sender, args) =>
        {
            StopAudio();
        };
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
    VocabularyRecord _data;
    public void setData(VocabularyRecord data)
    {
        _data = data;
        wordText.Text = data.WordText;
        aITranslation.Text = data.AITranslation;
        aIExplanation.Text = data.AIExplanation;
    }

    public void PlayAudio(byte[] wavData)
    {
        StopAudio(); // 播放前先停止上一次播放

        _audioEngine = new MiniAudioEngine();
        var device = _audioEngine.PlaybackDevices.FirstOrDefault(d => d.IsDefault);
        if (device.Id == IntPtr.Zero)
            return;

        var audioFormat = AudioFormat.Cd; // 根据实际格式调整
        _playbackDevice = _audioEngine.InitializePlaybackDevice(device, audioFormat);
        var ms = new MemoryStream(wavData);
        var provider = new StreamDataProvider(_audioEngine, audioFormat, ms);
        _player = new SoundPlayer(_audioEngine, audioFormat, provider);

        _playbackDevice.MasterMixer.AddComponent(_player);
        _playbackDevice.Start();
        _player.Play();
    }

    public void StopAudio()
    {
        try
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;

            _playbackDevice?.Stop();
            _playbackDevice?.Dispose();
            _playbackDevice = null;

            _audioEngine?.Dispose();
            _audioEngine = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StopAudio error: {ex.Message}");
        }
    }

    private void stopAudio(object sender, RoutedEventArgs e)
    {
        StopAudio();
    }
    
    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // 双击
        if (sender is TextBox textBox)
        {
            string selectedText = textBox.SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
            {
                _ = _transService.Trans(selectedText);
            }
        }
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 
        if (sender is TextBox textBox)
        {
            string selectedText = textBox.SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
            {
                _ = _transService.Trans(selectedText);
            }
        }
    }

    private void playWord(object? sender, RoutedEventArgs e)
    {
        if (_data.WordPronunciation != null)
        {
            PlayAudio(_data.WordPronunciation);
        }
    }
}
