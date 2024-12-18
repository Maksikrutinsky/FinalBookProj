using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using BookProject.Controllers;
using BookProject.Models;

public class BookReturnJob
{
    private readonly EBookLibraryEntities _context;
    
    public BookReturnJob()
    {
        _context = new EBookLibraryEntities();
    }
    public async Task CheckExpiredBooks()
    {
        DateTime expiryDate = DateTime.Now.AddDays(-30);

        var expiredBorrows = _context.OrderItems
            .Include(oi => oi.Book)
            .Include(oi => oi.Order)
            .Where(oi => oi.TypeBook && 
                         oi.Order.Status == "Completed" &&
                         oi.Order.OrderDate < expiryDate)
            .ToList();

        var booksController = new BooksController();

        foreach (var expiredBorrow in expiredBorrows)
        {
            expiredBorrow.Book.AvailableCopies++;
            expiredBorrow.Order.Status = "Cancelled";

            // וודא שזו קריאה אסינכרונית
            await booksController.NotifyWaitingUsers(expiredBorrow.BookId);
        }

        if (expiredBorrows.Any())
        {
            _context.SaveChanges();
        }
    }
}