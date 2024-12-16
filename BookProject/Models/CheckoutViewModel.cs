using System.Collections.Generic;

namespace BookProject.Models
{
    public class CheckoutViewModel
    {
        public IEnumerable<OrderItem> Items { get; set; }
        public Order Order { get; set; }
        public decimal TotalAmount { get; set; }
    }
}