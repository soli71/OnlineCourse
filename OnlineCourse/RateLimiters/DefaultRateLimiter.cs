using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace OnlineCourse.RateLimiters
{
    public class DefaultRateLimiter : IRateLimiterPolicy<string>
    {
        public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => throw new NotImplementedException();

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            return RateLimitPartition.GetTokenBucketLimiter(string.Empty,
             _ => new TokenBucketRateLimiterOptions
             {
                 TokenLimit=1000,
                 AutoReplenishment = true,
                 ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                 TokensPerPeriod=10,
                 QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                 QueueLimit=2,
             });
        }
    }
}
