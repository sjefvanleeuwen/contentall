using System.Diagnostics;

namespace Contentall.Api.Core
{
    public class ApiResultResponse : IDisposable
    {
        public ApiResult ApiResult { get; set; } = new ApiResult();
        public void Dispose()
        {
            ApiResult.sw.Stop();
        }
    }

    public class ApiResult
    {
        internal Stopwatch sw = new Stopwatch();
        public string? UiDisplayMessage { get; set; }
        public TimeSpan? ProcessingTime { get { return sw.Elapsed; } }

        public ApiResult()
        {
            sw.Start();
        }
    }
}
