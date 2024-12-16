using System.Collections.Generic;

namespace BookProject.Models
{
    public class MyLibraryViewModel
    {
        public List<OrderItem> BorrowedBooks { get; set; } = new List<OrderItem>();
        public List<OrderItem> PurchasedBooks { get; set; } = new List<OrderItem>();
    }
}