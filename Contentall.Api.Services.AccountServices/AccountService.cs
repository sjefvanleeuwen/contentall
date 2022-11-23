namespace Contentall.Api.Services.AccountServices;

using BCrypt.Net;
using Contentall.Api.Services.AccountServices.Helpers;
using Contentall.Security.Abstractions.Entities;
using Contentall.Security.Abstractions.Helpers;
using Contentall.Security.Abstractions.Models.Accounts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using Contentall.Security.Abstractions.Authorization;
using AutoMapper;
using Contentall.Data.Provider.Abstractions;
using Contentall.Api.Services.EmailServices;

public class AccountService : IAccountService
{

    private static MemoryCache _captchaCache = new MemoryCache("AccountService:CaptchaCachingProvider");
    private readonly EntitiesContainer _context;
    private readonly IJwtUtils _jwtUtils;
    private readonly IMapper _mapper;
    private readonly AppSettings _appSettings;
    private readonly IEmailService _emailService;
    private readonly object _mutex = new object();
    public AccountService(
        EntitiesContainer context,
        IJwtUtils jwtUtils,
        IMapper mapper,
        IOptions<AppSettings> appSettings,
        IEmailService emailService)
    {
        _context = context;
        _jwtUtils = jwtUtils;
        _mapper = mapper;
        _appSettings = appSettings.Value;
        _emailService = emailService;
    }

    public CaptchaGenerateResponse GenerateCaptcha()
    {
        using var response = new CaptchaGenerateResponse();
        var captchaImage = new CaptchaImageGenerator(new CaptchaImageOptions());
        var imageKey = CaptchaImageExtensions.GetUniqueKey(8);
        var transactionKey = CaptchaImageExtensions.GetUniqueKey(8);
        var image = captchaImage.Generate(imageKey);
        var base64 = $"data:image/png;base64,{Convert.ToBase64String(image)}";
        response.Image = base64;
        response.TransactionKey = transactionKey;
        lock (_mutex)
        {
            _captchaCache.Add(transactionKey, imageKey, DateTimeOffset.Now.AddMinutes(30));
        }
        return response;
    }

    public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
    {
        var account = _context.GetQueryableEntities<Account>().SingleOrDefault(x => x.Email == model.Email);

        // validate
        if (account == null || !account.IsVerified || !BCrypt.Verify(model.Password, account.PasswordHash))
            throw new AppException("Email or password is incorrect");

        // authentication successful so generate jwt and refresh tokens
        var jwtToken = _jwtUtils.GenerateJwtToken(account);
        var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
        account.RefreshTokens.Add(refreshToken);

        // remove old refresh tokens from account
        removeOldRefreshTokens(account);

        // save changes to db
        _context.Update(account);

        var response = _mapper.Map<AuthenticateResponse>(account);
        response.JwtToken = jwtToken;
        response.RefreshToken = refreshToken.Token;
        return response;
    }

    public AuthenticateResponse RefreshToken(string token, string ipAddress)
    {
        var account = getAccountByRefreshToken(token);
        var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

        if (refreshToken.IsRevoked)
        {
            // revoke all descendant tokens in case this token has been compromised
            revokeDescendantRefreshTokens(refreshToken, account, ipAddress, $"Attempted reuse of revoked ancestor token: {token}");
            _context.Update(account);
        }

        if (!refreshToken.IsActive)
            throw new AppException("Invalid token");

        // replace old refresh token with a new one (rotate token)
        var newRefreshToken = rotateRefreshToken(refreshToken, ipAddress);
        account.RefreshTokens.Add(newRefreshToken);

        // remove old refresh tokens from account
        removeOldRefreshTokens(account);

        // save changes to db
        _context.Update(account);

        // generate new jwt
        var jwtToken = _jwtUtils.GenerateJwtToken(account);

        // return data in authenticate response object
        var response = _mapper.Map<AuthenticateResponse>(account);
        response.JwtToken = jwtToken;
        response.RefreshToken = newRefreshToken.Token;
        return response;
    }

    public void RevokeToken(string token, string ipAddress)
    {
        var account = getAccountByRefreshToken(token);
        var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

        if (!refreshToken.IsActive)
            throw new AppException("Invalid token");

        // revoke token and save
        revokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
        _context.Update(account);
    }

