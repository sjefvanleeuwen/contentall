namespace Contentall.Api.Services.AccountServices;
using Contentall.Security.Abstractions.Models.Accounts;

public interface IAccountService
{
    AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
    AuthenticateResponse RefreshToken(string token, string ipAddress);
    void RevokeToken(string token, string ipAddress);
    void Register(RegisterRequest model, string origin, bool isValidCaptchaMasterKey);
    void VerifyEmail(string token);
    void ForgotPassword(ForgotPasswordRequest model);
    void ValidateResetToken(ValidateResetTokenRequest model);
    void ResetPassword(ResetPasswordRequest model);
    IEnumerable<AccountResponse> GetAll();
    AccountResponse GetById(string id);
    AccountResponse Create(CreateRequest model);
    AccountResponse Update(string id, UpdateRequest model);
    void Delete(string id);
    CaptchaGenerateResponse GenerateCaptcha();
}
