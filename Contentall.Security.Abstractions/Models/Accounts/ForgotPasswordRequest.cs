namespace Contentall.Security.Abstractions.Models.Accounts;

using System.ComponentModel.DataAnnotations;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Origin { get; set; }
}