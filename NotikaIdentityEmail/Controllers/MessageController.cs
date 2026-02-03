using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Services;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize(Roles = "User")]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHtmlSanitizerService _htmlSanitizer;
        public MessageController(EmailContext context, UserManager<AppUser> userManager, IHtmlSanitizerService htmlSanitizer)
        {
            _context = context;
            _userManager = userManager;
            _htmlSanitizer = htmlSanitizer;
        }

       
        public async Task<IActionResult> Inbox(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

          
            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.SenderEmail equals u.Email into userGroup
                         from sender in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                       
                         where m.ReceiverEmail == user.Email && !m.IsDeleted && !m.IsDraft
                         select new MessageWithSenderInfoViewModel
                         {
                             MessageId = m.MessageId,
                             MessageDetail = m.MessageDetail,
                             Subject = m.Subject,
                             SendDate = m.SendDate,
                             SenderEmail = m.SenderEmail,
                             SenderName = sender != null ? sender.Name : "Bilinmeyen",
                             SenderSurname = sender != null ? sender.Surname : "Kullanıcı",
                            
            CategoryName = category != null ? category.CategoryName : "Kategori Yok",
                              IsRead = m.IsRead
                          };

            if (!string.IsNullOrWhiteSpace(query))
            {
                values = values.Where(x =>
                    x.Subject.Contains(query) ||
                    x.MessageDetail.Contains(query) ||
                    x.SenderEmail.Contains(query) ||
                    x.SenderName.Contains(query) ||
                    x.SenderSurname.Contains(query));
            }

            return View(values.ToList());

        
}


        public async Task<IActionResult> Sendbox(string? query)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);

  
            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.ReceiverEmail equals u.Email into userGroup
                         from receiver in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                        
                         where m.SenderEmail == user.Email && !m.IsDeleted && !m.IsDraft
                         select new MessageWithReceiverInfoViewModel
                         {
                             MessageId = m.MessageId,
                             MessageDetail = m.MessageDetail,
                             Subject = m.Subject,
                             SendDate = m.SendDate,
                             ReceiverEmail = m.ReceiverEmail,
                             ReceiverName = receiver != null ? receiver.Name : "Bilinmeyen",
                             ReceiverSurname = receiver != null ? receiver.Surname : "Kullanıcı",
                             CategoryName = category != null ? category.CategoryName : "Kategori Yok"
                         };




if (!string.IsNullOrWhiteSpace(query))
{
    values = values.Where(x =>
        x.Subject.Contains(query) ||
        x.MessageDetail.Contains(query) ||
        x.ReceiverEmail.Contains(query) ||
        x.ReceiverName.Contains(query) ||
        x.ReceiverSurname.Contains(query));
}

return View(values.ToList());
        }

   
        public async Task<IActionResult> MessageDetail(int id)
{
   
    var user = await _userManager.FindByNameAsync(User.Identity.Name);
    var value = await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && !x.IsDeleted);
    if (value == null)
    {
        return NotFound();
    }

    if (!value.IsRead && value.ReceiverEmail == user.Email)
    {
        value.IsRead = true;
        await _context.SaveChangesAsync();
    }

    return View(value);
}
[HttpGet]
public IActionResult ComposeMessage()
{
   
    LoadCategories();
    return View(new ComposeMessageViewModel());
}
[HttpPost]

        [ValidateAntiForgeryToken]
public async Task<IActionResult> ComposeMessage(ComposeMessageViewModel model, string action)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);
  

    if (!string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase))
    {
        var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail);
        if (receiver == null)
        {
            ModelState.AddModelError(nameof(model.ReceiverEmail), "Alıcı email adresi sistemde bulunamadı.");
        }
    }

    if (!ModelState.IsValid)
    {
        LoadCategories();
        return View(model);
    }

    var message = new Message
    {
        SenderEmail = user.Email,
        ReceiverEmail = model.ReceiverEmail,
        Subject = model.Subject,
        MessageDetail = _htmlSanitizer.Sanitize(model.MessageDetail),
        SendDate = DateTime.Now,
        IsRead = false,
        IsDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase),
        IsDeleted = false,
        CategoryId = model.CategoryId ?? 0
    };

    _context.Messages.Add(message);

    await _context.SaveChangesAsync();
    return RedirectToAction(message.IsDraft ? "Draft" : "Sendbox");

}

        public async Task<IActionResult> GetMessageListByCategory(int id)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);

    var values = (from m in _context.Messages
                  join u in _context.Users
                  on m.SenderEmail equals u.Email into userGroup
                  from sender in userGroup.DefaultIfEmpty()

                  join c in _context.Categories
                  on m.CategoryId equals c.CategoryId into categoryGroup
                  from category in categoryGroup.DefaultIfEmpty()

               
                  where m.ReceiverEmail == user.Email && m.CategoryId == id && !m.IsDeleted && !m.IsDraft
                  select new MessageWithSenderInfoViewModel
                  {
                      MessageId = m.MessageId,
                      MessageDetail = m.MessageDetail,
                      Subject = m.Subject,
                      SendDate = m.SendDate,
                      SenderEmail = m.SenderEmail,
                      SenderName = sender != null ? sender.Name : "Bilinmeyen",
                      SenderSurname = sender != null ? sender.Surname : "Kullanıcı",
                      IsRead = m.IsRead
                  }).ToList();

    return View(values);
}

