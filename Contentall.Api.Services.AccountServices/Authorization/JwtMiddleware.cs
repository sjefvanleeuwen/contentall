namespace Contentall.Api.Services.AccountServices.Authorization;

using Contentall.Security.Abstractions.Authorization;
using Contentall.Security.Abstractions.Entities;
using Contentall.Security.Abstractions.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Claims;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task Invoke(HttpContext context, IAccountService accounts, IJwtUtils jwtUtils)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        var accountId = jwtUtils.ValidateJwtToken(token);
        if (accountId != null)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, accountId));
           // claims.Add(new Claim(ClaimTypes.Email, "brockallen@gmail.com"));
            var id = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ExternalBearer);
            context.User = new ClaimsPrincipal(id);
         //   ClaimsPrincipal.Current.AddIdentity(id);

            // attach account to context on successful jwt validation
            context.Items["Account"] = accounts.GetById(accountId);
        }

        await _next(context);
    }
}