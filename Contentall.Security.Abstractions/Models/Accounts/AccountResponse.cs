using Contentall.Api.Core;

namespace Contentall.Security.Abstractions.Models.Accounts;

public class AccountResponse : ApiResultResponse
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public bool IsVerified { get; set; }
}