using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QmtdltTools.Avaloina.Utils;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class SysSetting : Window,ITransientDependency
{
    public SysSetting(SysSettingVm vm)
    {
        InitializeComponent();
        vm.setWindow(this);
        DataContext = vm;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}

public class SysSettingVm: ReactiveObject, ITransientDependency
{
    public SysSettingVm()
    {
        OpenSubTitleApiKey = AppSettingHelper.OpenSubtitleApiKey;
        LocalSubtitlePath = AppSettingHelper.LastVideoSrt;
        SaveApiKeyCommand = ReactiveCommand.Create(saveApiKey);
        ChooseLocalSrtCmd = ReactiveCommand.Create(chooseLocalSrt);
    }

    private async void chooseLocalSrt()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择字幕文件",
            Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "字幕文件", Extensions = new List<string> { "srt" } }
                },
            AllowMultiple = false
        };

        var result = await dialog.ShowAsync(_window);
        if (result != null && result.Length > 0)
        {
            LocalSubtitlePath = result[0];
            AppSettingHelper.LastVideoSrt = result[0];
        }
    }

    private void saveApiKey()
    {
        AppSettingHelper.OpenSubtitleApiKey = OpenSubTitleApiKey;
    }
    SysSetting _window = null;
    internal void setWindow(SysSetting sysSetting)
    {
        _window = sysSetting;
    }

    public ICommand SaveApiKeyCommand { get; set; }
    public ICommand ChooseLocalSrtCmd { get; set; }

    private string _openSubTitleApiKey;
    public string OpenSubTitleApiKey
    {
        get { return _openSubTitleApiKey; }
        set
        {
            this.RaiseAndSetIfChanged(ref _openSubTitleApiKey, value);
        }
    }

    private string _localSubtitlePath;
    public string LocalSubtitlePath
    {
        get { return _localSubtitlePath; }
        set
        {
            this.RaiseAndSetIfChanged(ref _localSubtitlePath, value);
        }
    }
}