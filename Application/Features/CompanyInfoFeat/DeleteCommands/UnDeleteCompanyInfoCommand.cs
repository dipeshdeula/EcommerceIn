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
    public record UnDeleteCompanyInfoCommand (int Id): IRequest<Result<string>>;

    public class UnDeleteCompanyInfoCommandHandler : IRequestHandler<UnDeleteCompanyInfoCommand, Result<string>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public UnDeleteCompanyInfoCommandHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
        }
        public async Task<Result<string>> Handle(UnDeleteCompanyInfoCommand request, CancellationToken cancellationToken)
        {
            var companyInfo = await _companyInfoRepository.FindByIdAsync(request.Id);
            if (companyInfo == null)
            {
                return Result<string>.Failure("Company Info Id is not found");
            }

            await _companyInfoRepository.UndeleteAsync(companyInfo, cancellationToken);
            await _companyInfoRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("company info undeleted successfully");
        }
    }

}
