using System.Collections.Generic;
using BookProject.Models;

namespace BookProject.Models
{
    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public decimal TotalAmount { get; set; }
    }

}