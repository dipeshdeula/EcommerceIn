using Application.Common;
using Application.Dto.CompanyDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CompanyInfoFeat.Queries
{
    public record GetAllCompanyInfoQuery (int PageNumber, int PageSize) : IRequest<Result<IEnumerable<CompanyInfoDTO>>>;

    public class GetAllCompanyInfoQueryHandler : IRequestHandler<GetAllCompanyInfoQuery, Result<IEnumerable<CompanyInfoDTO>>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public GetAllCompanyInfoQueryHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
        }
        public async Task<Result<IEnumerable<CompanyInfoDTO>>> Handle(GetAllCompanyInfoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var companyInfo = await _companyInfoRepository.GetAllAsync(
                      orderBy: query => query.OrderBy(c => c.Id),
                      includeDeleted: false,
                      skip: ((request.PageNumber - 1) * request.PageSize),
                      take: request.PageSize,
                      cancellationToken: cancellationToken
                      );

                var companyInfoList = companyInfo.Select(c => c.ToDTO()).ToList();

                return Result<IEnumerable<CompanyInfoDTO>>.Success(companyInfoList, "Comany Info fetched successfully");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<CompanyInfoDTO>>.Failure("Failed to retrieve company Info:", ex.Message);
            }
        }
    }

}
