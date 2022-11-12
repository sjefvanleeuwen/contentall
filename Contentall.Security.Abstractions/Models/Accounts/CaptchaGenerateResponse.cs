using Contentall.Api.Core;

namespace Contentall.Security.Abstractions.Models.Accounts
{
    public class CaptchaGenerateResponse : ApiResultResponse
    {
        public string Image { get; set; }
        public string TransactionKey { get; set; }
    }
}
