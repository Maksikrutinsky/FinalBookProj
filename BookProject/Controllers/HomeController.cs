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
            return View();
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