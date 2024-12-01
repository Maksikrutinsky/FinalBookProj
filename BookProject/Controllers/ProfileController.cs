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

        // GET: Profile
        public ActionResult Profile()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var user = _context.Users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .Include(u => u.Carts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                Purchases = user.Purchases,
                Borrows = user.Borrows,
                Carts = user.Carts
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadUserData(model);
                return View("Profile", model);
            }

            int userId = (int)Session["UserId"];
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if username is taken
            if (_context.Users.Any(u => u.Username == model.Username && u.UserId != userId))
            {
                ModelState.AddModelError("Username", "The username already exists in the system.");
                LoadUserData(model);
                return View("Profile", model);
            }

            // Check if email is taken
            if (_context.Users.Any(u => u.Email == model.Email && u.UserId != userId))
            {
                ModelState.AddModelError("Email", "The email address already exists in the system.");
                LoadUserData(model);
                return View("Profile", model);
            }

            // Update user details
            user.Username = model.Username;
            user.Email = model.Email;

            _context.SaveChanges();
            TempData["SuccessMessage"] = "The details have been updated successfully!";

            return RedirectToAction("Profile");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(UserProfileViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmNewPassword))
                {
                    return Json(new { success = false, message = "All password fields are required." });
                }

                int userId = (int)Session["UserId"];
                var user = _context.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Verify current password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
                {
                    return Json(new { success = false, message = "Current password is incorrect." });
                }

                // Validate new password
                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    return Json(new { success = false, message = "New password and confirmation do not match." });
                }

                // Hash new password using BCrypt
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.SaveChanges();

                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while changing the password." });
            }
        }

        private void LoadUserData(UserProfileViewModel model)
        {
            var user = _context.Users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .Include(u => u.Carts)
                .FirstOrDefault(u => u.UserId == model.UserId);

            if (user != null)
            {
                model.Purchases = user.Purchases;
                model.Borrows = user.Borrows;
                model.Carts = user.Carts;
                model.CreatedAt = user.CreatedAt;
                model.IsActive = user.IsActive;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}