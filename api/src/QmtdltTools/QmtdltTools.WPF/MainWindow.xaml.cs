using System.Windows;
using Volo.Abp.DependencyInjection;
using QmtdltTools.WPF.Views;
using QmtdltTools.WPF.Utils;
using QmtdltTools.Domain.Enums;

namespace QmtdltTools.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowVm vm)
    {
        InitializeComponent();

        DataContext = vm;

        Loaded += MainWindow_Loaded;
    }


    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _ = RestHelper.login("qmtdlt", "12000asd");
    }
}

public class MainWindowVm:BindableBase,ITransientDependency
{
    public DelegateCommand OpenLocalPalyGround { get; set; }
    public MainWindowVm()
    {
        SelectedVideoType = VideoCollectionType.OnLine; // 默认选择在线
        OpenLocalPalyGround = new DelegateCommand(openLocal);
    }

    private void openLocal()
    {
        var wd = App.Get<PlayGround>();
        wd?.Show();
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
            this.RaisePropertyChanged("SelectedVideoType");
        }
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