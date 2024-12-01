using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username required")]
        [Display(Name = "User name")]
        public string Username { get; set; }
    
        [Required(ErrorMessage = "Password required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        
        
        public string ForgotPasswordUrl { get; set; }
    }
}