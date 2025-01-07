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
                var bookToAdd = _context.Books.Find(bookId);
        
                if (bookToAdd == null)
                {
                    return Json(new { success = false, message = "הספר לא נמצא" });
                }

                if (isBorrow)
                {
                    // ספירת ספרים מושאלים קיימים (בעגלה ובהשאלה פעילה)
                    var currentBorrowedBooksCount = _context.Orders
                        .Where(order => order.UserId == userId && 
                                        (order.Status == "InCart" || order.Status == "Completed"))
                        .SelectMany(order => order.OrderItems)
                        .Count(orderItem => orderItem.TypeBook);

                    // ספירת ספרים מושאלים בעגלה הנוכחית
                    var currentCartBorrowedCount = _context.Orders
                        .Where(order => order.UserId == userId && order.Status == "InCart")
                        .SelectMany(order => order.OrderItems)
                        .Count(orderItem => orderItem.TypeBook);

                    // בדיקה אם הוספת הספר תעביר את הסך הכל מעבר ל-3
                    if (currentBorrowedBooksCount + currentCartBorrowedCount > 3)
                    {
                        return Json(new { 
                            success = false, 
                            message = "לא ניתן להשאיל יותר משלושה ספרים בו-זמנית" 
                        });
                    }

            // בדיקת זמינות הספר
            var firstWaitingUser = _context.WaitingLists
                .Where(w => w.BookId == bookId)
                .OrderBy(w => w.Position)
                .FirstOrDefault();

            // בדוק אם המשתמש ברשימת ההמתנה
            var userWaitingEntry = _context.WaitingLists
                .FirstOrDefault(w => w.BookId == bookId && w.UserId == userId);

            if (firstWaitingUser != null)
            {
                // אם המשתמש לא ברשימה בכלל
                if (userWaitingEntry == null)
                {
                    var lastPosition = _context.WaitingLists
                        .Where(w => w.BookId == bookId)
                        .Select(w => w.Position)
                        .DefaultIfEmpty(0)
                        .Max();

                    var waitingList = new WaitingList
                    {
                        BookId = bookId,
                        UserId = userId,
                        Position = lastPosition + 1,
                        RequestDate = DateTime.Now,
                        NotificationSent = false
                    };
                    _context.WaitingLists.Add(waitingList);
                    _context.SaveChanges();

                    return Json(new { 
                        success = false, 
                        message = "הספר שמור למשתמשים אחרים ברשימת ההמתנה. הוספת לרשימה במקום " + (lastPosition + 1)
                    });
                }
                else if (firstWaitingUser.UserId != userId)
                {
                    return Json(new { 
                        success = false, 
                        message = $"הספר שמור למשתמש אחר שממתין בתור. המיקום שלך ברשימה: {userWaitingEntry.Position}"
                    });
                }
            }
            else if (bookToAdd.AvailableCopies <= 0)
            {
                var waitingList = new WaitingList
                {
                    BookId = bookId,
                    UserId = userId,
                    Position = 1,
                    RequestDate = DateTime.Now,
                    NotificationSent = false
                };
                _context.WaitingLists.Add(waitingList);
                _context.SaveChanges();

                return Json(new { 
                    success = false, 
                    message = "הספר אינו זמין כרגע. הוספת לרשימת ההמתנה במקום ראשון"
                });
            }
        }

        // הוספה לעגלה
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

        decimal price = isBorrow ? bookToAdd.BorrowPrice : bookToAdd.BuyPrice;

        var newOrderItem = new OrderItem
        {
            OrderId = currentCart.OrderId,
            BookId = bookId,
            Price = price,
            TypeBook = isBorrow
        };

        _context.OrderItems.Add(newOrderItem);
        currentCart.TotalAmount += price;
        _context.SaveChanges();

        var message = isBorrow ? 
            "הספר נוסף לעגלה להשאלה" : 
            "הספר נוסף לעגלה לרכישה";

        return Json(new { success = true, message = message });
    }
    catch (Exception ex)
    {
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

                    _context.OrderItems.Remove(orderItem);
                    _context.SaveChanges();

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Item not found or you don't have permission to remove it." });
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

// מתודה חדשה לעדכון מיקומים
private void UpdateWaitingListPositions(int bookId)
{
    var waitingListEntries = _context.WaitingLists
        .Where(w => w.BookId == bookId)
        .OrderBy(w => w.RequestDate)
        .ToList();

    for (int i = 0; i < waitingListEntries.Count; i++)
    {
        waitingListEntries[i].Position = i + 1;
    }
    _context.SaveChanges();
}

// מתודות עדכון ProcessPayment ו-ProcessPayPalPayment 
[HttpPost]
public async Task<ActionResult> ProcessPayment(CheckoutViewModel model)
{
    try
    {
        if (model.Payment != null && model.Payment.CardNumber != null)
        {
            model.Payment.CardNumber = model.Payment.CardNumber.Replace(" ", "");
        }

        if (!ModelState.IsValid)
        {
            var userid = Convert.ToInt32(Session["UserId"]);
            var currentcart = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.OrderItems.Select(oi => oi.Book))
                .FirstOrDefault(o => o.UserId == userid && o.Status == "InCart");

            if (currentcart != null)
            {
                model.Items = currentcart.OrderItems;
                model.Order = currentcart;
                model.TotalAmount = currentcart.TotalAmount;
            }
            
            return View("Checkout", model);
        }

        int userId = Convert.ToInt32(Session["UserId"]);
        var currentCart = _context.Orders
            .Include(o => o.OrderItems.Select(oi => oi.Book))
            .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

        if (currentCart == null || !currentCart.OrderItems.Any())
        {
            TempData["ErrorMessage"] = "העגלה שלך ריקה";
            return RedirectToAction("Index", "Home");
        }

        // בדיקת זמינות ספרים להשאלה
        var unavailableItems = new List<OrderWaitingViewModel.WaitingItemInfo>();
        var itemsToRemove = new List<OrderItem>();

        foreach (var orderItem in currentCart.OrderItems)
        {
            if (orderItem.TypeBook) // אם זו השאלה
            {
                var book = orderItem.Book;
                
                if (book.AvailableCopies <= 0)
                {
                    // הוספה לרשימת המתנה
                    var position = _context.WaitingLists
                        .Where(w => w.BookId == book.BookId)
                        .Select(w => w.Position)
                        .DefaultIfEmpty(0)
                        .Max() + 1;

                    var waitingList = new WaitingList
                    {
                        BookId = book.BookId,
                        UserId = userId,
                        Position = position,
                        RequestDate = DateTime.Now,
                        NotificationSent = false
                    };
                    
                    _context.WaitingLists.Add(waitingList);
                    
                    // הוספה לרשימת הספרים הלא זמינים
                    unavailableItems.Add(new OrderWaitingViewModel.WaitingItemInfo 
                    { 
                        Book = book, 
                        Position = position 
                    });
                    
                    // הוספה לרשימת הפריטים להסרה מהעגלה
                    itemsToRemove.Add(orderItem);
                }
            }
        }

        // אם יש פריטים לא זמינים
        if (unavailableItems.Any())
        {
            // הסרת הפריטים הלא זמינים מהעגלה
            foreach (var item in itemsToRemove)
            {
                currentCart.TotalAmount -= item.Price;
                _context.OrderItems.Remove(item);
            }

            await _context.SaveChangesAsync();

            // עדכון העגלה הנוכחית
            currentCart = _context.Orders
                .Include(o => o.OrderItems.Select(oi => oi.Book))
                .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");

            var waitingModel = new OrderWaitingViewModel
            {
                WaitingItems = unavailableItems,
                RemainingItems = currentCart?.OrderItems.ToList() ?? new List<OrderItem>()
            };

            return View("OrderWaiting", waitingModel);
        }

        // אם כל הפריטים זמינים, המשך בתהליך התשלום
        bool paymentSuccessful = true;

        if (paymentSuccessful)
        {
            foreach (var orderItem in currentCart.OrderItems)
            {
                if (orderItem.TypeBook)
                {
                    var book = orderItem.Book;
                    book.AvailableCopies--;

                    var waitingEntry = _context.WaitingLists
                        .FirstOrDefault(w => w.BookId == book.BookId && w.UserId == userId);
                    if (waitingEntry != null)
                    {
                        // הסרת הרשומה
                        _context.WaitingLists.Remove(waitingEntry);
                        
                        // מציאת כל הרשומות שאחרי הרשומה שנמחקה
                        var laterEntries = _context.WaitingLists
                            .Where(w => w.BookId == book.BookId && w.Position > waitingEntry.Position)
                            .ToList();
                    
                        // עדכון המיקומים
                        foreach (var entry in laterEntries)
                        {
                            entry.Position--;
                        }

                        _context.SaveChanges();
                    }
                }
            }

            currentCart.Status = "Completed";
            currentCart.OrderDate = DateTime.Now;
            await _context.SaveChangesAsync();

            var user = _context.Users.Find(userId);
            await SendPurchaseConfirmationEmailAsync(user.Email, currentCart);

            var confirmationModel = new OrderConfirmationViewModel
            {
                OrderId = currentCart.OrderId,
                OrderItems = currentCart.OrderItems,
                TotalAmount = currentCart.TotalAmount
            };

            return View("OrderConfirmation", confirmationModel);
        }
        else
        {
            TempData["ErrorMessage"] = "התשלום נכשל. אנא נסה שנית.";
            return RedirectToAction("Checkout");
        }
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "אירעה שגיאה בעיבוד התשלום: " + ex.Message;
        return RedirectToAction("Checkout");
    }
}

