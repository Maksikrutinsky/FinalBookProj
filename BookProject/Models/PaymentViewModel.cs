using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
    public class PaymentViewModel
    {
        [Required(ErrorMessage = "נדרש מספר כרטיס אשראי")]
        [RegularExpression(@"^(\d{4}\s?){4}$", ErrorMessage = "מספר כרטיס האשראי חייב להכיל 16 ספרות")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "נדרש תאריך תפוגה")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "פורמט תאריך תפוגה לא תקין (MM/YY)")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "נדרש קוד CVV")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "קוד CVV חייב להכיל 3 או 4 ספרות")]
        public string CVV { get; set; }

        public decimal TotalAmount { get; set; }
    }
}