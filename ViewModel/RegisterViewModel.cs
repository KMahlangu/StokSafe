using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StokSafe.Models.Enums;

namespace StokSafe.ViewModel;

public class RegisterViewModel
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Select Club")]
    public int ClubId { get; set; }

    [Required]
    [Display(Name = "Club Role")]
    public UserRole Role { get; set; } = UserRole.Member;

    public IEnumerable<SelectListItem>? Clubs { get; set; }
}