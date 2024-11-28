using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "You have to enter your email")]
        [EmailAddress(ErrorMessage = "email address is not valid")]
        public string Email { get; set; }
    
        [Required(ErrorMessage = "you have to enter your password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}