using QmtdltTools.Domain.Enums;
using QmtdltTools.WPF.IServices;
using QmtdltTools.WPF.Services;
using System;
using System.Collections.Concurrent;
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
using System.Windows.Shapes;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Views
{
    /// <summary>
    /// PlayGround.xaml 的交互逻辑
    /// </summary>
    public partial class PlayGround : Window,ITransientDependency
    {
        private readonly TransService _transService;
        public PlayGround(PlayGroundVm vm, TransService transService)
        {
            InitializeComponent();
            _transService = transService;
            DataContext = vm;
            this.Closing += MainWindow_Closing;
        }
        public void SetType(VideoCollectionType videoCollectionType)
        {
            if (DataContext is PlayGroundVm vm)
            {
                vm.SetType(videoCollectionType);
            }
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
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is PlayGroundVm vm)
            {
                vm.onClose();
            }
        }

        internal void LoadUrl(string videoUrl)
        {
            if (DataContext is PlayGroundVm vm)
            {
                vm.LoadUrl(videoUrl);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Space)
            {
                if (DataContext is PlayGroundVm vm)
                {
                    vm.PauseVideo();
                }
            }
        }
    }

    public class PlayGroundVm : BindableBase, ITransientDependency
    {
        ISubtitleService _subtitleService;
        public PlayGroundVm(ISubtitleService subtitleService)
        {
            _subtitleService = subtitleService;            
        }
        public void onClose()
        {
            if (VideoView is WebVideoView view1)
            {
                view1.onClose();
            }
            if (VideoView is LocalVideoView view2)
            {
                view2.onClose();
            }
        }

        ConcurrentQueue<string> subtitleQueue = new ConcurrentQueue<string>();
        void updatingTitle(string subTitle)
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
            if (subtitleQueue.Count > 4)
            {
                subtitleQueue.TryDequeue(out string? data);
            }
            var list = subtitleQueue.ToList();
            if (list.Count > 1)
            {
                if(_videoCollectionType == VideoCollectionType.OffLine)
                {
                    PastSubtitle = string.Join("\n", list.Take(list.Count - 1));
                }
                else
                {
                    PastSubtitle = string.Join("\n", list);
                }
            }
        }

        public void LoadUrl(string url)
        {
            if (VideoView is WebVideoView view)
            {
                view.LoadUrl(url);
            }
        }
        VideoCollectionType _videoCollectionType;
        internal void SetType(VideoCollectionType videoCollectionType)
        {
            _videoCollectionType = videoCollectionType;
            if (videoCollectionType == VideoCollectionType.OnLine)
            {
                _ = Task.Run(async () =>
                {
                    await _subtitleService.StopAsync();
                    await _subtitleService.StartRecognizeAsync(updatingTitle, SetSubTitle);
                });
                VideoView = App.Get<WebVideoView>();
            }
            else
            {
                var view = App.Get<LocalVideoView>();
                view.InitAction(updatingTitle,SetSubTitle);
                VideoView = view;
            }
        }

        internal void PauseVideo()
        {
            if (_videoCollectionType == VideoCollectionType.OnLine)
            {
                if (VideoView is WebVideoView view1)
                {
                    //view1.pause();
                }
            }
            else
            {
                if (VideoView is LocalVideoView view2)
                {
                    view2.PauseBtn_Click(null,null);
                }
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
}
