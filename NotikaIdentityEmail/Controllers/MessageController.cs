using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;

namespace NotikaIdentityEmail.Controllers
{
    public class MessageController : Controller
    {
        private readonly EmailContext _context;

        public MessageController(EmailContext context)
        {
            _context = context;
        }

        public IActionResult Inbox()
        {
            var values=_context.Messages.Where(x=>x.ReceiverMail=="ali@gmail.com").ToList();
            return View(values);
        }
        public IActionResult SendBox()
        {
            var values = _context.Messages.Where(x => x.SenderEmail == "ali@gmail.com").ToList();
            return View(values);
        }
       public IActionResult MessageDetail()
        {
            var value = _context.Messages.Where(x => x.MessageId == 1).FirstOrDefault();
            return View(value);
        }
        [HttpGet]
        public IActionResult ComposeMessage()
        {
            return View();
        }
    }
}
