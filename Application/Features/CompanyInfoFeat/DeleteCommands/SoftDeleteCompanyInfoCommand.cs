using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CompanyInfoFeat.DeleteCommands
{
    public record SoftDeleteCompanyInfoCommand(int Id) : IRequest<Result<string>>;

    public class SoftDeleteCompanyInfoCommandHandler : IRequestHandler<SoftDeleteCompanyInfoCommand, Result<string>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public SoftDeleteCompanyInfoCommandHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
            
        }
        public async Task<Result<string>> Handle(SoftDeleteCompanyInfoCommand request, CancellationToken cancellationToken)
        {
            var companyInfo = await _companyInfoRepository.FindByIdAsync(request.Id);
            if (companyInfo == null)
            {
                return Result<string>.Failure("Company Info Id is not found");
            }

            await _companyInfoRepository.SoftDeleteAsync(companyInfo,cancellationToken);
            await _companyInfoRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("company info soft deleted successfully");
        }
    }

}
    

