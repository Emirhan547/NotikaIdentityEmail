namespace NotikaIdentityEmail.Services.HuggingFaces
{
    public class ToxicityAnalysisResult
    {
        public string Label { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsToxic { get; set; }
    }
}
