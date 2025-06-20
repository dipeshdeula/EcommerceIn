using Application.Dto.CompanyDTOs;
using Application.Dto.ProductDTOs;
using Application.Features.CompanyInfoFeat.Commands;
using Application.Features.CompanyInfoFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.CompanyInfoFeat.Module
{
    public class CompanyInfoMod : CarterModule
    {
        public CompanyInfoMod() : base("")
        {
            WithTags("CompanyInfo");
            IncludeInOpenApi();
            
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("");

            app.MapPost("/createCompanyInfo", async (AddCompanyInfoDTO addCompanyInfo, ISender mediator) =>
            {
                var command = new CreateCompanyInfoCommand(addCompanyInfo);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapGet("/getAllCompanyInfo", async (ISender mediator,int PageNumber = 1,int PageSize = 10) =>
            {
                var command = new GetAllCompanyInfoQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapGet("/getCompanyInfoById", async (ISender mediator, int Id) =>
            {
                var command = new GetCompanyInfoByIdQuery(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapPost("/uploadCompanyLogo", async (ISender mediator, int Id,IFormFile file) =>
            {
                var command = new UploadCompanyLogoCommand(Id,file);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            }).DisableAntiforgery()
            .Accepts<UploadCompanyLogoCommand>("multipart/form-data")
            .Produces<IEnumerable<ProductImageDTO>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapPut("/updateCompanyInfo", async (ISender mediator, int Id, UpdateCompanyInfoDTO updateCompanyInfoDto) =>
            {
                var command = new UpdateCompanyInfoCommand(Id, updateCompanyInfoDto);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapDelete("/hardDelete", async (ISender mediator, int Id) =>
            {
                var command = new HardDeleteCompanyInfoCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            });
        }
    }
}
