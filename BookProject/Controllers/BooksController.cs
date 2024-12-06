using System;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Linq;
using System.Web.Mvc;
using BookProject.Models;

namespace BookProject.Controllers
{
public class BooksController : Controller
{
    private readonly EBookLibraryEntities _context;

    public BooksController()
    {
        _context = new EBookLibraryEntities();
    }

    [HttpGet]
    public ActionResult Gallery(string searchTerm, string genre, string sortOrder)
    {
        var books = _context.Books.Where(b => b.IsActive == true);

        // Get genres for dropdown
        ViewBag.Genres = _context.Books
            .Where(b => b.IsActive == true)
            .Select(b => b.Genre)
            .Distinct()
            .Select(g => new SelectListItem { Text = g, Value = g })
            .ToList();

        // Search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            books = books.Where(b => b.Title.ToLower().Contains(searchTerm) || 
                                   b.Author.ToLower().Contains(searchTerm));
            ViewBag.CurrentSearch = searchTerm;
        }

        // Genre filter
        if (!string.IsNullOrEmpty(genre))
        {
            books = books.Where(b => b.Genre == genre);
        }

        // Sort
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

        ViewBag.AverageRating = GetAverageRating(id);

        if (Session["UserId"] != null)
        {
            var userId = Convert.ToInt32(Session["UserId"]);
            ViewBag.HasPurchased = _context.Purchases
                .Any(p => p.BookId == id && p.UserId == userId && p.PurchaseStatus == true);
        }
        return View(book);
    }
    private int GetCurrentUserId()
    {
        // יש להתאים את זה לפי המימוש שלך של זיהוי משתמשים
        return Convert.ToInt32(User.Identity.Name);
    }
    
}
}