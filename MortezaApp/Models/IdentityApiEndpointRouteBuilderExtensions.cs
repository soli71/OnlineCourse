using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MortezaApp.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace OnlineCourse.Models;

public static partial class IdentityApiEndpointRouteBuilderExtensions
{
    // Validate the email address using DataAnnotations like the UserValidator does when RequireUniqueEmail = true.
    private static readonly EmailAddressAttribute _emailAddressAttribute = new();

    /// <summary>
    /// Add endpoints for registering, logging in, and logging out using ASP.NET Core Identity.
    /// </summary>
    /// <typeparam name="TUser">The type describing the user. This should match the generic parameter in <see cref="UserManager{TUser}"/>.</typeparam>
    /// <param name="endpoints">
    /// The <see cref="IEndpointRouteBuilder"/> to add the identity endpoints to.
    /// Call <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, string)"/> to add a prefix to all the endpoints.
    /// </param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> to further customize the added endpoints.</returns>
    public static IEndpointConventionBuilder MapMyIdentityApi<TUser>(this IEndpointRouteBuilder endpoints)
        where TUser : User, new()
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var timeProvider = endpoints.ServiceProvider.GetRequiredService<TimeProvider>();
        var bearerTokenOptions = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
        var emailSender = endpoints.ServiceProvider.GetRequiredService<IEmailSender<TUser>>();
        var linkGenerator = endpoints.ServiceProvider.GetRequiredService<LinkGenerator>();

        // We'll figure out a unique endpoint name based on the final route pattern during endpoint generation.
        string confirmEmailEndpointName = null;

        var routeGroup = endpoints.MapGroup("account");

