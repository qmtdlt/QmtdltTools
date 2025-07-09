using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Views
{
    /// <summary>
    /// PronunciationEvaluation.xaml 的交互逻辑
    /// </summary>
    public partial class PronunciationEvaluation : UserControl,ITransientDependency
    {
        public PronunciationEvaluation(PronunciationEvaluationVm vm)
        {
            InitializeComponent();
            DataContext = vm;
            plot.Plot.Font.Set(SKFontManager.Default.MatchCharacter('汉').FamilyName);
        }

        public void SetScores(PronunciationAssessmentResult input)
        {
            string[] spokeLabels = { "发音准确度", "语音的流畅度", "完整性", "韵律" };
            double[] values = { input.AccuracyScore, input.FluencyScore, input.CompletenessScore, input.ProsodyScore };
            var radar = plot.Plot.Add.Radar(values);
            radar.PolarAxis.SetSpokes(spokeLabels, length: 110);
            plot.Refresh();
            overAllScore.Text = input.PronunciationScore.ToString("0.00") + "分";
            ShowWordsResult(input.Words);
        }

        void ShowWordsResult(IEnumerable<PronunciationAssessmentWordResult> words)
        {
            if(DataContext is PronunciationEvaluationVm vm)
            {
                vm.setWords(words);
            }
        }
    }

    public class PronunciationEvaluationVm:BindableBase,ITransientDependency
    {
        public PronunciationEvaluationVm()
        {
            
        }

        internal void setWords(IEnumerable<PronunciationAssessmentWordResult> words)
        {
            WordsData = new ObservableCollection<PronunciationAssessmentWordResult>();
            WordsData.AddRange(words);
        }

        private ObservableCollection<PronunciationAssessmentWordResult> wordsData;
        public ObservableCollection<PronunciationAssessmentWordResult> WordsData
        {
            get { return wordsData; }
            set
            {
                wordsData = value;
                this.RaisePropertyChanged("WordsData");
            }
        }
    }
}
