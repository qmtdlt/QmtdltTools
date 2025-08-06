using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MovieCollection.OpenSubtitles.Models;
using MsBox.Avalonia;
using QmtdltTools.Avaloina.Utils;
using ReactiveUI;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;
using static System.Net.WebRequestMethods;

namespace QmtdltTools.Avaloina.Views;

public partial class ChooseSubtitle : Window,ITransientDependency
{
    public ChooseSubtitle(ChooseSubtitleVm vm)
    {
        InitializeComponent();
        DataContext = vm;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
    public async void SetMoviePath(string moviePath)
    {
        if (DataContext is ChooseSubtitleVm vm)
        {
            vm.SetMoviePath(moviePath);
        }   
    }
}

public class ChooseSubtitleVm : ReactiveObject, ITransientDependency
{
    OpenSubtitlesAPIService _openSubtitlesAPIService;
    public ICommand DownloadSubtitleCommand { get; set; }
    public ChooseSubtitleVm(OpenSubtitlesAPIService openSubtitlesAPIService)
    {
        _openSubtitlesAPIService = openSubtitlesAPIService;
        DownloadSubtitleCommand = ReactiveCommand.Create<Subtitle>(downloadSubtitle);         //<Subtitle>
    }
    public async void downloadSubtitle(Subtitle subtitle)
    {
        if (subtitle == null) return;
        AppSettingHelper.LastVideoSrt = await _openSubtitlesAPIService.DownloadSubtitle(subtitle.Files.FirstOrDefault().FileId, movieDir);          // 下载的字幕文件路径
        _ = MessageBoxManager.GetMessageBoxStandard("提示", $"字幕已保存：{AppSettingHelper.LastVideoSrt}").ShowWindowAsync();
    }
    public async void SetMoviePath(string moviePath)
    {
        try
        {
            PagedResult<AttributeResult<Subtitle>> page = await _openSubtitlesAPIService.SearchSubtitles(moviePath);
            List<Subtitle?>? list = page.Data?.Select(t => t.Attributes)?.ToList();
            if (list != null)
            {
                Subtitles = new ObservableCollection<Subtitle>(list ?? new List<Subtitle?>());


                var info = new FileInfo(moviePath);
                movieDir = info.Directory.Name;
            }
        }
        catch (Exception ex)
        {
            _ = MessageBoxManager.GetMessageBoxStandard("错误", $"获取字幕失败: {ex.Message}").ShowWindowAsync();
        }
    }
    public string movieDir { get; set; }
    private ObservableCollection<Subtitle> _subtitles = new();
    public ObservableCollection<Subtitle> Subtitles
    {
        get => _subtitles;
        set => this.RaiseAndSetIfChanged(ref _subtitles, value);
    }
}