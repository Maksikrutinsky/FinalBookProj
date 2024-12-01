using System.Web.Mvc;
using System.Threading.Tasks;
using System.Data.Entity;
using BookProject.Models;
using System.Linq;

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
        public ActionResult UpdateProfile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            int userId = (int)Session["UserId"];
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // בדיקה שהשם משתמש לא תפוס
            if (_context.Users.Any(u => u.Username == model.Username && u.UserId != userId))
            {
                ModelState.AddModelError("Username", "The username already exists in the system.");
                return View("Profile", model);
            }

            // עדכון פרטי המשתמש
            user.Username = model.Username;
            user.Email = model.Email;

            _context.SaveChanges();
            TempData["SuccessMessage"] = "The details have been updated successfully!";

            return RedirectToAction("Profile");
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