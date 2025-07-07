using QmtdltTools.Domain.Entitys;
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
using System.Windows.Shapes;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Views
{
    /// <summary>
    /// TranslateResultWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TranslateResultWindow : Window,ITransientDependency
    {
        public TranslateResultWindow(TranslateResultWindowVm vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        public void setData(VocabularyRecord data)
        {
            if(DataContext is TranslateResultWindowVm vm)
            {
                vm.setData(data);
            }
        }
    }

    public class TranslateResultWindowVm : BindableBase, ITransientDependency
    {
        public DelegateCommand playWordCmd { get; set; }
        public DelegateCommand playExplainCmd { get; set; }
        public DelegateCommand stopCmd { get; set; }

        public TranslateResultWindowVm()
        {
            playWordCmd = new DelegateCommand(playWord);
            playExplainCmd = new DelegateCommand(playExplain);
            stopCmd = new DelegateCommand(stopPlay);
        }

        private void stopPlay()
        {
            
        }

        private void playExplain()
        {
            
        }

        private void playWord()
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
