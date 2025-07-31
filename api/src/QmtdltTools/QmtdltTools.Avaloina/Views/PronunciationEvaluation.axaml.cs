using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using QmtdltTools.Avaloina.Dto;
using ReactiveUI;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class PronunciationEvaluation : UserControl,ITransientDependency
{
    public PronunciationEvaluation(PronunciationEvaluationVm vm)
    {
        InitializeComponent();
        DataContext = vm;
        plot.Plot.Font.Set(SKFontManager.Default.MatchCharacter('汉').FamilyName);
    }


    public void SetScores(PronunciationAssessmentResultDto input)
    {
        string[] spokeLabels = { "发音准确度", "语音的流畅度", "完整性", "韵律" };
        double[] values = { input.AccuracyScore, input.FluencyScore, input.CompletenessScore, input.ProsodyScore };
        var radar = plot.Plot.Add.Radar(values);
        radar.PolarAxis.SetSpokes(spokeLabels, length: 110);
        plot.Refresh();
        overAllScore.Text = input.PronunciationScore.ToString("0.00") + "分";
        ShowWordsResult(input.Words);
    }

    void ShowWordsResult(IEnumerable<WordResultDto> words)
    {
        if (DataContext is PronunciationEvaluationVm vm)
        {
            vm.setWords(words);
        }
    }
}

public class PronunciationEvaluationVm : ReactiveObject, ITransientDependency
{
    public PronunciationEvaluationVm()
    {

    }

    internal void setWords(IEnumerable<WordResultDto> words)
    {
        WordsData = new ObservableCollection<WordResultDto>(words);
    }

    private ObservableCollection<WordResultDto> _wordsData;
    public ObservableCollection<WordResultDto> WordsData
    {
        get { return _wordsData; }
        set
        {
            this.RaiseAndSetIfChanged(ref _wordsData,value);
        }
    }
}