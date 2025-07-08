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
using System.Collections.ObjectModel;
using QmtdltTools.Domain.Enums;
using NAudio.CoreAudioApi.Interfaces;
using QmtdltTools.Service.Utils;
using System.Text.Json;
using Serilog;
using CefSharp;
using CefSharp.Wpf;

namespace QmtdltTools.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    SubtitleService _subtitleService;

    public MainWindow(MainWindowVm vm, SubtitleService subtitleService)
    {
        InitializeComponent();

        DataContext = vm;

        Loaded += MainWindow_Loaded;

        Closing += MainWindow_Closing;
        this._subtitleService = subtitleService;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _ = _subtitleService.StopAsync();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _ = RestHelper.login("qmtdlt", "12000asd");
    }
}

public class MainWindowVm:BindableBase,ITransientDependency
{
    public DelegateCommand AddNew { get; set; }
    public DelegateCommand OpenLocalPalyGround { get; set; }
    public MainWindowVm()
    {
        VideoTypeSelection = new ObservableCollection<ComboxSelectItem<VideoCollectionType>>();
        VideoTypeSelection.AddRange(EnumHelper.GetComboxList<VideoCollectionType>());

        SelectedVideoType = VideoCollectionType.OnLine; // 默认选择在线
        AddNew = new DelegateCommand(addNew);
        OpenLocalPalyGround = new DelegateCommand(openLocal);
    }

    private void openLocal()
    {
        var wd = App.Get<PlayGround>();
        wd.SetType(VideoCollectionType.OffLine);
        wd?.Show();
    }

    private void addNew()
    {
        string json = AppSettingHelper.OnLineCfg;
        if (string.IsNullOrEmpty(json))
        {
            AppSettingHelper.OnLineCfg = JsonSerializer.Serialize(new List<OnLineCfg>
            {
                new OnLineCfg
                {
                    url = InputUrl,
                    name = InputUrl
                }
            });
        }
        else
        {
            var cfg = JsonSerializer.Deserialize<List<OnLineCfg>>(json);

            if (!cfg.Any(t => t.url == InputUrl))
            {
                cfg.Add(new OnLineCfg
                {
                    url = InputUrl,
                    name = InputUrl
                });
                AppSettingHelper.OnLineCfg = JsonSerializer.Serialize(cfg);
            }
        }
            
    }

    void query()
    {
        try
        {
            if (selectedVideoType == VideoCollectionType.OnLine)
            {
                if (!string.IsNullOrEmpty(AppSettingHelper.OnLineCfg))
                {
                    var list = JsonSerializer.Deserialize<List<OnLineCfg>>(AppSettingHelper.OnLineCfg);
                    OnLineViews = new ObservableCollection<OnLineItemView>();

                    foreach (var item in list)
                    {
                        var view  = App.Get<OnLineItemView>();
                        view.init(item.name, item.url);
                        view.SetOnRemoveAction(() =>
                        {
                            OnLineViews.Remove(view);
                        });
                        OnLineViews.Add(view);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            MessageBox.Show(ex.Message);
        }
    }

    private ObservableCollection<OnLineItemView> onLineViews;
    public ObservableCollection<OnLineItemView> OnLineViews
    {
        get { return onLineViews; }
        set
        {
            onLineViews = value;
            this.RaisePropertyChanged("OnLineViews");
        }
    }
    private ObservableCollection<ComboxSelectItem<VideoCollectionType>> videoTypeSelection;
    public ObservableCollection<ComboxSelectItem<VideoCollectionType>> VideoTypeSelection
    {
        get { return videoTypeSelection; }
        set
        {
            videoTypeSelection = value;
            this.RaisePropertyChanged("VideoTypeSelection");
        }
    }
    private string inputUrl;
    public string InputUrl
    {
        get { return inputUrl; }
        set
        {
            inputUrl = value;
            this.RaisePropertyChanged("InputUrl");
        }
    }
    private VideoCollectionType selectedVideoType;
    public VideoCollectionType SelectedVideoType
    {
        get { return selectedVideoType; }
        set
        {
            selectedVideoType = value;
            onSelVideoTypeChange();
            this.RaisePropertyChanged("SelectedVideoType");
        }
    }

    private void onSelVideoTypeChange()
    {
        if(selectedVideoType == VideoCollectionType.OnLine)
        {
            // 显示在线
            UrlInputIsShow = Visibility.Visible;
            OffLineIsShow = Visibility.Hidden;
        }
        else
        {
            // 显示离线
            UrlInputIsShow = Visibility.Hidden;
            OffLineIsShow = Visibility.Visible;
        }
        query();
    }

    private Visibility urlInputIsShow;
    public Visibility UrlInputIsShow
    {
        get { return urlInputIsShow; }
        set
        {
            urlInputIsShow = value;
            this.RaisePropertyChanged("UrlInputIsShow");
        }
    }

    private Visibility offLineIsShow;
    public Visibility OffLineIsShow
    {
        get { return offLineIsShow; }
        set
        {
            offLineIsShow = value;
            this.RaisePropertyChanged("OffLineIsShow");
        }
    }
}