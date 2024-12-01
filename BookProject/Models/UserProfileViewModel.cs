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

        public virtual ICollection<Purchase> Purchases { get; set; }
        public virtual ICollection<Borrow> Borrows { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }

        [Display(Name = "Joining date")]
        public DateTime? CreatedAt { get; set; }

        public bool? IsActive { get; set; }
    }
}