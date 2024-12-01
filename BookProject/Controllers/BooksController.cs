using System;
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
        
        // GET: Books/Gallery with optional search
        public ActionResult Gallery(string searchTerm, string sortOrder)
        {
            // התחל עם ספרים פעילים
            var books = _context.Books.Where(b => b.IsActive == true);
    
            // חיפוש אם יש מונח חיפוש
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                books = books.Where(b => b.Title.ToLower().Contains(searchTerm) || 
                                         b.Author.ToLower().Contains(searchTerm));
                ViewBag.CurrentSearch = searchTerm;
            }

            // מיון לפי הבחירה
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

            // שמור את המיון הנוכחי ב-ViewBag
            ViewBag.CurrentSort = sortOrder;
    
            var booksWithRatings = books.ToList().Select(b => new
            {
                Book = b,
                AverageRating = GetAverageRating(b.BookId)
            });
    
            ViewBag.BooksRatings = booksWithRatings.ToDictionary(b => b.Book.BookId, b => b.AverageRating);
    
            return View(books.ToList());
        }
        
        private double GetAverageRating(int bookId)
        {
            var ratings = _context.Ratings.Where(r => r.BookId == bookId);
            if (!ratings.Any())
                return 0;
    
            return Math.Round(ratings.Average(r => r.RatingValue), 1);
        }
    }
}