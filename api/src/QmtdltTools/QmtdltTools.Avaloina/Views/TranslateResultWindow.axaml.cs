using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QmtdltTools.Avaloina.Services;
using QmtdltTools.Avaloina.ViewModels;
using QmtdltTools.Domain.Entitys;
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
    public TranslateResultWindow(TransRestService transService)
    {
        InitializeComponent();
        _transService = transService;
        this.Closing += (sender, args) =>
        {
            StopAudio();
        };
    }
    VocabularyRecord _data;
    public void setData(VocabularyRecord data)
    {
        _data = data;
        wordText.Text = data.WordText;
        aITranslation.Text = data.AITranslation;
        aIExplanation.Text = data.AIExplanation;
    }

    public void PlayAudio(byte[] audioData)
    {
        using var audioEngine = new MiniAudioEngine();

        var defaultPlaybackDevice = audioEngine.PlaybackDevices.FirstOrDefault(d => d.IsDefault);
        if (defaultPlaybackDevice.Id == IntPtr.Zero)
        {
            return;
        }

        var audioFormat = new AudioFormat;

        using var device = audioEngine.InitializePlaybackDevice(defaultPlaybackDevice, audioFormat);

        using var ms = new MemoryStream(audioData);

        using var dataProvider = new StreamDataProvider(audioEngine, audioFormat, ms);

        using var player = new SoundPlayer(audioEngine, audioFormat, dataProvider);

        device.MasterMixer.AddComponent(player);

        device.Start();

        player.Play();

        while (player.State != PlaybackState.Stopped)
        {
            Thread.Sleep(100);
        }

        Console.WriteLine("");
    }

    public void StopAudio()
    {
        
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
