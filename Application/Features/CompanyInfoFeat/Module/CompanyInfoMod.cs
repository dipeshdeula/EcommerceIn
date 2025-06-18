using Application.Dto.CompanyDTOs;
using Application.Features.CompanyInfoFeat.Commands;
using Application.Features.CompanyInfoFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}
