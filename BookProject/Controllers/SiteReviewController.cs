using System;
using System.Linq;
using System.Web.Mvc;
using BookProject.Models;

namespace BookProject.Controllers
{
    public class SiteReviewController : Controller
    {
        private EBookLibraryEntities db = new EBookLibraryEntities();

        public ActionResult Index()
        {
            var reviews = db.Ratings
                .Where(r => r.Type == "Site")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SiteReviewViewModel
                {
                    RatingId = r.RatingId,
                    RatingValue = r.RatingValue,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Username = r.User.Username
                })
                .ToList();

            if (Session["UserId"] != null)
            {
                var userId = Convert.ToInt32(Session["UserId"]);
                ViewBag.CanAddReview = !db.Ratings.Any(r => r.UserId == userId && r.Type == "Site");
                ViewBag.HasOrderOrBorrow = db.Orders.Any(o => o.UserId == userId) ||
                                           db.Borrows.Any(b => b.UserId == userId);
            }

            return View(reviews);
        }

        // GET: SiteReview/Create - להצגת הטופס
        public ActionResult Create()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = Convert.ToInt32(Session["UserId"]);
            var hasOrderOrBorrow = db.Orders.Any(o => o.UserId == userId) ||
                                   db.Borrows.Any(b => b.UserId == userId);

            if (!hasOrderOrBorrow)
            {
                TempData["ErrorMessage"] = "You need to purchase or borrow a book before you can leave a review";
                return RedirectToAction("Index");
            }

            var hasExistingReview = db.Ratings.Any(r => r.UserId == userId && r.Type == "Site");
            if (hasExistingReview)
            {
                TempData["ErrorMessage"] = "You have already submitted a review";
                return RedirectToAction("Index");
            }

            return View(new SiteReviewViewModel());
        }

        // POST: SiteReview/Create - לשמירת הביקורת
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SiteReviewViewModel model)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var userId = Convert.ToInt32(Session["UserId"]);

                // בדיקה נוספת למניעת ביקורות כפולות
                var hasExistingReview = db.Ratings.Any(r => r.UserId == userId && r.Type == "Site");
                if (hasExistingReview)
                {
                    TempData["ErrorMessage"] = "You have already submitted a review";
                    return RedirectToAction("Index");
                }

                var rating = new Rating
                {
                    UserId = userId,
                    RatingValue = model.RatingValue,
                    Comment = model.Comment,
                    Type = "Site",
                    CreatedAt = DateTime.Now
                };

                db.Ratings.Add(rating);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thank you for your review!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}