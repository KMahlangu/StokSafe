using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StokSafe.Models.Enums;

namespace StokSafe.Models.ViewModels;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Surname")]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    // NEW: Club and Role Selection
    [Required(ErrorMessage = "Please select a club.")]
    [Display(Name = "Select Club")]
    public int ClubId { get; set; }

    [Required(ErrorMessage = "Please select a role.")]
    [Display(Name = "Select Role")]
    public UserRole Role { get; set; }

    public IEnumerable<SelectListItem>? AvailableClubs { get; set; }
}