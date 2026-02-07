namespace NotikaIdentityEmail.Services.HuggingFaces
{
    public interface IHuggingFaceService
    {
        Task<ToxicityAnalysisResult?> AnalyzeToxicityAsync(string text, CancellationToken cancellationToken = default);
        Task<string?> TranslateAsync(string text, CancellationToken cancellationToken = default);
    }
}
