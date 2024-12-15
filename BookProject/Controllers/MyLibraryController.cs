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
                         oi.Order.OrderDate > expiryDate)  // השינוי כאן
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

    // פעולה להורדת ספר בפורמט ספציפי
    public FileResult DownloadBook(int bookId, string format)
    {
        var book = _context.Books.Find(bookId);
        string fileName = $"{book.Title}.{format.ToLower()}";
        string filePath = Server.MapPath($"~/Content/Books/Sample.{format.ToLower()}");
        
        return File(filePath, GetMimeType(format), fileName);
    }

    private string GetMimeType(string format)
    {
        switch (format.ToLower())
        {
            case "pdf":
                return "application/pdf";
            case "epub":
                return "application/epub+zip";
            case "mobi":
                return "application/x-mobipocket-ebook";
            default:
                return "application/octet-stream";
        }
    }
}
}