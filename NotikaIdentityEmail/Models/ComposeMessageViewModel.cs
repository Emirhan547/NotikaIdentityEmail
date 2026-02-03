using System.ComponentModel.DataAnnotations;

namespace NotikaIdentityEmail.Models
{
    public class ComposeMessageViewModel
    {
        [Required(ErrorMessage = "Alıcı email adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string ReceiverEmail { get; set; }

        [Required(ErrorMessage = "Konu alanı zorunludur.")]
        [StringLength(150, ErrorMessage = "Konu en fazla 150 karakter olabilir.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Mesaj içeriği zorunludur.")]
        [StringLength(5000, ErrorMessage = "Mesaj en fazla 5000 karakter olabilir.")]
        public string MessageDetail { get; set; }

        [Required(ErrorMessage = "Kategori seçiniz.")]
        public int? CategoryId { get; set; }
    }
}