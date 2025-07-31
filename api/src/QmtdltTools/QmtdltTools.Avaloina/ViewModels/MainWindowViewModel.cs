using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using QmtdltTools.Avaloina.Utils;
using QmtdltTools.Domain.Data;
using ReactiveUI;
using System.Collections.Concurrent;
using System.Reactive;
using System;
using Volo.Abp.DependencyInjection;
using QmtdltTools.Avaloina.Views;
using System.Linq;
using System.IO;

namespace QmtdltTools.Avaloina.ViewModels;

public partial class MainWindowViewModel : ViewModelBase,ISingletonDependency
{
    const string startRecord = "开始录音";
    const string stopRecord = "停止录音";
    private bool isRecording = false;
    string tempAudioFilePath = "";

    public ReactiveCommand<Unit, Unit> AudioRecordCmd { get; set; }
    public ReactiveCommand<Unit, Unit> CheckShadowingCmd { get; set; }
    public MainWindowViewModel()
    {
        CheckShadowingCmd = ReactiveCommand.Create(checkShadowing);

        AudioRecordCmd = ReactiveCommand.Create(onRecordClick);
        RecordBtnContent = startRecord;
    }

    private async void checkShadowing()
    {
        try
        {
            if (File.Exists(tempAudioFilePath))
            {
                StatusText = "正在进行评估...";
                PronunciationResult = null;

                OverAllPlot = null;

                //PronunciationResult = await MsTTSHelperRest.PronunciationAssessmentWithLocalWavFileAsync(tempAudioFilePath, CurSubtitle);

                //if(PronunciationResult != null)
                //{
                //    // var view = App.Get<PronunciationEvaluation>();
                //    // view.SetScores(PronunciationResult);
                //    // OverAllPlot = view;
                //    StatusText = "请查看发音评价。";
                //}
                //else
                //{
                //    // MessageBox.Show("未获取到评价，请重试");
                //}
            }
        }
        catch (Exception ex)
        {
            // MessageBox.Show(ex.Message);
        }
    }
    private void onRecordClick()
    {
        if (!isRecording)
        {
            StartRecording();           // 录音
        }
        else
        {
            //StopRecording();            // 停止录音
        }
    }
    private void StartRecording()
    {
        try
        {
            // waveIn = new WaveInEvent();
            // waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, mono
            // audioStream = new MemoryStream();
            // waveFileWriter = new WaveFileWriter(audioStream, waveIn.WaveFormat);
            //
            // waveIn.DataAvailable += (s, a) =>
            // {
            //     waveFileWriter.Write(a.Buffer, 0, a.BytesRecorded);
            // };
            //
            // waveIn.StartRecording();
            isRecording = true;
            RecordBtnContent = stopRecord;
            IsSubmitEnable = false;
            StatusText = "正在录音...";
        }
        catch (Exception ex)
        {
            // MessageBox.Show($"录音启动失败: {ex.Message}");
        }
    }
    private void StopRecording()
    {
        try
        {
            // waveIn.StopRecording();
            // waveFileWriter.Flush();
            //isRecording = false;
            //RecordBtnContent = startRecord;
            //IsSubmitEnable = true;
            //StatusText = "录音已停止，可以提交或播放";

            //// Save audio to a temporary file
            //tempAudioFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"recording_submit.wav");
            //File.WriteAllBytes(tempAudioFilePath, audioStream.ToArray());

            //// Prepare audio for playback
            //RecordAudioUri = new Uri(tempAudioFilePath, UriKind.Absolute);
        }
        catch (Exception ex)
        {
            // MessageBox.Show($"录音停止失败: {ex.Message}");
        }
    }
    


    public Uri RecordAudioUri { get; set; }
    public bool IsSubmitEnable { get; set; }
    public string StatusText { get; set; }
    public string RecordBtnContent { get; set; }
    public PronunciationAssessmentResult? PronunciationResult { get; set; }
    public object OverAllPlot { get; set; }
}