using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BookProject.Models;
using BookProject.Filters;

namespace BookProject.Controllers
{
    [CartCount]
    public class HomeController : Controller
    {
        private readonly EBookLibraryEntities db = new EBookLibraryEntities();
    
        public async Task<ActionResult> Index()
        {
            var bookReturn = new BookReturnJob();
            await bookReturn.CheckExpiredBooks();

            var topRatedBooks = db.Books
                .Where(b => b.IsActive == true)
                .Select(b => new
                {
                    Book = b,
                    AverageRating = b.Ratings.Any() ? b.Ratings.Average(r => r.RatingValue) : 0,
                    RatingsCount = b.Ratings.Count
                })
                .OrderByDescending(b => b.AverageRating)
                .ThenByDescending(b => b.RatingsCount)
                .Take(4)
                .Select(b => b.Book)
                .ToList();

            return View(topRatedBooks);  // חשוב שזה יהיה כאן
        }

        public ActionResult Contact()
        {
            return View();
        }
        
        public ActionResult About()
        {
            return View();
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