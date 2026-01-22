
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

using System.Threading.Tasks;


namespace NotikaIdentityEmail.Controllers
{
    public class PasswordChangeController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public PasswordChangeController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult ForgetPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            string passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResetTokenLink = Url.Action("ResetPassword", "PasswordChange", new
            {
                userId = user.Id,
                token = passwordResetToken
            }, HttpContext.Request.Scheme);
            MimeMessage mimeMessage = new MimeMessage();
            MailboxAddress mailboxAddress = new MailboxAddress("Notika Admin", "emirhanhacioglu099@gmail.com");
            mimeMessage.From.Add(mailboxAddress);
            MailboxAddress mailboxAddressTo = new MailboxAddress("User", model.Email);
            mimeMessage.To.Add(mailboxAddressTo);
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody = passwordResetTokenLink;
            mimeMessage.Body = bodyBuilder.ToMessageBody();
            mimeMessage.Subject = "Şifre Değişiklik Talebi";
            SmtpClient client = new SmtpClient();
            client.Connect("smtp.gmail.com", 587, false);
            client.Authenticate("emirhanhacioglu909@gmail.com", "zedhniyorxuqovzp");
            client.SendAsync(mimeMessage);
            client.Disconnect(true);
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            TempData["userId"] = userId;
            TempData["token"] = token;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
        {
            var userid = TempData["userId"];
            var token = TempData["token"];
            if (userid == null || token == null)
            {
                ViewBag.v = "Hata Oluştu";
            }
            var user = await _userManager.FindByIdAsync(userid.ToString());
            var result = await _userManager.ResetPasswordAsync(user, token.ToString(), resetPasswordViewModel.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("UserLogin", "Login");
            }
            return View();
        }
    } 
}  