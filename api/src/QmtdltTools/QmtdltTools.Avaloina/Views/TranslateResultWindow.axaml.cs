using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QmtdltTools.Avaloina.Services;
using QmtdltTools.Avaloina.ViewModels;
using QmtdltTools.Domain.Entitys;
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
