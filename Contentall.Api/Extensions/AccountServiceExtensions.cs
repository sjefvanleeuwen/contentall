using Contentall.Api.Services.AccountServices;
using Contentall.Api.Services.AccountServices.Authorization;
using Contentall.Security.Abstractions.Authorization;
using Contentall.Security.Abstractions.Entities;
using Contentall.Security.Abstractions.Helpers;
using Contentall.Security.Abstractions.Models.Accounts;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Contentall.Api.Extensions
{
    [ExtendObjectType(typeof(Mutation))]
    public class AccountServiceExtensions
    {
        private readonly IAccountService accountService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IOptions<AppSettings> appSettings;

        public AccountServiceExtensions(IAccountService accountService, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings)
        {
            this.accountService = accountService;
            this.httpContextAccessor = httpContextAccessor;
            this.appSettings = appSettings;
        }

        [AllowAnonymous]
        public async Task<VerifyEmailResponse> VerifyEmail(string token)
        {
            accountService.VerifyEmail(token);
            return new VerifyEmailResponse() { ValidToken = true };
        }

        [Authorize(Role.User)]
        public async Task<CaptchaGenerateResponse> GenerateCaptcha()
        {
           return accountService.GenerateCaptcha();
        }

        public ResetPasswordResponse ResetPassword(ResetPasswordRequest model)
        {
            ResetPasswordResponse response = new ResetPasswordResponse();
            accountService.ResetPassword(model);
            return response;
        }

        public ForgotPasswordResponse ForgotPassword(ForgotPasswordRequest model)
        {
            ForgotPasswordResponse response = new ForgotPasswordResponse();
            accountService.ForgotPassword(model);
            return response;
        }

        [AllowAnonymous]
        public AuthenticateResponse Authenticate(AuthenticateRequest model)
        {
            var response = accountService.Authenticate(model, ipAddress());
            return response;
        }

        [AllowAnonymous]
        public RegisterResponse Register(RegisterRequest model)
        {
            accountService.Register(model, httpContextAccessor.HttpContext.Request.Headers["origin"], appSettings.Value.CaptchaMasterKey == model.Captcha);
            return new RegisterResponse() {
                ApiResult = new Core.ApiResult() { 
                    UiDisplayMessage = "Registration successful, please check your email for verification instructions" 
                } 
            };
        }

        // helper methods
        private string ipAddress()
        {
            if (httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                return httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
            else
                return httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
