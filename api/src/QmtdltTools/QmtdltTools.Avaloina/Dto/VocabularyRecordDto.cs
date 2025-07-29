namespace QmtdltTools.Avaloina.Dto;

public class VocabularyRecordDto
{
    public string id { get; set; }
    public string wordText { get; set; }
    public string aiExplanation { get; set; }
    public string aiTranslation { get; set; }
    public string sentenceYouMade { get; set; }
    public bool ifUsageCorrect { get; set; }
    public string incorrectReason { get; set; }
    public string pronunciation { get; set; }  // 语音buffer
    public string translation { get; set; }  // 翻译结果
    public string explanation { get; set; }  // 解释
    public string wordPronunciation { get; set; }  // 单词语音buffer
    public string createTime { get; set; }
}