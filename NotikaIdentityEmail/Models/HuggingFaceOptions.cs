namespace NotikaIdentityEmail.Models
{
    public class HuggingFaceOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ToxicityModel { get; set; } = "unitary/toxic-bert";
        public string TranslateEnToTrModel { get; set; } = "Helsinki-NLP/opus-mt-en-tr";
        public string TranslateTrToEnModel { get; set; } = "Helsinki-NLP/opus-mt-tr-en";
        public double ToxicityThreshold { get; set; } = 0.5;

    }
}
