using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.WPF.Views;
using QmtdltTools.WPF.Utils;
using QmtdltTools.WPF.IServices;
using QmtdltTools.WPF.Dto;
using System.Collections.Concurrent;
using System.Linq;
using QmtdltTools.WPF.Services;

namespace QmtdltTools.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TransService _transService;
    
    public MainWindow(MainWindowVm vm, TransService transService)
    {
        InitializeComponent();
        _transService = transService;
        DataContext = vm;
        this.Closing += MainWindow_Closing;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _ = RestHelper.login("qmtdlt", "12000asd");
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        
    }

    private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 获取选中的文本
        if (sender is TextBox textBox)
        {
            string selectedText = textBox.SelectedText;
            _ = _transService.Trans(selectedText);
        }
    }
}

public class MainWindowVm : BindableBase, ISingletonDependency
{
    ISubtitleService _subtitleService;
    public MainWindowVm(ISubtitleService subtitleService)
    {
        _subtitleService = subtitleService;
        _ = Task.Run(async () =>
        {
            await _subtitleService.StartAsync(updatingTitle,SetSubTitle);
        });

        VideoView = App.Get<WebVideoView>();
    }

    public void onClose()
    {
        _subtitleService.StopAsync();
    }
    
    ConcurrentQueue<string> subtitleQueue = new ConcurrentQueue<string>();
    void updatingTitle(string  subTitle)
    {
        CurSubtitle = subTitle;
    }
    void SetSubTitle(string subTitle)
    {
        subtitleQueue.Enqueue(subTitle);
        updatePastSubtitles();
    }
    void updatePastSubtitles()
    {
        if(subtitleQueue.Count > 4)
        {
            subtitleQueue.TryDequeue(out string? data);
        }
        var list = subtitleQueue.ToList();
        if(list.Count > 1)
        {
            PastSubtitle = string.Join("\n", list);
        }
    }
    private string curSubtitle;
    public string CurSubtitle
    {
        get { return curSubtitle; }
        set
        {
            curSubtitle = value;
            this.RaisePropertyChanged("CurSubtitle");
        }
    }
    private string pastSubtitle;
    public string PastSubtitle
    {
        get { return pastSubtitle; }
        set
        {
            pastSubtitle = value;
            this.RaisePropertyChanged("PastSubtitle");
        }
    }
    private IVideoView videoView;
    public IVideoView VideoView
    {
        get { return videoView; }
        set
        {
            videoView = value;
            this.RaisePropertyChanged("VideoView");
        }
    }
}