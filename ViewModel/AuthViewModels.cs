using System.ComponentModel.DataAnnotations;

namespace StokSafe.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "First Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Surname")]
    public string Surname { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(13, MinimumLength = 13)]
    [Display(Name = "ID Number")]
    public string IdNumber { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}