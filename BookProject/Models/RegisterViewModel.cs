using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage="you have to enter UserName")]
        [Display(Name = "UserName")]
        public string Username { get; set; } 
        
        [Required(ErrorMessage = "you have to enter email")] 
        [EmailAddress(ErrorMessage = "email is invalid")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "you have to enter password")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "passord must be at least 6 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "you have to enter confirm password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "passwords don't match")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }
}