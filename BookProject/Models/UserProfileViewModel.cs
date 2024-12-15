using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
    public class UserProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Required field")]
        [Display(Name = "User name")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Required field")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Display(Name = "New Password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
        public string ConfirmNewPassword { get; set; }

        // רשימת כל ההזמנות שהושלמו
        public virtual ICollection<Order> Orders { get; set; }
        
        // העגלה הנוכחית (הזמנה עם סטטוס InCart)
        public virtual Order CurrentCart { get; set; }
        
        // רשימת ההשאלות
        public virtual ICollection<Borrow> Borrows { get; set; }

        [Display(Name = "Joining date")]
        public DateTime? CreatedAt { get; set; }

        public bool? IsActive { get; set; }
        
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Zip Code")]
        [RegularExpression(@"^\d{5,7}$", ErrorMessage = "Invalid zip code")]
        public string ZipCode { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Phone Number")]
        [RegularExpression(@"^0\d{8,9}$", ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        // תכונות עזר לתצוגה
        public int TotalOrders => Orders?.Count ?? 0;
        public int TotalBorrows => Borrows?.Count ?? 0;
        public int CartItemsCount => CurrentCart?.OrderItems?.Count ?? 0;
        public decimal CartTotal => CurrentCart?.TotalAmount ?? 0;
    }
}