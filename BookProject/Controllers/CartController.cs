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

    // הוספת פריט לעגלה
    [HttpPost]
    public JsonResult AddToCart(int bookId, bool isBorrow)
    {
        if (Session["UserId"] == null)
        {
            return Json(new { success = false, message = "Please login to add items to cart" });
        }

        try
        {
            int userId = Convert.ToInt32(Session["UserId"]);

            // בדיקת הספר לפני בדיקות נוספות
            var bookToAdd = _context.Books.Find(bookId);
            if (bookToAdd == null)
            {
                return Json(new { success = false, message = "Book not found" });
            }

            // בדיקת מגבלת השאלות
            if (isBorrow)
            {
                var activeCartBorrows = _context.Orders
                    .Where(o => o.UserId == userId && o.Status == "InCart")
                    .SelectMany(o => o.OrderItems)
                    .Count(oi => oi.TypeBook);

                if (activeCartBorrows >= 3)
                {
                    return Json(new { 
                        success = false, 
                        message = "You can't borrow more than 3 books at a time" 
                    });
                }

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
                        message = "Book is currently unavailable. You've been added to the waiting list." 
                    });
                }

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

            var orderItem = new OrderItem
            {
                OrderId = currentCart.OrderId,
                BookId = bookId,
                Price = price,
                TypeBook = isBorrow
            };

            _context.OrderItems.Add(orderItem);
            currentCart.TotalAmount += price;
            _context.SaveChanges();

            var message = isBorrow ? 
                "Book has been added to cart for borrowing" : 
                "Book has been added to cart for purchase";

            return Json(new { success = true, message = message });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "An error occurred" });
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