    public void Register(RegisterRequest model, string origin, bool validCaptchaMasterkey = false)
    {
        if (!validCaptchaMasterkey)
        {
            var s = _captchaCache[model.CaptchaTransactionKey];
            if (s == null || s.ToString().ToLower() != model.Captcha.ToLower())
            {
                throw new Exception("Invalid Captcha");
            }
        }
        // validate
        if (_context.GetQueryableEntities<Account>().Any(x => x.Email == model.Email))
        {
            // send already registered error in email to prevent account enumeration
            sendAlreadyRegisteredEmail(model.Email, origin);
            return;
        }

        // map model to new account object
        var account = _mapper.Map<Account>(model);

        // first registered account is an admin
        var isFirstAccount = _context.GetQueryableEntities<Account>().Count() == 0;
        account.Role = isFirstAccount ? Role.Admin : Role.User;
        account.Created = DateTime.UtcNow;
        account.VerificationToken = generateVerificationToken();

        // hash password
        account.PasswordHash = BCrypt.HashPassword(model.Password);

        // save account
        _context.Insert(account);
        _context.EnsureIndex<Account>(p => p.Email);
        _context.EnsureIndex<Account>(p => p.PasswordHash);

        // send email
        sendVerificationEmail(account, origin);
    }

    public void VerifyEmail(string token)
    {
        var account = _context.GetQueryableEntities<Account>().SingleOrDefault(x => x.VerificationToken == token);

        if (account == null)
            throw new AppException("Verification failed");

        account.Verified = DateTime.UtcNow;
        account.VerificationToken = null;

        _context.Update(account);
    }

    public void ForgotPassword(ForgotPasswordRequest model)
    {
        var account = _context.GetQueryableEntities<Account>().SingleOrDefault(x => x.Email == model.Email);

        // always return ok response to prevent email enumeration
        if (account == null) return;

        // create reset token that expires after 1 day
        account.ResetToken = generateResetToken();
        account.ResetTokenExpires = DateTime.UtcNow.AddDays(1);

        _context.Update(account);

        // send email
        sendPasswordResetEmail(account, model.Origin);
    }

    public void ValidateResetToken(ValidateResetTokenRequest model)
    {
        getAccountByResetToken(model.Token);
    }

    public void ResetPassword(ResetPasswordRequest model)
    {
        var account = getAccountByResetToken(model.Token);

        // update password and remove reset token
        account.PasswordHash = BCrypt.HashPassword(model.Password);
        account.PasswordReset = DateTime.UtcNow;
        account.ResetToken = null;
        account.ResetTokenExpires = null;

        _context.Update(account);
    }

    public IEnumerable<AccountResponse> GetAll()
    {
        var accounts = _context.GetQueryableEntities<Account>();
        return _mapper.Map<IList<AccountResponse>>(accounts);
    }

    public AccountResponse GetById(string id)
    {
        var account = getAccount(id);
        return _mapper.Map<AccountResponse>(account);
    }

    public AccountResponse Create(CreateRequest model)
    {
        // validate
        if (_context.GetQueryableEntities<Account>().Any(x => x.Email == model.Email))
            throw new AppException($"Email '{model.Email}' is already registered");

        // map model to new account object
        var account = _mapper.Map<Account>(model);
        account.Created = DateTime.UtcNow;
        account.Verified = DateTime.UtcNow;

        // hash password
        account.PasswordHash = BCrypt.HashPassword(model.Password);

        // save account
        _context.Insert(account);

        return _mapper.Map<AccountResponse>(account);
    }

    public AccountResponse Update(string id, UpdateRequest model)
    {
        var account = getAccount(id);

        // validate
        if (account.Email != model.Email && _context.GetQueryableEntities<Account>().Any(x => x.Email == model.Email))
            throw new AppException($"Email '{model.Email}' is already registered");

        // hash password if it was entered
        if (!string.IsNullOrEmpty(model.Password))
            account.PasswordHash = BCrypt.HashPassword(model.Password);

        // copy model to account and save
        _mapper.Map(model, account);
        account.Updated = DateTime.UtcNow;
        _context.Update(account);

        return _mapper.Map<AccountResponse>(account);
    }

    public void Delete(string id)
    {
        var account = getAccount(id);
        _context.Delete<Account>(id.ToString());
    }

    // helper methods

    private Account getAccount(string id)
    {
        var account = _context.GetQueryableEntities<Account>().Where(p=>p.Id == id.ToString()).SingleOrDefault();
        if (account == null) throw new KeyNotFoundException("Account not found");
        return account;
    }

