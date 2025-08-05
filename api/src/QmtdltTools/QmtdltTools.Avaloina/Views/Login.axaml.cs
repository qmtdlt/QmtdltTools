using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using QmtdltTools.Avaloina.Utils;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class Login : Window,ITransientDependency
{
    public Login()
    {
        InitializeComponent();
        UsernameTextBox.Text = AppSettingHelper.LoginUserName;
        PasswordBox.Text = AppSettingHelper.LoguserPwd;
    }

    private async void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = await RestHelper.login(UsernameTextBox.Text,PasswordBox.Text);                     // 登录默认用户，后续需要修改为使用自己的账户登录
        if (result)
        {
            // 登录成功，关闭登录窗口
            App.Get<MainWindow>()?.Show();
            AppSettingHelper.LoginUserName = UsernameTextBox.Text;
            AppSettingHelper.LoguserPwd = PasswordBox.Text;
            this.Close();
        }
        else
        {
            // 登录失败，显示错误信息
            MessageBoxManager.GetMessageBoxStandard("登录失败", "用户名或密码错误，请重试。", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync();
        }
    }
}