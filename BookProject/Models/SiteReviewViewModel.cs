using System;
using System.ComponentModel.DataAnnotations;

namespace BookProject.Models
{
public class SiteReviewViewModel
{
    public int RatingId { get; set; }
    
    [Required(ErrorMessage = "נא לבחור דירוג")]
    [Range(1, 5, ErrorMessage = "הדירוג חייב להיות בין 1 ל-5")]
    public int RatingValue { get; set; }
    
    [Required(ErrorMessage = "נא להזין ביקורת")]
    [StringLength(1000, ErrorMessage = "הביקורת יכולה להכיל עד 1000 תווים")]
    [Display(Name = "ביקורת")]
    public string Comment { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public string Username { get; set; }
}
}