    private Account getAccountByRefreshToken(string token)
    {
        var account = _context.GetQueryableEntities<Account>().SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
        if (account == null) throw new AppException("Invalid token");
        return account;
    }

    private Account getAccountByResetToken(string token)
    {
        var account = _context.GetQueryableEntities<Account>().SingleOrDefault(x =>
            x.ResetToken == token && x.ResetTokenExpires > DateTime.UtcNow);
        if (account == null) throw new AppException("Invalid token");
        return account;
    }

    private string generateJwtToken(Account account)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string generateResetToken()
    {
        // token is a cryptographically strong random sequence of values
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        // ensure token is unique by checking against db
        var tokenIsUnique = !_context.GetQueryableEntities<Account>().Any(x => x.ResetToken == token);
        if (!tokenIsUnique)
            return generateResetToken();

        return token;
    }

    private string generateVerificationToken()
    {
        // token is a cryptographically strong random sequence of values
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        // ensure token is unique by checking against db
        var tokenIsUnique = !_context.GetQueryableEntities<Account>().Any(x => x.VerificationToken == token);
        if (!tokenIsUnique)
            return generateVerificationToken();

        return token;
    }

    private RefreshToken rotateRefreshToken(RefreshToken refreshToken, string ipAddress)
    {
        var newRefreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
        revokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
        return newRefreshToken;
    }

    private void removeOldRefreshTokens(Account account)
    {
        account.RefreshTokens.RemoveAll(x =>
            !x.IsActive &&
            x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
    }

    private void revokeDescendantRefreshTokens(RefreshToken refreshToken, Account account, string ipAddress, string reason)
    {
        // recursively traverse the refresh token chain and ensure all descendants are revoked
        if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
        {
            var childToken = account.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);
            if (childToken.IsActive)
                revokeRefreshToken(childToken, ipAddress, reason);
            else
                revokeDescendantRefreshTokens(childToken, account, ipAddress, reason);
        }
    }

    private void revokeRefreshToken(RefreshToken token, string ipAddress, string reason = null, string replacedByToken = null)
    {
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;
        token.ReplacedByToken = replacedByToken;
    }

    private void sendVerificationEmail(Account account, string origin)
    {
        string message;
        if (!string.IsNullOrEmpty(origin))
        {
            // origin exists if request sent from browser single page app (e.g. Angular or React)
            // so send link to verify via single page app
            var verifyUrl = $"{origin}/account/verify-email?token={account.VerificationToken}";
            message = $@"<p>Please click the below link to verify your email address:</p>
                            <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
        }
        else
        {
            // origin missing if request sent directly to api (e.g. from Postman)
            // so send instructions to verify directly with api
            message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                            <p><code>{account.VerificationToken}</code></p>";
        }

        _emailService.Send(
            to: account.Email,
            subject: "Sign-up Verification API - Verify Email",
            html: $@"<h4>Verify Email</h4>
                        <p>Thanks for registering!</p>
                        {message}"
        );
    }

    private void sendAlreadyRegisteredEmail(string email, string origin)
    {
        string message;
        if (!string.IsNullOrEmpty(origin))
            message = $@"<p>If you don't know your password please visit the <a href=""{origin}/account/forgot-password"">forgot password</a> page.</p>";
        else
            message = "<p>If you don't know your password you can reset it via the <code>/accounts/forgot-password</code> api route.</p>";

        _emailService.Send(
            to: email,
            subject: "Sign-up Verification API - Email Already Registered",
            html: $@"<h4>Email Already Registered</h4>
                        <p>Your email <strong>{email}</strong> is already registered.</p>
                        {message}"
        );
    }

    private void sendPasswordResetEmail(Account account, string origin)
    {
        string message;
        if (!string.IsNullOrEmpty(origin))
        {
            var resetUrl = $"{origin}/account/reset-password?token={account.ResetToken}";
            message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                            <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
        }
        else
        {
            message = $@"<p>Please use the below token to reset your password with the <code>/accounts/reset-password</code> api route:</p>
                            <p><code>{account.ResetToken}</code></p>";
        }

        _emailService.Send(
            to: account.Email,
            subject: "Sign-up Verification API - Reset Password",
            html: $@"<h4>Reset Password Email</h4>
                        {message}"
        );
    }
}