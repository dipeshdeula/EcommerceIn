using Application.Dto;
using Application.Dto.AuthDTOs;
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

            app.MapPost("/google-auth", async ([FromBody] VerifyGoogleTokenCommand command, ISender mediator) =>
            {
                return await mediator.Send(command);
            });

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