public async Task<IActionResult> Draft(string? query)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);

    var values = from m in _context.Messages
                 join u in _context.Users
                 on m.ReceiverEmail equals u.Email into userGroup
                 from receiver in userGroup.DefaultIfEmpty()
                 join c in _context.Categories
                 on m.CategoryId equals c.CategoryId into categoryGroup
                 from category in categoryGroup.DefaultIfEmpty()
                 where m.SenderEmail == user.Email && m.IsDraft && !m.IsDeleted
                 select new MessageWithReceiverInfoViewModel
                 {
                     MessageId = m.MessageId,
                     MessageDetail = m.MessageDetail,
                     Subject = m.Subject,
                     SendDate = m.SendDate,
                     ReceiverEmail = m.ReceiverEmail,
                     ReceiverName = receiver != null ? receiver.Name : "Bilinmeyen",
                     ReceiverSurname = receiver != null ? receiver.Surname : "Kullanıcı",
                     CategoryName = category != null ? category.CategoryName : "Kategori Yok"
                 };

    if (!string.IsNullOrWhiteSpace(query))
    {
        values = values.Where(x =>
            x.Subject.Contains(query) ||
            x.MessageDetail.Contains(query) ||
            x.ReceiverEmail.Contains(query) ||
            x.ReceiverName.Contains(query) ||
            x.ReceiverSurname.Contains(query));
    }

    return View(values.ToList());
}

public async Task<IActionResult> Trash(string? query)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);

    var values = from m in _context.Messages
                 join c in _context.Categories
                 on m.CategoryId equals c.CategoryId into categoryGroup
                 from category in categoryGroup.DefaultIfEmpty()
                 where m.IsDeleted && (m.SenderEmail == user.Email || m.ReceiverEmail == user.Email)
                 select new MessageTrashViewModel
                 {
                     MessageId = m.MessageId,
                     Subject = m.Subject,
                     SenderEmail = m.SenderEmail,
                     ReceiverEmail = m.ReceiverEmail,
                     CategoryName = category != null ? category.CategoryName : "Kategori Yok",
                     SendDate = m.SendDate,
                     IsRead = m.IsRead
                 };

    if (!string.IsNullOrWhiteSpace(query))
    {
        values = values.Where(x =>
            x.Subject.Contains(query) ||
            x.SenderEmail.Contains(query) ||
            x.ReceiverEmail.Contains(query) ||
            x.CategoryName.Contains(query));
    }

    return View(values.ToList());
}

public async Task<IActionResult> Reply(int id)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);
    var message = await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && x.ReceiverEmail == user.Email && !x.IsDeleted);
    if (message == null)
    {
        return NotFound();
    }

    LoadCategories();
    var model = new ComposeMessageViewModel
    {
        ReceiverEmail = message.SenderEmail,
        Subject = $"Re: {message.Subject}"
    };
    return View("ComposeMessage", model);
}

public async Task<IActionResult> Forward(int id)
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);
    var message = await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email) && !x.IsDeleted);
    if (message == null)
    {
        return NotFound();
    }

    LoadCategories();
    var model = new ComposeMessageViewModel
    {
        Subject = $"Fwd: {message.Subject}",
        MessageDetail = message.MessageDetail
    };
    return View("ComposeMessage", model);
}

public async Task<IActionResult> MoveToTrash(int id, string returnAction = "Inbox")
{
    var user = await _userManager.FindByNameAsync(User.Identity.Name);
    var message = await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && !x.IsDeleted && (x.SenderEmail == user.Email || x.ReceiverEmail == user.Email));
    if (message == null)
    {
        return NotFound();
    }

    message.IsDeleted = true;
    message.DeletedAt = DateTime.Now;
    await _context.SaveChangesAsync();

    return RedirectToAction(returnAction);
}

private void LoadCategories()
{
    var categories = _context.Categories.ToList();
    ViewBag.v = categories.Select(c => new SelectListItem
    {
        Text = c.CategoryName,
        Value = c.CategoryId.ToString(),
    });
}
    }
}