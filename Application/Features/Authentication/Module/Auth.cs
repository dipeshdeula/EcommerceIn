using Application.Dto;
using Application.Dto.AuthDTOs;
using Application.Dto.AuthDTOs.GoogleAuthDTOs;
using Application.Features.Authentication.Commands;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Otp.Commands;
using Application.Features.Authentication.Queries.Login;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;


namespace Application.Features.Authentication.Module
{
    public class Auth : CarterModule
    {
        public Auth() : base("")
        {
            WithTags("Authentication");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("auth");

            app.MapPost("/register", async ([FromServices] ISender mediator, RegisterUserDTO regUserDto) =>
            {
                var command = new RegisterCommand(regUserDto);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest("User registration failed");

                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/verify-otp", async ([FromBody] VerifyOtpCommand command, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });

            });

            app.MapPost("/login", async ([FromBody] LoginQuery query, [FromServices] ISender mediator) =>
            {
                return await mediator.Send(query);
            });

            // Test google auth for single client id
           /* app.MapPost("/google-auth", async ([FromBody] VerifyGoogleTokenCommand command, ISender mediator) =>
            {
                return await mediator.Send(command);
            });*/


            // Google OAuth login endpoint
            app.MapPost("/google-auth-login", async (
                [FromBody] GoogleLoginRequest request,
                ISender mediator) =>
            {
                if (string.IsNullOrEmpty(request.IdToken))
                {
                    return Results.BadRequest(new { message = "ID token is required" });
                }

                var command = new GoogleAuthCommand(request.IdToken, request.ClientType);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        message = result.Message,
                        errors = result.Errors
                    });
                }

                //  Set secure HTTP-only cookies for web clients
                if (request.ClientType == "web")
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true, // HTTPS only in production
                        SameSite = SameSiteMode.Lax, // Allow for OAuth redirects
                        Expires = result.Data.ExpiresAt
                    };

                    var response = Results.Ok(new
                    {
                        message = result.Message,
                        user = result.Data.User,
                        isNewUser = result.Data.IsNewUser
                    });

                    // Add cookies to response
                    return Results.Ok(result.Data);
                }

                return Results.Ok(result.Data);
            })
            .WithName("GoogleLogin")
            .WithSummary("Authenticate user with Google OAuth");


            // Get Google auth configuration
            app.MapGet("/config", (IConfiguration configuration) =>
            {
                var config = new
                {
                    webClientId = configuration["GoogleAuth:WebClientId"],
                    supportedPlatforms = new[] { "web", "android", "ios" }
                };

                return Results.Ok(config);
            })
            .WithName("GoogleAuthConfig")
            .WithSummary("Get Google OAuth configuration");     
        

            app.MapPost("/refresh-token", async (
                  [FromBody] TokenRequestDto tokenRequest,
                  [FromServices] ITokenService tokenService
                 ) =>
            {
                var result = await tokenService.RefreshTokenAsync(tokenRequest);

                if (!result.Success)
                {
                    return Results.BadRequest(new { result.Message });
                }

                return Results.Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresIn = result.ExpiresIn
                });
            });

            app.MapPost("/forgot-password", async ([FromBody] ForgotPasswordDTO forgotPasswordDto, ISender mediator) =>
            {
                var command = new ForgotPasswordCommand(forgotPasswordDto);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Data });
                }

                return Results.Ok(new { result.Message, result.Data });

            });


            app.MapPost("/verify-otp-reset-password", async (
                string email,
                string otp,
                [FromServices] ISender mediator) =>
            {
                var command = new VerifyForgotPasswordCommand(email, otp);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });

            });

            app.MapPost("/logout", async ([FromBody] TokenRequestDto tokenRequest, [FromServices] IRefreshTokenRepository refreshTokenRepo) =>
            {
                // Validate and invalidate the refresh token
                var token = await refreshTokenRepo.GetByTokenAsync(tokenRequest.RefreshToken);
                if (token == null)
                    return Results.BadRequest(new { Message = "Invalid refresh token." });

                token.Invalidated = true;
                await refreshTokenRepo.UpdateAsync(token);

                return Results.Ok(new { Message = "Logged out successfully." });
            });

        }

    }
}

