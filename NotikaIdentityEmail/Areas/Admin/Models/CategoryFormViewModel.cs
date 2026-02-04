using System.ComponentModel.DataAnnotations;

namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class CategoryFormViewModel
    {
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Icon URL en fazla 250 karakter olabilir.")]
        public string? CategoryIconUrl { get; set; }

        public bool CategoryStatus { get; set; }
    }
}
