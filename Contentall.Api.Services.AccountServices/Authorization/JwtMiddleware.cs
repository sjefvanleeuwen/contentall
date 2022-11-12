namespace Contentall.Api.Services.AccountServices.Authorization;

using Contentall.Security.Abstractions.Authorization;
using Contentall.Security.Abstractions.Entities;
using Contentall.Security.Abstractions.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task Invoke(HttpContext context, IQueryable<Account> accounts, IJwtUtils jwtUtils)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        var accountId = jwtUtils.ValidateJwtToken(token);
        if (accountId != null)
        {
            // attach account to context on successful jwt validation
            context.Items["Account"] = accounts.First(p => p.Id == accountId);
        }

        await _next(context);
    }
}