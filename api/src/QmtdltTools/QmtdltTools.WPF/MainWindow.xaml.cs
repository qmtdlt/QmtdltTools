using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.WPF.Views;
using QmtdltTools.WPF.Utils;
using QmtdltTools.WPF.IServices;

namespace QmtdltTools.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TranslationService _translationService;
    public MainWindow(MainWindowVm vm, TranslationService translationService)
    {
        InitializeComponent();
        _translationService = translationService;
        DataContext = vm;
        this.Closing += MainWindow_Closing;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        
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
            // 示例：显示选中的文本
            //System.Windows.MessageBox.Show("选中的文本: " + selectedText);
            // 您可以在这里添加其他逻辑，例如：
            Task.Run(async () =>
            {
                VocabularyRecord? findRes = await _translationService.Trans(0, 0, "", selectedText, Guid.Parse("08dd7e88-9af1-4775-8a21-554610976784"));

                if (findRes != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var wd = App.Get<TranslateResultWindow>();
                        wd.setData(findRes);
                        if (DataContext is MainWindowVm vm1)
                        {
                            vm1.pauseWork();
                        }
                        wd.ShowDialog();

                        if (DataContext is MainWindowVm vm2)
                        {
                            vm2.continueWork();
                        }
                    });
                }
            });
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
            await _subtitleService.StartAsync(SetSubTitle);
        });

        VideoView = App.Get<WebVideoView>();
    }

    public void onClose()
    {
        _subtitleService.StopAsync();
    }
    public void pauseWork()
    {
        _subtitleService.Pause();
    }

    public void continueWork()
    {
        _subtitleService.Resume();
    }
    void SetSubTitle(string  subTitle)
    {
        CurSubtitle = subTitle;
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