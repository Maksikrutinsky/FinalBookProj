using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
    public class PaymentViewModel
    {
        [Required(ErrorMessage = "נדרש מספר כרטיס")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "מספר כרטיס לא תקין")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "נדרש תאריך תפוגה")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "תאריך תפוגה לא תקין (MM/YY)")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "נדרש CVV")]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "CVV לא תקין")]
        public string CVV { get; set; }

        public decimal TotalAmount { get; set; }
    }
}