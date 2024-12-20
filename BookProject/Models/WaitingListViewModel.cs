using System;

namespace BookProject.Models
{
    public class WaitingListViewModel
    {
        public string Username { get; set; }
        public int Position { get; set; }
        public DateTime? RequestDate { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}