namespace NotikaIdentityEmail.Entities
{
    public class Comment
    {
        public int CommentId { get; set; }

        // 🔑 Yorum içeriği
        public string CommentDetail { get; set; } = string.Empty;

        public DateTime CommentDate { get; set; }

        // 🔑 Moderasyon durumu
        // Aktif / Pasif / Onay Bekliyor
        public string CommentStatus { get; set; } = "Onay Bekliyor";

        // 🔑 AI Moderation sonuçları (ASLA NULL DEĞİL)
        public bool IsToxic { get; set; } = false;

        // 0.0 – 1.0 arası skor (analiz başarısızsa 0)
        public double ToxicityScore { get; set; } = 0;

        // toxic / non-toxic / unknown
        public string ToxicityLabel { get; set; } = "unknown";

        // 🔑 Kullanıcı ilişkisi
        public string AppUserId { get; set; } = string.Empty;
        public AppUser AppUser { get; set; } = null!;
    }
}
