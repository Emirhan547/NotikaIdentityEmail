using System.ComponentModel.DataAnnotations;

namespace NotikaIdentityEmail.Models
{
    public class ActivationViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public int Code { get; set; }
    }
}
