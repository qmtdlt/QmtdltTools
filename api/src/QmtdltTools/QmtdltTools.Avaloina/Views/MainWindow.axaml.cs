using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using QmtdltTools.Avaloina.Services;
using QmtdltTools.Avaloina.Utils;
using QmtdltTools.Avaloina.ViewModels;
using ReactiveUI;
using System.Collections.Concurrent;
using System.Reactive;
using System;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class MainWindow : Window
{
    private readonly TransRestService _transService;
    public MainWindow(TransRestService transService, MainWindowViewModel vm)
    {
        InitializeComponent();
        _transService = transService;
        DataContext = vm;
        this.Closing += MainWindow_Closing;
        
        localVideoView.InitAction(vm.updatingTitle, vm.SetSubTitle);
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        localVideoView.onClose();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _ = RestHelper.login("qmtdlt", "12000asd");                     // 登录默认用户，后续需要修改为使用自己的账户登录
    }
    private void TextBox_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            string selectedText = tb.SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
            {
                _ = _transService.Trans(selectedText);
            }
        }
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            localVideoView.PauseMedia();
        }
        if (e.Key == Key.Left)
        {
            localVideoView.GoLastSentence();
        }
        if (e.Key == Key.Right)
        {
            localVideoView.GoNextSentence();
        }
        if (e.Key == Key.Up)
        {
            localVideoView.RepeatOne();
        }
        if (e.Key == Key.Down)
        {
            localVideoView.CancelRepeat();
        }
    }

    private void InputElement_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb)
        {
            string selectedText = tb.SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
            {
                _ = _transService.Trans(selectedText);
            }
        }
    }
}
