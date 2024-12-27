using System.Collections.Generic;

namespace BookProject.Models
{
    public class OrderWaitingViewModel
    {
        public class WaitingItemInfo
        {
            public Book Book { get; set; }
            public int Position { get; set; }
        }

        public List<WaitingItemInfo> WaitingItems { get; set; }
        public List<OrderItem> RemainingItems { get; set; }
    }
}