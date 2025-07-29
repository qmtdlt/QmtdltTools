using Avalonia.Controls;
using Avalonia.Interactivity;
using QmtdltTools.Avaloina.Utils;

namespace QmtdltTools.Avaloina.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _ = RestHelper.login("qmtdlt", "12000asd");
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        App.Get<PlayGround>()?.Show();
    }

    private void Button_OnClick2(object? sender, RoutedEventArgs e)
    {
        AppSettingHelper.ApiServer = "老铁666";
    }
}