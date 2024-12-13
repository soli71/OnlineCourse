using System.Threading.RateLimiting;

public class GlobalLimiterOptions
{
    public bool Enabled { get; set; }
    public GlobalFixedWindowLimiterOptions GlobalFixedWindowLimiterOptions { get; set; }
    public GlobalTokenBucketLimiterOptions GlobalTokenBucketLimiterOptions { get; set; }
    public GlobalConcurrencyLimiterOptions GlobalConcurrencyLimiterOptions { get; set; }

}
public class GlobalFixedWindowLimiterOptions
{
    public bool Enabled { get; set; }
    public FixedWindowRateLimiterOptions FixedWindowRateLimiterOptions { get; set; }
}
public class GlobalTokenBucketLimiterOptions
{
    public bool Enabled { get; set; }
    public TokenBucketRateLimiterOptions TokenBucketRateLimiterOptions { get; set; }
}
public class GlobalConcurrencyLimiterOptions
{
    public bool Enabled { get; set; }
    public ConcurrencyLimiterOptions ConcurrencyLimiterOptions { get; set; }
}
