using Application.Common;
using Application.Dto.CompanyDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.CompanyInfoFeat.Queries
{
    public record GetCompanyInfoByIdQuery(int Id) : IRequest<Result<CompanyInfoDTO>>;

    public class GetCompanyInfoByIdQueryHandler : IRequestHandler<GetCompanyInfoByIdQuery, Result<CompanyInfoDTO>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public GetCompanyInfoByIdQueryHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
        }
        public async Task<Result<CompanyInfoDTO>> Handle(GetCompanyInfoByIdQuery request, CancellationToken cancellationToken)
        {
            var companyInfo = await _companyInfoRepository.FindByIdAsync(request.Id);

            if (companyInfo == null)
            {
                return Result<CompanyInfoDTO>.Failure("Company id not found");
            }

            return Result<CompanyInfoDTO>.Success(companyInfo.ToDTO(), "CompanyInfo retrieve successfully");
        }
    }
}
