using QmtdltTools.Domain.Enums;
using QmtdltTools.WPF.Dto;
using QmtdltTools.WPF.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Views
{
    /// <summary>
    /// OnLineItemView.xaml 的交互逻辑
    /// </summary>
    public partial class OnLineItemView : UserControl,ITransientDependency
    {
        public OnLineItemView(OnLineItemViewVm vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
        public void init(string name, string url)
        {
            if (DataContext is OnLineItemViewVm vm)
            {
                vm.init(name, url);
            }
        }
        public void SetOnRemoveAction(Action action)
        {
            if (DataContext is OnLineItemViewVm vm)
            {
                vm.SetOnRemoveAction(action);
            }
        }
    }

    public class OnLineItemViewVm:BindableBase,ITransientDependency
    {
        public DelegateCommand ClickCmd { get; set; }
        public DelegateCommand RemoveThis { get; set; }
        public OnLineItemViewVm()
        {
            ClickCmd = new DelegateCommand(onClick);
            RemoveThis = new DelegateCommand(removeThis);
        }

        private void removeThis()
        {
            var cfg = JsonSerializer.Deserialize<List<OnLineCfg>>(AppSettingHelper.OnLineCfg);

            if (cfg != null && cfg.Any(t => t.url == VideoUrl))
            {
                var findEntity = cfg.First(t => t.url == VideoUrl);
                cfg.Remove(findEntity);

                AppSettingHelper.OnLineCfg = JsonSerializer.Serialize(cfg);
                _onRemove?.Invoke();
            }
        }
        public void SetOnRemoveAction(Action action)
        {
            _onRemove = action;
        }
        Action _onRemove;
        private void onClick()
        {
            var wd = App.Get<PlayGround>();
            wd.SetType(VideoCollectionType.OnLine);
            wd.LoadUrl(VideoUrl);
            wd?.Show();
        }

        private string videoUrl;
        public string VideoUrl
        {
            get { return videoUrl; }
            set
            {
                videoUrl = value;
                this.RaisePropertyChanged("VideoUrl");
            }
        }
        private string videoName;
        public string VideoName
        {
            get { return videoName; }
            set
            {
                videoName = value;
                this.RaisePropertyChanged("VideoName");
            }
        }
        internal void init(string name, string url)
        {
            VideoName = name;
            videoUrl = url;
        }
    }
}
