namespace QmtdltTools.Domain.Dtos;

public class SentenceEvaluateDto
{
    public bool IfUsageCorrect { get; set; }
    public string IncorrectReason { get; set; }
    public string CorrectSentence { get; set; }
}