using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BookProject.Models;
using System.Collections.Generic;

namespace BookProject.Controllers
{
   public class BooksController : Controller
{
    private readonly EBookLibraryEntities _context;

    public BooksController()
    {
        _context = new EBookLibraryEntities();
    }

    private List<SelectListItem> GetGenresFromDb()
    {
        return _context.Books
            .Where(b => b.IsActive == true)
            .Select(b => b.Genre)
            .Distinct()
            .Select(g => new SelectListItem { Text = g, Value = g })
            .ToList();
    }

    [HttpGet]
    public ActionResult Gallery(string searchTerm, string genre, string sortOrder)
    {
        var userId = Session["UserId"];
        if (userId != null)
        {
            int id = Convert.ToInt32(userId);
            ViewBag.IsAdmin = _context.Users.FirstOrDefault(u => u.UserId == id)?.IsAdmin ?? false;
        }
        else
        {
            ViewBag.IsAdmin = false;
        }

        var books = _context.Books.Where(b => b.IsActive == true);

        ViewBag.Genres = GetGenresFromDb();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            books = books.Where(b => b.Title.ToLower().Contains(searchTerm) || 
                                   b.Author.ToLower().Contains(searchTerm));
            ViewBag.CurrentSearch = searchTerm;
        }

        if (!string.IsNullOrEmpty(genre))
        {
            books = books.Where(b => b.Genre == genre);
        }

        switch (sortOrder)
        {
            case "price_asc":
                books = books.OrderBy(b => b.BuyPrice);
                break;
            case "price_desc":
                books = books.OrderByDescending(b => b.BuyPrice);
                break;
            case "year_desc":
                books = books.OrderByDescending(b => b.PublishYear);
                break;
            case "year_asc":
                books = books.OrderBy(b => b.PublishYear);
                break;
            case "rating":
                books = books.OrderByDescending(b => b.Ratings.Average(r => r.RatingValue));
                break;
            default:
                books = books.OrderByDescending(b => b.PublishYear);
                break;
        }

        var booksWithRatings = books.ToList().Select(b => new
        {
            Book = b,
            AverageRating = GetAverageRating(b.BookId)
        });

        ViewBag.BooksRatings = booksWithRatings.ToDictionary(b => b.Book.BookId, b => b.AverageRating);

        if (Request.IsAjaxRequest())
        {
            return PartialView("_BooksGrid", books.ToList());
        }

        return View(books.ToList());
    }

    private double GetAverageRating(int bookId)
    {
        var ratings = _context.Ratings.Where(r => r.BookId == bookId);
        if (!ratings.Any())
            return 0;

        return Math.Round(ratings.Average(r => r.RatingValue), 1);
    }

    public ActionResult Details(int id)
    {
        var book = _context.Books
            .Include(b => b.Ratings)
            .FirstOrDefault(b => b.BookId == id && b.IsActive == true);

        if (book == null)
        {
            return HttpNotFound();
        }

        var userId = Session["UserId"];
        if (userId != null)
        {
            int currentUserId = Convert.ToInt32(userId);
            ViewBag.IsAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
        
            // בדיקה האם הספר נרכש באמצעות Orders
            ViewBag.HasPurchased = _context.Orders
                .Any(o => o.OrderItems.Any(oi => 
                    oi.BookId == id && 
                    o.UserId == currentUserId && 
                    o.Status == "Completed"));
        }
        else
        {
            ViewBag.IsAdmin = false;
            ViewBag.HasPurchased = false;
        }

        ViewBag.AverageRating = GetAverageRating(id);
        return View(book);
    }

    [HttpGet]
    public ActionResult AddBook()
    {
        var userId = Session["UserId"];
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
        if (!isAdmin)
        {
            return RedirectToAction("Gallery");
        }

        ViewBag.Genres = GetGenresFromDb();
        return View();
    }

    [HttpPost]
    public ActionResult AddBook(Book book)
    {
        var userId = Session["UserId"];
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
        if (!isAdmin)
        {
            return RedirectToAction("Gallery");
        }

        if (ModelState.IsValid)
        {
            try
            {
                book.IsActive = true;
                _context.Books.Add(book);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "הספר נוסף בהצלחה!";
                return RedirectToAction("Gallery");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "אירעה שגיאה בהוספת הספר: " + ex.Message;
            }
        }

        ViewBag.Genres = GetGenresFromDb();
        return View(book);
    }

    [HttpGet]
    public ActionResult Edit(int id)
    {
        var userId = Session["UserId"];
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int currentUserId = Convert.ToInt32(userId);
        var user = _context.Users.FirstOrDefault(u => u.UserId == currentUserId);
        var isAdmin = user?.IsAdmin == true;
        if (!isAdmin)
        {
            return RedirectToAction("Gallery");
        }

        var book = _context.Books.Find(id);
        if (book == null)
        {
            return HttpNotFound();
        }

        ViewBag.Genres = GetGenresFromDb();
        return View("EditBook", book);
    }

    [HttpPost]
    public ActionResult Edit(Book book)
    {
        var userId = Session["UserId"];
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int currentUserId = Convert.ToInt32(userId);
        var user = _context.Users.FirstOrDefault(u => u.UserId == currentUserId);
        var isAdmin = user?.IsAdmin == true;
        if (!isAdmin)
        {
            return RedirectToAction("Gallery");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingBook = _context.Books.Find(book.BookId);
                if (existingBook == null)
                {
                    return HttpNotFound();
                }

                // עדכון שאר הערכים
                _context.Entry(existingBook).CurrentValues.SetValues(book);
            
                // וידוא שהספר נשאר אקטיבי
                existingBook.IsActive = true;  // במסד נתונים זה יהיה 1

                _context.SaveChanges();

                TempData["SuccessMessage"] = "הספר עודכן בהצלחה!";
                return RedirectToAction("Details", new { id = book.BookId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "אירעה שגיאה בעדכון הספר: " + ex.Message;
            }
        }

        ViewBag.Genres = GetGenresFromDb();
        return View("EditBook", book);
    }
    
    
    [HttpGet]
    public JsonResult Search(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return Json(new List<object>(), JsonRequestBehavior.AllowGet);
        }

        var results = _context.Books
            .Where(b => b.IsActive == true &&
                        (b.Title.Contains(query) || 
                         b.Author.Contains(query)))
            .Select(b => new
            {
                b.BookId,
                b.Title,
                b.Author,
                b.CoverImageUrl,
                b.BuyPrice
            })
            .Take(5)
            .ToList();

        return Json(results, JsonRequestBehavior.AllowGet);
    }
    
    
    
}
}