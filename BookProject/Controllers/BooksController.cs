using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BookProject.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace BookProject.Controllers
{
    public class BooksController : Controller
    {
        private readonly EBookLibraryEntities _context;
        private readonly EmailService _emailService;
        
        public BooksController()
        {
            _context = new EBookLibraryEntities();
            _emailService = new EmailService(
                ConfigurationManager.AppSettings["SmtpServer"],
                int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                ConfigurationManager.AppSettings["SmtpEmail"],
                ConfigurationManager.AppSettings["SmtpPassword"]
            );
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
                                         b.Author.ToLower().Contains(searchTerm) ||
                                         b.Publisher.ToLower().Contains(searchTerm));
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

            foreach (var book in books)
            {
                if (book.DiscountEndDate.HasValue && book.DiscountEndDate < DateTime.Now)
                {
                    book.BuyPrice = book.PreviousPrice ?? book.BuyPrice;
                    book.PreviousPrice = null;
                    book.DiscountEndDate = null;
                }
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

    public ActionResult Details(int id)
    {
        var book = _context.Books
            .Include(b => b.Ratings.Select(r => r.User))
            .Include(b => b.WaitingLists.Select(w => w.User))
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
            ViewBag.CurrentUserId = currentUserId;

            var lastOrderItem = _context.Orders
                .Where(o => o.OrderItems.Any(oi => 
                    oi.BookId == id && 
                    o.UserId == currentUserId && 
                    o.Status == "Completed"))
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.BookId == id)
                .OrderByDescending(oi => oi.Order.OrderDate)
                .FirstOrDefault();

            ViewBag.HasPurchased = lastOrderItem != null;
            ViewBag.IsBorrowed = lastOrderItem?.TypeBook ?? false;
            ViewBag.PurchaseDate = lastOrderItem?.Order.OrderDate;
            
            ViewBag.IsInWaitingList = book.WaitingLists
                .Any(w => w.UserId == currentUserId);
        
            if (ViewBag.IsInWaitingList)
            {
                ViewBag.UserWaitingPosition = book.WaitingLists
                    .OrderBy(w => w.Position)
                    .ToList()
                    .FindIndex(w => w.UserId == currentUserId) + 1;
            }
        }
        else
        {
            ViewBag.IsAdmin = false;
            ViewBag.HasPurchased = false;
            ViewBag.IsBorrowed = false;
            ViewBag.IsInWaitingList = false;
        }

        ViewBag.AverageRating = GetAverageRating(id);

        ViewBag.WaitingList = book.WaitingLists
            .OrderBy(w => w.Position)
            .Select(w => new WaitingListViewModel
            {
                Username = w.User.Username,
                Position = w.Position,
                RequestDate = w.RequestDate,
                IsCurrentUser = w.UserId == (userId != null ? Convert.ToInt32(userId) : 0)
            })
            .ToList();

        return View(book);
    }
    
    [HttpPost]
    public async Task<ActionResult> DeleteBook(int id)
    {
        var userId = Session["UserId"];
        if (userId == null)
        {
            return Json(new { success = false, message = "יש להתחבר כדי לבצע פעולה זו" });
        }

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
        if (!isAdmin)
        {
            return Json(new { success = false, message = "אין לך הרשאה לבצע פעולה זו" });
        }

        try
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return Json(new { success = false, message = "הספר לא נמצא" });
            }

            book.IsActive = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "הספר נמחק בהצלחה" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "אירעה שגיאה במחיקת הספר: " + ex.Message });
        }
    }

        public async Task NotifyWaitingUsers(int bookId)
        {
            var book = _context.Books.Find(bookId);
            if (book == null) return;  // רק בדיקת null

            var waitingUsers = _context.WaitingLists 
                .Where(w => w.BookId == bookId && w.NotificationSent == false)
                .OrderBy(w => w.Position)
                .Take(3)
                .Include(w => w.User)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Found {waitingUsers.Count} waiting users for book {bookId}");

            foreach (var waitEntry in waitingUsers)
            {
                if (waitEntry.User?.Email != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Trying to send email to {waitEntry.User.Email}");
                        await _emailService.SendBookAvailableEmailAsync(waitEntry.User.Email, book.Title);
                        waitEntry.NotificationSent = true;
                        System.Diagnostics.Debug.WriteLine($"Successfully sent email to {waitEntry.User.Email}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send email: {ex.Message}");
                    }
                }
            }
    
            _context.SaveChanges();
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
                return Json(new { success = false, message = "You must be logged in!" });
            }

            int currentUserId = Convert.ToInt32(userId);
            var isAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
            if (!isAdmin)
            {
                return Json(new { success = false, message = "You don't have admin permissions" });
            }

            // בדיקת מחיר קנייה
            if (book.BuyPrice <= 0)
            {
                return Json(new { success = false, message = "Must enter purchase price for the book" });
            }

            // בדיקה רק אם הספר זמין להשאלה
            if (book.IsBorrowable == true && book.BorrowPrice <= 0)
            {
                return Json(new { success = false, message = "Must enter borrowing price when book is available for borrowing" });
            }

            try
            {
                book.IsActive = true;
                book.Type = "Physical";
                book.IsBuyable = true;

                // אם הספר לא להשאלה, נאפס את מחיר ההשאלה
                if (!book.IsBorrowable.GetValueOrDefault())
                {
                    book.BorrowPrice = 0;
                }

                _context.Books.Add(book);
                _context.SaveChanges();

                return Json(new { success = true, message = "Book added successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
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
            var isAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
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
        public ActionResult LeaveWaitingList(int bookId)
        {
            try
            {
                var userId = Session["UserId"];
                if (userId == null)
                {
                    return Json(new { success = false, message = "יש להתחבר כדי לבצע פעולה זו" });
                }

                int currentUserId = Convert.ToInt32(userId);
                
                var waitingEntry = _context.WaitingLists
                    .FirstOrDefault(w => w.BookId == bookId && w.UserId == currentUserId);
            
                if (waitingEntry == null)
                {
                    return Json(new { success = false, message = "לא נמצאת ברשימת ההמתנה לספר זה" });
                }

                // מחק את הרשומה
                _context.WaitingLists.Remove(waitingEntry);
        
                // עדכן את המיקומים של כל המשתמשים שאחריו ברשימה
                var laterEntries = _context.WaitingLists
                    .Where(w => w.BookId == bookId && w.Position > waitingEntry.Position)
                    .ToList();
            
                foreach (var entry in laterEntries)
                {
                    entry.Position--;
                }

                _context.SaveChanges();

                return Json(new { success = true, message = "הוסרת בהצלחה מרשימת ההמתנה" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "אירעה שגיאה: " + ex.Message });
            }
        }
        
[HttpPost]
public async Task<ActionResult> Edit(Book book)
{
    var userId = Session["UserId"];
    if (userId == null)
    {
        return Json(new { success = false, message = "You must be logged in!" });
    }

    int currentUserId = Convert.ToInt32(userId);
    var isAdmin = _context.Users.FirstOrDefault(u => u.UserId == currentUserId)?.IsAdmin ?? false;
    if (!isAdmin)
    {
        return Json(new { success = false, message = "You don't have admin permissions" });
    }

    if (book.BuyPrice <= 0)
    {
        return Json(new { success = false, message = "Must enter purchase price for the book" });
    }

    if (book.IsBorrowable == true && book.BorrowPrice <= 0)
    {
        return Json(new { success = false, message = "Must enter borrowing price when book is available for borrowing" });
    }

    try
    {
        var existingBook = _context.Books.Find(book.BookId);
        if (existingBook == null)
        {
            return Json(new { success = false, message = "Book not found" });
        }

        // Handle discount logic
        if (book.PreviousPrice.HasValue)
        {
            if (book.PreviousPrice <= book.BuyPrice)
            {
                return Json(new { success = false, message = "Original price must be higher than the discounted price" });
            }

            if (!book.DiscountEndDate.HasValue)
            {
                return Json(new { success = false, message = "Discount end date is required" });
            }

            if (book.DiscountEndDate <= DateTime.Now)
            {
                return Json(new { success = false, message = "Discount end date must be in the future" });
            }
        }
        else
        {
            // If removing discount, current price becomes previous price
            if (existingBook.PreviousPrice.HasValue)
            {
                book.BuyPrice = existingBook.PreviousPrice.Value;
            }
        }
        
        existingBook.Title = book.Title;
        existingBook.Author = book.Author;
        existingBook.Description = book.Description;
        existingBook.Genre = book.Genre;
        existingBook.PublishYear = book.PublishYear;
        existingBook.Publisher = book.Publisher;
        existingBook.CoverImageUrl = book.CoverImageUrl;
        existingBook.BuyPrice = book.BuyPrice;
        existingBook.PreviousPrice = book.PreviousPrice;
        existingBook.DiscountEndDate = book.DiscountEndDate;
        existingBook.BorrowPrice = book.BorrowPrice;
        existingBook.IsBorrowable = book.IsBorrowable;
        existingBook.AvailableCopies = book.AvailableCopies;
        existingBook.PDFUrl = book.PDFUrl;
        existingBook.EPUBUrl = book.EPUBUrl;
        existingBook.MOBIUrl = book.MOBIUrl;
        existingBook.F2BUrl = book.F2BUrl;
        existingBook.IsActive = true;

        _context.SaveChanges();

        if (book.AvailableCopies > 0)
        {
            await NotifyWaitingUsers(book.BookId);
        }

        return Json(new { success = true, message = "Book updated successfully!" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Error updating book: " + ex.Message });
    }
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
                     b.Author.Contains(query) || 
                     b.Publisher.Contains(query)))  
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

        [HttpPost]
        public ActionResult AddRating(int bookId, int rating, string comment)
        {
            try
            {
                var userId = Session["UserId"];
                if (userId == null)
                {
                    return Json(new { success = false, message = "Please log in to rate books." });
                }

                int currentUserId = Convert.ToInt32(userId);
                
                var hasPurchased = _context.Orders
                    .Any(o => o.OrderItems.Any(oi => 
                        oi.BookId == bookId && 
                        o.UserId == currentUserId && 
                        o.Status == "Completed"));
                    
                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Only users who have purchased this book can rate it." });
                }

                var existingRating = _context.Ratings
                    .FirstOrDefault(r => r.BookId == bookId && r.UserId == currentUserId);

                if (existingRating != null)
                {
                    existingRating.RatingValue = rating;
                    existingRating.Comment = comment;
                    existingRating.CreatedAt = DateTime.Now;
                }
                else
                {
                    var newRating = new Rating
                    {
                        BookId = bookId,
                        UserId = currentUserId,
                        RatingValue = rating,
                        Comment = comment,
                        CreatedAt = DateTime.Now,
                        Type = "Book"
                    };
                    _context.Ratings.Add(newRating);
                }

                _context.SaveChanges();
                
                var averageRating = GetAverageRating(bookId);
                return Json(new { 
                    success = true, 
                    message = "Rating submitted successfully!",
                    averageRating = averageRating
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting rating: " + ex.Message });
            }
        }

        private double GetAverageRating(int bookId)
        {
            var ratings = _context.Ratings.Where(r => r.BookId == bookId);
            if (!ratings.Any())
                return 0;

            return Math.Round(ratings.Average(r => r.RatingValue), 1);
        }
        
        [HttpPost]
        public ActionResult JoinWaitingList(int bookId)
        {
            try
            {
                var userId = Session["UserId"];
                if (userId == null)
                {
                    return Json(new { success = false, message = "יש להתחבר כדי להצטרף לרשימת המתנה" });
                }

                int currentUserId = Convert.ToInt32(userId);
                var book = _context.Books.Find(bookId);
        
                if (book == null || book.IsBorrowable != true)
                {
                    return Json(new { success = false, message = "הספר אינו זמין להשאלה" });
                }

                // בדיקה אם המשתמש כבר ברשימת ההמתנה
                var existingWait = _context.WaitingLists
                    .FirstOrDefault(w => w.BookId == bookId && w.UserId == currentUserId);
            
                if (existingWait != null)
                {
                    return Json(new { success = false, message = "הנך כבר ברשימת ההמתנה לספר זה" });
                }

                // מציאת המיקום האחרון ברשימה
                var lastPosition = _context.WaitingLists
                    .Where(w => w.BookId == bookId)
                    .Select(w => w.Position)
                    .DefaultIfEmpty(0)
                    .Max();

                var waitingEntry = new WaitingList
                {
                    BookId = bookId,
                    UserId = currentUserId,
                    Position = lastPosition + 1,
                    RequestDate = DateTime.Now,
                    NotificationSent = false
                };

                _context.WaitingLists.Add(waitingEntry);
                _context.SaveChanges();

                return Json(new { success = true, message = "נוספת בהצלחה לרשימת ההמתנה" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "אירעה שגיאה: " + ex.Message });
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