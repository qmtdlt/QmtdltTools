using QmtdltTools.Domain.Data;

namespace QmtdltTools.Avaloina.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = $@"ApplicationConst initialized with SPEECH_KEY: {ApplicationConst.SPEECH_KEY}, SPEECH_REGION: {ApplicationConst.SPEECH_REGION}, GROK_KEY: {ApplicationConst.GROK_KEY}, 
GEMINI_KEY: {ApplicationConst.GEMINI_KEY}, DOU_BAO: {ApplicationConst.DOU_BAO}, QIAN_WEN: {ApplicationConst.QIAN_WEN}, AIType: {ApplicationConst.AIType}";
}