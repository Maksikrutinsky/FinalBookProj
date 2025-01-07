using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BookProject.Controllers;
using BookProject.Models;

public class MyLibraryController : Controller
{
    private readonly EBookLibraryEntities _context;
    private readonly EmailService _emailService;
    
    public MyLibraryController()
    {
        _context = new EBookLibraryEntities();
        _emailService = new EmailService(
            ConfigurationManager.AppSettings["SmtpServer"],
            int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
            ConfigurationManager.AppSettings["SmtpEmail"],
            ConfigurationManager.AppSettings["SmtpPassword"]
        );
    }
    
    [HttpPost] 
    public async Task<ActionResult> DeleteBook(int bookId)
    {
        try
        {
            int userId = Convert.ToInt32(Session["UserId"]);
    
            // מצא את פריט ההזמנה הרלוונטי
            var orderItem = _context.OrderItems
                .FirstOrDefault(oi => oi.BookId == bookId && 
                                      oi.Order.UserId == userId && 
                                      oi.Order.Status == "Completed");

            if (orderItem == null)
            {
                return Json(new { success = false, message = "הספר לא נמצא בספרייה שלך" }, JsonRequestBehavior.AllowGet);
            }

            // אם זה ספר מושאל, החזר את העותק למלאי
            if (orderItem.TypeBook)
            {
                var book = _context.Books.Find(bookId);
                if (book != null)
                {
                    book.AvailableCopies++;
                    
                    // שליחת התראות למשתמשים ברשימת ההמתנה
                    var booksController = new BooksController();
                    await booksController.NotifyWaitingUsers(bookId);
                    
                }
            }

            // מחיקת הספר מפריטי ההזמנה
            _context.OrderItems.Remove(orderItem);
            _context.SaveChanges();

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "אירעה שגיאה במחיקת הספר" }, JsonRequestBehavior.AllowGet);
        }
    }

public async Task<ActionResult> Index()
{
    if (Session["UserId"] == null)
    {
        return RedirectToAction("Login", "Account");
    }

    int userId = Convert.ToInt32(Session["UserId"]);
    DateTime currentDate = DateTime.Now;
    DateTime expiryDate = currentDate.AddDays(-30);

    // בדיקת ספרים שזמן ההשאלה שלהם פג והחזרת העותקים למלאי
    var expiredBorrows = _context.OrderItems
        .Include(oi => oi.Book)
        .Include(oi => oi.Order)
        .Where(oi => oi.Order.UserId == userId && 
                    oi.TypeBook && 
                    oi.Order.Status == "Completed" &&
                    oi.Order.OrderDate < expiryDate)
        .ToList();

    foreach (var expiredBorrow in expiredBorrows)
    {
        // החזרת העותק למלאי
        expiredBorrow.Book.AvailableCopies++;
        
        // שינוי הסטטוס ל-Cancelled במקום Returned
        expiredBorrow.Order.Status = "Cancelled";

        try 
        {
            // שליחת התראות לרשימת המתנה
            var booksController = new BooksController();
            await booksController.NotifyWaitingUsers(expiredBorrow.BookId);
        }
        catch (Exception ex)
        {
            // לוג את השגיאה אבל להמשיך בתהליך
            System.Diagnostics.Debug.WriteLine($"Failed to notify waiting users: {ex.Message}");
        }
    }

    if (expiredBorrows.Any())
    {
        _context.SaveChanges();
    }

    // קבלת הספרים המושאלים הפעילים (רק אלו שלא פג תוקפם)
    var borrowedBooks = _context.OrderItems
        .Where(oi => oi.Order.UserId == userId && 
                    oi.Order.Status == "Completed" && 
                    oi.TypeBook &&
                    oi.Order.OrderDate >= expiryDate)
        .Include(oi => oi.Book)
        .Include(oi => oi.Order)
        .ToList();

    // קבלת הספרים שנקנו
    var purchasedBooks = _context.OrderItems
        .Where(oi => oi.Order.UserId == userId && 
                    oi.Order.Status == "Completed" && 
                    !oi.TypeBook)
        .Include(oi => oi.Book)
        .Include(oi => oi.Order)
        .ToList();

    var viewModel = new MyLibraryViewModel
    {
        BorrowedBooks = borrowedBooks,
        PurchasedBooks = purchasedBooks
    };

    return View(viewModel);
}

    public ActionResult GetBookFormats(int bookId)
    {
        var book = _context.Books.Find(bookId);
        if (book == null)
        {
            return Json(new { success = false, message = "הספר לא נמצא" }, JsonRequestBehavior.AllowGet);
        }

        var availableFormats = new List<object>();
    
        if (book.FormatPDF == true && !string.IsNullOrEmpty(book.PDFUrl)) 
            availableFormats.Add(new { format = "PDF", url = book.PDFUrl });
    
        if (book.FormatEpub == true && !string.IsNullOrEmpty(book.EPUBUrl)) 
            availableFormats.Add(new { format = "EPUB", url = book.EPUBUrl });
    
        if (book.FormatMobi == true && !string.IsNullOrEmpty(book.MOBIUrl)) 
            availableFormats.Add(new { format = "MOBI", url = book.MOBIUrl });
        
        if (book.FormatF2b == true && !string.IsNullOrEmpty(book.F2BUrl))
            availableFormats.Add(new { format = "F2B", url = book.F2BUrl });

        if (availableFormats.Count == 0)
        {
            return Json(new
            {
                success = false,
                message = "אין פורמטים זמינים להורדה עבור ספר זה"
            }, JsonRequestBehavior.AllowGet);
        }

        return Json(new
        {
            success = true,
            formats = availableFormats
        }, JsonRequestBehavior.AllowGet);
    }

    public ActionResult DownloadBook(int bookId, string format)
    {
        int userId = Convert.ToInt32(Session["UserId"]);
        bool hasAccess = _context.OrderItems
            .Any(oi => oi.BookId == bookId && 
                      oi.Order.UserId == userId && 
                      oi.Order.Status == "Completed");

        if (!hasAccess)
        {
            return Json(new { 
                success = false, 
                message = "אין גישה להורדת ספר זה" 
            }, JsonRequestBehavior.AllowGet);
        }

        var book = _context.Books.Find(bookId);
    
        string downloadUrl;
        switch (format.ToUpper())
        {
            case "PDF":
                downloadUrl = book.PDFUrl;
                break;
            case "EPUB":
                downloadUrl = book.EPUBUrl;
                break;
            case "MOBI":
                downloadUrl = book.MOBIUrl;
                break;
            case "F2B":
                downloadUrl = book.F2BUrl;
                break;
            default:
                downloadUrl = null;
                break;
        }

        if (string.IsNullOrEmpty(downloadUrl))
        {
            return Json(new { 
                success = false, 
                message = "פורמט לא זמין" 
            }, JsonRequestBehavior.AllowGet);
        }

        return Redirect(downloadUrl);
    }
}