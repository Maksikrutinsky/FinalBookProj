using System;
using System.Linq;
using System.Web.Mvc;
using BookProject.Filters;
using BookProject.Models;
using System.Data.Entity;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace BookProject.Controllers
{
    [CartCount]
    public class CartController : Controller
    {
        private readonly EBookLibraryEntities _context;
        private readonly EmailService _emailService;

        public CartController()
        {
            _context = new EBookLibraryEntities();
            _emailService = new EmailService(
                ConfigurationManager.AppSettings["SmtpServer"],
                int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                ConfigurationManager.AppSettings["SmtpEmail"],
                ConfigurationManager.AppSettings["SmtpPassword"]
            );
        }

        // הצגת פריטים בעגלה
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var currentCart = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.OrderItems.Select(oi => oi.Book))
                .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

            if (currentCart == null || !currentCart.OrderItems.Any())
            {
                ViewBag.Message = "Your cart is empty.";
                return View(new List<OrderItem>());
            }

            return View(currentCart.OrderItems.Where(oi => oi.Book.IsActive.GetValueOrDefault()));
        }

[HttpPost]
public JsonResult AddToCart(int bookId, bool isBorrow)
{
    if (Session["UserId"] == null)
    {
        return Json(new { success = false, message = "אנא התחבר כדי להוסיף פריטים לעגלה" });
    }

    try
    {
        int userId = Convert.ToInt32(Session["UserId"]);

        // בדיקת הספר לפני בדיקות נוספות
        var bookToAdd = _context.Books.Find(bookId);
        if (bookToAdd == null)
        {
            return Json(new { success = false, message = "הספר לא נמצא" });
        }

        // בדיקת מגבלת השאלות
        if (isBorrow)
        {
            // בדיקת מספר ספרים מושאלים כרגע (כולל אלו בעגלה וגם אלו שכבר הושאלו)
            var currentBorrowedBooksCount = _context.Orders
                .Where(order => order.UserId == userId && 
                            (order.Status == "InCart" || order.Status == "Completed"))
                .SelectMany(order => order.OrderItems)
                .Count(orderItem => orderItem.TypeBook);

            // אם כבר יש 3 ספרים מושאלים
            if (currentBorrowedBooksCount >= 3)
            {
                return Json(new { 
                    success = false, 
                    message = "לא ניתן להשאיל יותר משלושה ספרים בו-זמנית" 
                });
            }

            // בדיקת זמינות עותקים
            if (bookToAdd.AvailableCopies <= 0)
            {
                var waitingList = new WaitingList
                {
                    BookId = bookId,
                    UserId = userId,
                    RequestDate = DateTime.Now
                };
                _context.WaitingLists.Add(waitingList);
                _context.SaveChanges();

                return Json(new { 
                    success = false, 
                    message = "הספר אינו זמין כעת. הוספת לרשימת המתנה." 
                });
            }

            // הפחתת מספר עותקים זמינים
            bookToAdd.AvailableCopies--;
            _context.SaveChanges();
        }

        // מציאת או יצירת עגלה נוכחית
        var currentCart = _context.Orders
            .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

        if (currentCart == null)
        {
            currentCart = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "InCart",
                TotalAmount = 0
            };
            _context.Orders.Add(currentCart);
            _context.SaveChanges();
        }

        // חישוב מחיר
        decimal price = isBorrow ? bookToAdd.BorrowPrice : bookToAdd.BuyPrice;

        var orderItem1 = new OrderItem
        {
            OrderId = currentCart.OrderId,
            BookId = bookId,
            Price = price,
            TypeBook = isBorrow
        };

        _context.OrderItems.Add(orderItem1);
        currentCart.TotalAmount += price;
        _context.SaveChanges();

        var message = isBorrow ? 
            "הספר נוסף לעגלה להשאלה" : 
            "הספר נוסף לעגלה לרכישה";

        return Json(new { success = true, message = message });
    }
    catch (Exception ex)
    {
        // לוג השגיאה (מומלץ להוסיף)
        return Json(new { success = false, message = "אירעה שגיאה: " + ex.Message });
    }
}
        // הסרת פריט מהעגלה
        [HttpPost]
        public JsonResult RemoveFromCart(int orderItemId)
        {
            try
            {
                var orderItem = _context.OrderItems
                    .Include(oi => oi.Order)
                    .FirstOrDefault(oi => oi.OrderItemId == orderItemId);

                if (orderItem != null && orderItem.Order.UserId == Convert.ToInt32(Session["UserId"]))
                {
                    var order = orderItem.Order;
                    order.TotalAmount -= orderItem.Price;

                    if (orderItem.Book.Type == "Physical")
                    {
                        var book = _context.Books.Find(orderItem.BookId);
                        book.AvailableCopies++;
                    }

                    _context.OrderItems.Remove(orderItem);
                    _context.SaveChanges();

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Item not found or you don't have permission." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while removing the item." });
            }
        }

// תהליך Checkout
        [HttpGet]
        public ActionResult Checkout()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var currentCart = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.OrderItems.Select(oi => oi.Book))
                .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

            if (currentCart == null || !currentCart.OrderItems.Any())
            {
                return RedirectToAction("Index");
            }

            var viewModel = new CheckoutViewModel
            {
                Items = currentCart.OrderItems,
                Order = currentCart,
                TotalAmount = currentCart.TotalAmount,
                Payment = new PaymentViewModel
                { 
                    TotalAmount = currentCart.TotalAmount 
                }
            };

            return View(viewModel);
        }

// עיבוד תשלום
        [HttpPost]
        public async Task<ActionResult> ProcessPayment(CheckoutViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Checkout", model);
                }

                int userId = Convert.ToInt32(Session["UserId"]);
                var currentCart = _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.OrderItems.Select(oi => oi.Book))
                    .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

                if (currentCart == null || !currentCart.OrderItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Home");
                }

                bool paymentSuccessful = true; // Simulated payment result

                if (paymentSuccessful)
                {
                    currentCart.Status = "Completed";
                    currentCart.OrderDate = DateTime.Now;
                    _context.SaveChanges();

                    var user = _context.Users.Find(userId);
                    await SendPurchaseConfirmationEmailAsync(user.Email, currentCart);

                    TempData["SuccessMessage"] = "Payment was successful! Thank you for your purchase.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment failed. Please try again.";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        // שליחת אימייל אישור רכישה
        private async Task SendPurchaseConfirmationEmailAsync(string email, Order order)
        {
            string orderDetails = string.Join("<br>", order.OrderItems.Select(item => 
                $"- {item.Book.Title} ({item.Book.Type}) - {item.Price:C}"));

            orderDetails += $"<br><strong>Total Amount: {order.TotalAmount:C}</strong>";

            await _emailService.SendPurchaseConfirmationEmailAsync(email, orderDetails);
        }
    }
}