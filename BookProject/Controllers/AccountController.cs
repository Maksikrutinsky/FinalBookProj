using System;
using System.Configuration;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using BookProject.Models;

public class AccountController : Controller
{
    private readonly EBookLibraryEntities _context;
    private readonly EmailService _emailService;

    public AccountController()
    {
        _context = new EBookLibraryEntities();
        _emailService = new EmailService(
            ConfigurationManager.AppSettings["SmtpServer"],
            int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
            ConfigurationManager.AppSettings["SmtpEmail"],
            ConfigurationManager.AppSettings["SmtpPassword"]
        );
    }
    
    public AccountController(EBookLibraryEntities context)
    {
        _context = context;
        _emailService = new EmailService(
            ConfigurationManager.AppSettings["SmtpServer"],
            int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
            ConfigurationManager.AppSettings["SmtpEmail"],
            ConfigurationManager.AppSettings["SmtpPassword"]
        );
    }

    [HttpGet]
    public ActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<ActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                Session["UserId"] = user.UserId;
                Session["UserName"] = user.Username;
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid email or password");
        }
        return View(model);
    }

    [HttpGet]
    public ActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<ActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
            
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = hashedPassword,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
            }
            catch
            {
                // לוג את השגיאה אבל להמשיך
            }

            Session["UserId"] = user.UserId;
            Session["UserName"] = user.Username;
            
            return RedirectToAction("Index", "Home");
        }
        return View(model);
    }

    public ActionResult Logout()
    {
        Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public ActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<ActionResult> ForgotPassword(string Email)
    {
        if (!string.IsNullOrEmpty(Email))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user != null)
            {
                string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                Session[$"ResetToken_{Email}"] = token;
                Session[$"ResetTokenExpiry_{Email}"] = DateTime.Now.AddHours(1);

                var resetLink = Url.Action("ResetPassword", "Account",
                    new { email = Email, token = token },
                    protocol: Request.Url.Scheme);

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(Email, resetLink);
                }
                catch
                {
                    ModelState.AddModelError("", "Error sending email. Please try again later.");
                    return View();
                }
            }
            // תמיד מחזירים את אותה תגובה מסיבות אבטחה
            return RedirectToAction("ForgotPasswordConfirmation");
        }
        return View();
    }

    [HttpGet]
    public ActionResult ResetPassword(string email, string token)
    {
        var model = new User { Email = email };
        ViewBag.Token = token;
        return View(model);
    }

    [HttpPost]
    public async Task<ActionResult> ResetPassword(string Email, string Password, string Token)
    {
        if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
        {
            var storedToken = Session[$"ResetToken_{Email}"]?.ToString();
            var tokenExpiry = Session[$"ResetTokenExpiry_{Email}"] as DateTime?;

            if (storedToken != Token || tokenExpiry == null || tokenExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "Invalid or expired token");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                await _context.SaveChangesAsync();
                
                Session.Remove($"ResetToken_{Email}");
                Session.Remove($"ResetTokenExpiry_{Email}");
                
                return RedirectToAction("Login");
            }
        }
        return View();
    }

    public ActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    public ActionResult ResetPasswordConfirmation()
    {
        return View();
    }
}