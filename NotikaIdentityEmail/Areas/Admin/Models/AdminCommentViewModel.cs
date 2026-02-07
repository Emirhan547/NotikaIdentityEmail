namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class AdminCommentViewModel
    {
        public int CommentId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CommentDetail { get; set; } = string.Empty;
        public string? Translation { get; set; }
        public string ToxicityLabel { get; set; } = "Analiz Yok";
        public double? ToxicityScore { get; set; }
        public bool IsToxic { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CommentDate { get; set; }
    }
}
