using QmtdltTools.Domain.Entitys;
using QmtdltTools.Service.Services;
using QmtdltTools.WPF.Dto;
using QmtdltTools.WPF.Services;
using QmtdltTools.WPF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// TranslateResultWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TranslateResultWindow : Window,ITransientDependency
    {
        private readonly TransService _transService;
        public TranslateResultWindow(TranslateResultWindowVm vm, TransService transService)
        {
            InitializeComponent();
            _transService = transService;
            DataContext = vm;
        }
        VocabularyRecord _data;
        public void setData(VocabularyRecord data)
        {
            _data = data;
            if (DataContext is TranslateResultWindowVm vm)
            {
                vm.setData(data);
            }
        }

        public void PlayAudio(byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0) return;

            // 写入临时文件
            var tempFile = System.IO.Path.GetTempFileName() + ".wav";
            File.WriteAllBytes(tempFile, audioData);

            mediaElement.Source = new Uri(tempFile, UriKind.Absolute);
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        }

        public void StopAudio()
        {
            mediaElement.Stop();
            mediaElement.Source = null;
        }

        private void playWord(object sender, RoutedEventArgs e)
        {
            if(_data.WordPronunciation  != null)
            {
                PlayAudio(_data.WordPronunciation);
            }            
        }

        private void playExplain(object sender, RoutedEventArgs e)
        {
            if (_data.Pronunciation != null)
            {
                PlayAudio(_data.Pronunciation);
            }
        }

        private void stopAudio(object sender, RoutedEventArgs e)
        {
            StopAudio();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopAudio();
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string selectedText = textBox.SelectedText;
                _ = _transService.Trans(selectedText);
            }
        }
    }

    public class TranslateResultWindowVm : BindableBase, ITransientDependency
    {
        public TranslateResultWindowVm()
        {
        }

        private VocabularyRecord resData;
        public VocabularyRecord ResData
        {
            get { return resData; }
            set
            {
                resData = value;
                this.RaisePropertyChanged("ResData");
            }
        }

        internal void setData(VocabularyRecord data)
        {
            ResData = data; 
        }
    }
}
