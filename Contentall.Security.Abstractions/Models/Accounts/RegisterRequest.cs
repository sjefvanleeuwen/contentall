namespace Contentall.Security.Abstractions.Models.Accounts;

using Contentall.Security.Abstractions.Helpers;
using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    [Email]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }

    [Required]
    public string CaptchaTransactionKey { get; set; }

    [Required]
    public string Captcha { get; set; }


    [Range(typeof(bool), "true", "true")]
    public bool AcceptTerms { get; set; }
}