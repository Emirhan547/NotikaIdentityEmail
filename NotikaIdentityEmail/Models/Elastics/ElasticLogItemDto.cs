namespace NotikaIdentityEmail.Models.Elastics
{
    public class ElasticLogItemDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Level { get; set; }
        public string? MessageTemplate { get; set; }
        public string? RenderedMessage { get; set; }

        public string? RequestPath { get; set; }
        public int? StatusCode { get; set; }
        public double? Elapsed { get; set; }

        public string? UserEmail { get; set; }
        public int? MessageId { get; set; }
        public int? CategoryId { get; set; }
        public bool? IsDraft { get; set; }
        public bool? IsRead { get; set; }

        public string? ExceptionMessage { get; set; }

    }
}
