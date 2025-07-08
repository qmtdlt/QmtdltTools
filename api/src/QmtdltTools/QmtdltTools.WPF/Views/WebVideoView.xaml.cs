using CefSharp;
using CefSharp.Wpf;
using QmtdltTools.WPF.IServices;
using QmtdltTools.WPF.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QmtdltTools.WPF.Views
{
    /// <summary>
    /// WebVideoView.xaml 的交互逻辑
    /// </summary>
    public partial class WebVideoView : UserControl,IVideoView
    {
        public WebVideoView()
        {
            InitializeComponent();
            Browser.LifeSpanHandler = new CustomLifeSpanHandler();
            Loaded += WebVideoView_Loaded;
        }

        private void WebVideoView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AppSettingHelper.lastUrl))
            {
                var res = MessageBox.Show($"是否上次打开的地址：{AppSettingHelper.lastUrl}", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (res == MessageBoxResult.OK)
                {
                    Browser.LoadUrl(AppSettingHelper.lastUrl);
                }
            }
        }

        private void GoToUrl(object sender, RoutedEventArgs e)
        {
            LoadUrl(targetUrl.Text);
        }
        public void LoadUrl(string url)
        {
            Browser.LoadUrl(url);
        }

        internal void onClose()
        {
            AppSettingHelper.lastUrl = Browser.Address;
        }
    }
}
