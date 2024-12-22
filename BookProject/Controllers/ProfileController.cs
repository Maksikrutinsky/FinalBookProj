using System;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Data.Entity;
using BookProject.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BookProject.Controllers
{
    public class ProfileController : Controller
    {
        private readonly EBookLibraryEntities _context;

        public ProfileController()
        {
            _context = new EBookLibraryEntities();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(string CurrentPassword, string NewPassword)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                int userId = (int)Session["UserId"];
                var user = _context.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, user.Password))
                {
                    return Json(new { success = false, message = "Current password is incorrect." });
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                _context.SaveChanges();

                return Json(new { success = true, message = "Password changed successfully!" });  
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while changing password." });
            }
        }
        
        public ActionResult Profile()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Address = user.Address,
                ZipCode = user.ZipCode,
                City = user.City,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            // מילוי נתוני הזמנות
            viewModel.Orders = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.OrderItems.Select(oi => oi.Book))
                .Where(o => o.UserId == userId && o.Status != "InCart")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // מילוי נתוני השאלות
            viewModel.Borrows = _context.Borrows
                .Include(b => b.Book)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BorrowDate)
                .ToList();

            // הזמנה נוכחית בעגלה
            viewModel.CurrentCart = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.OrderItems.Select(oi => oi.Book))
                .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UserProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    LoadUserData(model);
                    return View("Profile", model);
                }

                int userId = (int)Session["UserId"];
                var user = _context.Users.Find(userId);

                if (user == null)
                    return RedirectToAction("Login", "Account");

                if (_context.Users.Any(u => u.Username == model.Username && u.UserId != userId))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    LoadUserData(model);
                    return View("Profile", model);
                }

                if (_context.Users.Any(u => u.Email == model.Email && u.UserId != userId))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    LoadUserData(model);
                    return View("Profile", model);
                }

                user.Username = model.Username;
                user.Email = model.Email;
                user.Address = model.Address;
                user.ZipCode = model.ZipCode;
                user.City = model.City;
                user.PhoneNumber = model.PhoneNumber;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating profile.";
                LoadUserData(model);
                return View("Profile", model);
            }
        }

        private void LoadUserData(UserProfileViewModel model)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.UserId == model.UserId);

            if (user != null)
            {
                // מילוי נתוני הזמנות
                model.Orders = _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.OrderItems.Select(oi => oi.Book))
                    .Where(o => o.UserId == user.UserId && o.Status != "InCart")
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                // מילוי נתוני השאלות
                model.Borrows = _context.Borrows
                    .Include(b => b.Book)
                    .Where(b => b.UserId == user.UserId)
                    .OrderByDescending(b => b.BorrowDate)
                    .ToList();

                // הזמנה נוכחית בעגלה
                model.CurrentCart = _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.OrderItems.Select(oi => oi.Book))
                    .FirstOrDefault(o => o.UserId == user.UserId && o.Status == "InCart");

                // מילוי שאר הנתונים
                model.CreatedAt = user.CreatedAt;
                model.IsActive = user.IsActive;
                model.Address = user.Address;
                model.ZipCode = user.ZipCode;
                model.City = user.City;
                model.PhoneNumber = user.PhoneNumber;
            }
        }
    }
}