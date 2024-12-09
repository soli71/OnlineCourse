using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace OnlineCourse;

public sealed class CustomBearerTokenOption(IDataProtectionProvider dp) : IConfigureNamedOptions<BearerTokenOptions>
{
    private const string _primaryPurpose = "Microsoft.AspNetCore.Authentication.BearerToken";

    public void Configure(string schemeName, BearerTokenOptions options)
    {
        if (schemeName is null)
        {
            return;
        }

        var expirationTime = int.Parse(Environment.GetEnvironmentVariable("ExpireTokenTime"));
        var refreshExpirationTime = int.Parse(Environment.GetEnvironmentVariable("RefreshExpireTokenTime"));
        options.BearerTokenProtector = new TicketDataFormat(dp.CreateProtector(_primaryPurpose, schemeName, "BearerToken"));
        options.RefreshTokenProtector = new TicketDataFormat(dp.CreateProtector(_primaryPurpose, schemeName, "RefreshToken"));
        options.BearerTokenExpiration = TimeSpan.FromSeconds(expirationTime);
        options.RefreshTokenExpiration = TimeSpan.FromSeconds(refreshExpirationTime);
    }

    public void Configure(BearerTokenOptions options)
    {
        throw new NotImplementedException();
    }
}