        routeGroup.MapPost("/register", async Task<Results<Ok, ValidationProblem>>
           ([FromBody] RegisterRequest registration, HttpContext context, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            var smsService = sp.GetRequiredService<ISmsService>();
            if (!userManager.SupportsUserEmail)
            {
                throw new NotSupportedException($"{nameof(MapMyIdentityApi)} requires a user store with email support.");
            }

            var userStore = sp.GetRequiredService<IUserStore<TUser>>();
            var emailStore = (IUserEmailStore<TUser>)userStore;
            var phoneNumber = registration.PhoneNumber;

            if (string.IsNullOrEmpty(registration.PhoneNumber))
            {
                return CreateValidationProblem(IdentityResult.Failed(new IdentityError { Description = "شماره موبایل اشتباه می باشد" }));
            }

            var user = new TUser();
            await userStore.SetUserNameAsync(user, phoneNumber, CancellationToken.None);
            user.PhoneNumber = phoneNumber;
            var result = await userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                return CreateValidationProblem(result);
            }

            await SendConfirmationMobileAsync(user, userManager, smsService, phoneNumber, sp.GetRequiredService<IMemoryCache>());
            return TypedResults.Ok();
        });

        routeGroup.MapPost("/login", async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>>
            ([FromBody] LoginRequest login, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();

            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await signInManager.PasswordSignInAsync(login.PhoneNumber, login.Password, true, lockoutOnFailure: true);

            if (result.IsNotAllowed)
            {
                return TypedResults.Problem("لطفا موبایل خود را تایید نمایید", statusCode: StatusCodes.Status405MethodNotAllowed);
            }
            if (!result.Succeeded)
            {
                return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
            }

            // The signInManager already produced the needed response in the form of a cookie or bearer token.
            return TypedResults.Empty;
        });

        routeGroup.MapGet("/me", Results<Ok<MeModel>, EmptyHttpResult, ProblemHttpResult>
            ([FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor.HttpContext;

            var user = httpContext.User.Identity.Name;
            return TypedResults.Ok(new MeModel
            {
                UserName = user,
            });
        });

        routeGroup.MapPost("/refresh", async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
            ([FromBody] RefreshRequest refreshRequest, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

            // Reject the /refresh attempt with a 401 if the token expired or the security stamp validation fails
            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
                timeProvider.GetUtcNow() >= expiresUtc ||
                await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not TUser user)

            {
                return TypedResults.Challenge();
            }

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        });

        routeGroup.MapPost("/confirm-phone-number", async Task<Results<ContentHttpResult, UnauthorizedHttpResult>>
            ([FromBody] ConfirmPhoneNumberRequest request, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            var user = await userManager.Users.FirstOrDefaultAsync(c => c.UserName == request.PhoneNumber);
            if (user is not { } user1)
            {
                // We could respond with a 404 instead of a 401 like Identity UI, but that feels like unnecessary information.
                return TypedResults.Unauthorized();
            }
            if (await userManager.IsPhoneNumberConfirmedAsync(user1))
            {
                return TypedResults.Text("Your phone number is already confirmed.");
            }

            var memoryCache = sp.GetRequiredService<IMemoryCache>();

            memoryCache.TryGetValue($"{user.Id}-VerificationCode", out VerificationStoreModel cacheCode);

            if (cacheCode == null || cacheCode.VerificationCode.ToString() != request.Code
            || request.PhoneNumber != cacheCode.PhoneNumber)
            {
                return TypedResults.Unauthorized();
            }
            user.PhoneNumberConfirmed = true;

            await userManager.UpdateAsync(user);

            memoryCache.Remove($"{user.Id}-VerificationCode");

            return TypedResults.Text("Thank you for confirming your phone number.");
        });

        routeGroup.MapGet("/phone/send-verification-code", async Task<Results<Ok<SendVerificationCodeResponse>, NotFound, Ok>>
            ([FromQuery] string phoneNumber, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            var user = await userManager.FindByNameAsync(phoneNumber);

            if (user is not null && await userManager.IsPhoneNumberConfirmedAsync(user))
            {
                var memoryCache = sp.GetRequiredService<IMemoryCache>();

                var getVerificationCode = memoryCache.Get<VerificationStoreModel>($"{user.Id}-VerificationCode");
                if (getVerificationCode != null)
                {
                    if (getVerificationCode.ExpireTime > DateTime.UtcNow)
                    {
                        return TypedResults.Ok(new SendVerificationCodeResponse { TimeToExpire = (int)(getVerificationCode.ExpireTime - DateTime.UtcNow).TotalSeconds });
                    }
                }
                string code = GenerateVerificationCode();

                memoryCache.Set($"{user.Id}-VerificationCode", new VerificationStoreModel { PhoneNumber = user.PhoneNumber, VerificationCode = code, ExpireTime = DateTime.UtcNow.AddSeconds(120) }, TimeSpan.FromSeconds(120));
                // Send the verification code to the user
                // ...

                return TypedResults.Ok(new SendVerificationCodeResponse { TimeToExpire = 120 });
            }
            else
            {
                return TypedResults.Ok();
            }
        });

        routeGroup.MapPost("/forgotPassword", async Task<Results<Ok<SendVerificationCodeResponse>, ValidationProblem>>
            ([FromBody] ForgotPasswordRequest resetRequest, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            var user = await userManager.FindByNameAsync(resetRequest.PhoneNumber);

            if (user is not null && await userManager.IsPhoneNumberConfirmedAsync(user))
            {
                var memoryCache = sp.GetRequiredService<IMemoryCache>();

                var getVerificationCode = memoryCache.Get<VerificationStoreModel>($"{user.Id}-forgetPassword");
                if (getVerificationCode != null)
                {
                    if (getVerificationCode.ExpireTime > DateTime.UtcNow)
                    {
                        return TypedResults.Ok(new SendVerificationCodeResponse { TimeToExpire = (int)(getVerificationCode.ExpireTime - DateTime.UtcNow).TotalSeconds });
                    }
                }

                string code = GenerateVerificationCode();
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                memoryCache.Set($"{user.Id}-forgetPassword", new VerificationStoreModel { PhoneNumber = user.PhoneNumber, VerificationCode = code, Token = token, ExpireTime = DateTime.UtcNow.AddSeconds(120) }, TimeSpan.FromMinutes(2));
                // Send the verification code to the user
                // ...
                return TypedResults.Ok(new SendVerificationCodeResponse { TimeToExpire = 120 });
            }
            return CreateValidationProblem("InvalidPhoneNumber", "The phone number provided does not match any user in the system.");
        });

        routeGroup.MapPost("/resetPassword", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] ResetPasswordRequest resetRequest, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();

            var user = await userManager.FindByNameAsync(resetRequest.PhoneNumber);

            if (user is null || !await userManager.IsPhoneNumberConfirmedAsync(user))
            {
                // Don't reveal that the user does not exist or is not confirmed, so don't return a 200 if we would have
                // returned a 400 for an invalid code given a valid user email.
                return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken()));
            }

            IdentityResult result;
            try
            {
                sp.GetRequiredService<IMemoryCache>().TryGetValue($"{user.Id}-forgetPassword", out VerificationStoreModel verificationStoreModel);

                if (verificationStoreModel == null || verificationStoreModel.VerificationCode != resetRequest.ResetCode || verificationStoreModel.PhoneNumber != resetRequest.PhoneNumber)
                {
                    return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken()));
                }
                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(verificationStoreModel.Token));
                result = await userManager.ResetPasswordAsync(user, code, resetRequest.NewPassword);

                if (result.Succeeded)
                {
                    // Clear the verification code from memory cache
                    sp.GetRequiredService<IMemoryCache>().Remove($"{user.Id}-forgetPassword");
                }
            }
            catch (FormatException)
            {
                result = IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken());
            }

            if (!result.Succeeded)
            {
                return CreateValidationProblem(result);
            }

            return TypedResults.Ok();
        });

        var accountGroup = routeGroup.MapGroup("/manage").RequireAuthorization();

        accountGroup.MapPost("/2fa", async Task<Results<Ok<TwoFactorResponse>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromBody] TwoFactorRequest tfaRequest, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();
            var userManager = signInManager.UserManager;
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                return TypedResults.NotFound();
            }

            if (tfaRequest.Enable == true)
            {
                if (tfaRequest.ResetSharedKey)
                {
                    return CreateValidationProblem("CannotResetSharedKeyAndEnable",
                        "Resetting the 2fa shared key must disable 2fa until a 2fa token based on the new shared key is validated.");
                }
                else if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
                {
                    return CreateValidationProblem("RequiresTwoFactor",
                        "No 2fa token was provided by the request. A valid 2fa token is required to enable 2fa.");
                }
                else if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode))
                {
                    return CreateValidationProblem("InvalidTwoFactorCode",
                        "The 2fa token provided by the request was invalid. A valid 2fa token is required to enable 2fa.");
                }

                await userManager.SetTwoFactorEnabledAsync(user, true);
            }
            else if (tfaRequest.Enable == false || tfaRequest.ResetSharedKey)
            {
                await userManager.SetTwoFactorEnabledAsync(user, false);
            }

            if (tfaRequest.ResetSharedKey)
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
            }

            string[] recoveryCodes = null;
            if (tfaRequest.ResetRecoveryCodes || tfaRequest.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                recoveryCodes = recoveryCodesEnumerable?.ToArray();
            }

            if (tfaRequest.ForgetMachine)
            {
                await signInManager.ForgetTwoFactorClientAsync();
            }

            var key = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                key = await userManager.GetAuthenticatorKeyAsync(user);

                if (string.IsNullOrEmpty(key))
                {
                    throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
                }
            }

            return TypedResults.Ok(new TwoFactorResponse
            {
                SharedKey = key,
                RecoveryCodes = recoveryCodes,
                RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
                IsTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
                IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
            });
        });

        accountGroup.MapGet("/info", async Task<Results<Ok<InfoResponse>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(await CreateInfoResponseAsync(user, userManager));
        });

        accountGroup.MapPost("/info", async Task<Results<Ok<InfoResponse>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromBody] InfoRequest infoRequest, HttpContext context, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                return TypedResults.NotFound();
            }

            if (!string.IsNullOrEmpty(infoRequest.NewEmail) && !_emailAddressAttribute.IsValid(infoRequest.NewEmail))
            {
                return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(infoRequest.NewEmail)));
            }

            if (!string.IsNullOrEmpty(infoRequest.NewPassword))
            {
                if (string.IsNullOrEmpty(infoRequest.OldPassword))
                {
                    return CreateValidationProblem("OldPasswordRequired",
                        "The old password is required to set a new password. If the old password is forgotten, use /resetPassword.");
                }

                var changePasswordResult = await userManager.ChangePasswordAsync(user, infoRequest.OldPassword, infoRequest.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    return CreateValidationProblem(changePasswordResult);
                }
            }

            if (!string.IsNullOrEmpty(infoRequest.NewEmail))
            {
                var email = await userManager.GetEmailAsync(user);

                if (email != infoRequest.NewEmail)
                {
                    await SendConfirmationEmailAsync(user, userManager, context, infoRequest.NewEmail, isChange: true);
                }
            }

            return TypedResults.Ok(await CreateInfoResponseAsync(user, userManager));
        });

        async Task SendConfirmationMobileAsync(TUser user, UserManager<TUser> userManager, ISmsService smsService, string mobile, IMemoryCache memoryCache, bool isChange = false)
        {
            string code = GenerateVerificationCode();

            memoryCache.Set($"{user.Id}-VerificationCode", new VerificationStoreModel { PhoneNumber = user.PhoneNumber, ExpireTime = DateTime.UtcNow.AddSeconds(120), VerificationCode = code }, TimeSpan.FromSeconds(120));

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return;
            }
            await smsService.SendAsync(mobile, $"کد تایید شما : {code}\r\nای آی چت");
        }

        async Task SendConfirmationEmailAsync(TUser user, UserManager<TUser> userManager, HttpContext context, string email, bool isChange = false)
        {
            if (confirmEmailEndpointName is null)
            {
                throw new NotSupportedException("No email confirmation endpoint was registered!");
            }

            var code = isChange
                ? await userManager.GenerateChangeEmailTokenAsync(user, email)
                : await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var userId = await userManager.GetUserIdAsync(user);
            var routeValues = new RouteValueDictionary()
            {
                ["userId"] = userId,
                ["code"] = code,
            };

            if (isChange)
            {
                // This is validated by the /confirmEmail endpoint on change.
                routeValues.Add("changedEmail", email);
            }

            var confirmEmailUrl = linkGenerator.GetUriByName(context, confirmEmailEndpointName, routeValues)
                ?? throw new NotSupportedException($"Could not find endpoint named '{confirmEmailEndpointName}'.");

            await emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(confirmEmailUrl));
        }

        return new IdentityEndpointsConventionBuilder(routeGroup);
    }

    private static string GenerateVerificationCode()
    {
        string code;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            code = "12345";
        }
        else
        {
            code = new Random().Next(10000, 99999).ToString();
        }

        return code;
    }

    private static ValidationProblem CreateValidationProblem(string errorCode, string errorDescription) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> {
            { errorCode, [errorDescription] }
        });

    private static ValidationProblem CreateValidationProblem(IdentityResult result)
    {
        // We expect a single error code and description in the normal case.
        // This could be golfed with GroupBy and ToDictionary, but perf! :P
        Debug.Assert(!result.Succeeded);
        var errorDictionary = new Dictionary<string, string[]>(1);

        foreach (var error in result.Errors)
        {
            string[] newDescriptions;

            if (errorDictionary.TryGetValue(error.Code, out var descriptions))
            {
                newDescriptions = new string[descriptions.Length + 1];
                Array.Copy(descriptions, newDescriptions, descriptions.Length);
                newDescriptions[descriptions.Length] = error.Description;
            }
            else
            {
                newDescriptions = [error.Description];
            }

            errorDictionary[error.Code] = newDescriptions;
        }

        return TypedResults.ValidationProblem(errorDictionary);
    }

    private static async Task<InfoResponse> CreateInfoResponseAsync<TUser>(TUser user, UserManager<TUser> userManager)
        where TUser : User
    {
        return new InfoResponse()
        {
            Email = await userManager.GetEmailAsync(user),
            Mobile = user.Mobile,
            IsEmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
            IsMobileConfirmed = user.PhoneNumberConfirmed
        };
    }

    // Wrap RouteGroupBuilder with a non-public type to avoid a potential future behavioral breaking change.
    private sealed class IdentityEndpointsConventionBuilder(RouteGroupBuilder inner) : IEndpointConventionBuilder
    {
        private IEndpointConventionBuilder InnerAsConventionBuilder => inner;

        public void Add(Action<EndpointBuilder> convention) => InnerAsConventionBuilder.Add(convention);

        public void Finally(Action<EndpointBuilder> finallyConvention) => InnerAsConventionBuilder.Finally(finallyConvention);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromBodyAttribute : Attribute, IFromBodyMetadata
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromServicesAttribute : Attribute, IFromServiceMetadata
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromQueryAttribute : Attribute, IFromQueryMetadata
    {
        public string Name => null;
    }

    public class MeModel
    {
        public string UserName { get; set; }
    }
}