[HttpPost]
public async Task<JsonResult> ProcessPayPalPayment(string orderID, dynamic paymentDetails)
{
    try
    {
        if (Session["UserId"] == null)
        {
            return Json(new { success = false, message = "User not logged in" });
        }

        int userId = Convert.ToInt32(Session["UserId"]);
        var currentCart = _context.Orders
            .Include(o => o.OrderItems.Select(oi => oi.Book))
            .FirstOrDefault(o => o.UserId == userId && o.Status == "InCart");
        
        if (currentCart == null || !currentCart.OrderItems.Any())
        {
            return Json(new { success = false, message = "Cart is empty" });
        }

        // עדכון ספרים להשאלה
        foreach (var orderItem in currentCart.OrderItems)
        {
            if (orderItem.TypeBook) // אם זו השאלה
            {
                var book = orderItem.Book;
                if (book.AvailableCopies <= 0)
                {
                    return Json(new { success = false, message = "One or more books are no longer available" });
                }
                book.AvailableCopies--;

                // טיפול ברשימת המתנה
                var waitingEntry = _context.WaitingLists
                    .FirstOrDefault(w => w.BookId == book.BookId && w.UserId == userId);
                if (waitingEntry != null)
                {
                    // הסרת הרשומה
                    _context.WaitingLists.Remove(waitingEntry);
                    
                    // מציאת כל הרשומות שאחרי הרשומה שנמחקה
                    var laterEntries = _context.WaitingLists
                        .Where(w => w.BookId == book.BookId && w.Position > waitingEntry.Position)
                        .ToList();
                
                    // עדכון המיקומים
                    foreach (var entry in laterEntries)
                    {
                        entry.Position--;
                    }

                    _context.SaveChanges();
                }
            }
        }

        // עדכון ההזמנה
        currentCart.Status = "Completed";
        currentCart.OrderDate = DateTime.Now;
        currentCart.PaymentMethod = "PayPal";

        await _context.SaveChangesAsync();

        // שליחת אימייל אישור
        var user = _context.Users.Find(userId);
        await SendPurchaseConfirmationEmailAsync(user.Email, currentCart);

        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}

public ActionResult PaymentSuccess()
{
    if (TempData["SuccessMessage"] == null)
    {
        TempData["SuccessMessage"] = "Thank you for your purchase!";
    }
    return RedirectToAction("Index", "Home");
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