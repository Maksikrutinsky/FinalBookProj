using System;
using System.Web.Mvc;
using System.Data.Entity;
using BookProject.Models;
using System.Linq;
using System.Collections.Generic;


namespace BookProject.Controllers
{
public class MyLibraryController : Controller
{
    private readonly EBookLibraryEntities _context;
    
    public MyLibraryController()
    {
        _context = new EBookLibraryEntities();
    }

    public ActionResult Index()
    {
        if (Session["UserId"] == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int userId = Convert.ToInt32(Session["UserId"]);
        DateTime currentDate = DateTime.Now;

        // מחיקת השאלות שפג תוקפן
        var expiredBorrows = _context.Borrows
            .Where(b => b.UserId == userId && b.ReturnDate < currentDate);
        _context.Borrows.RemoveRange(expiredBorrows);
        _context.SaveChanges();

        // קבלת הספרים המושאלים הפעילים
        DateTime expiryDate = currentDate.AddDays(-30);

        var borrowedBooks = _context.OrderItems
            .Where(oi => oi.Order.UserId == userId && 
                         oi.Order.Status == "Completed" && 
                         oi.TypeBook &&
                         oi.Order.OrderDate > expiryDate)
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
    
        // בדיקה וסינון פורמטים
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
        // בדיקת גישה - האם המשתמש קנה/השאיל את הספר
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
}