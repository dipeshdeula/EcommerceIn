using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CompanyInfoFeat.DeleteCommands
{
    public record HardDeleteCompanyInfoCommand(int id) : IRequest<Result<string>>;

    public class HardDeleteCompanyInfoCommandHandler : IRequestHandler<HardDeleteCompanyInfoCommand, Result<string>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public HardDeleteCompanyInfoCommandHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
        }
        public async Task<Result<string>> Handle(HardDeleteCompanyInfoCommand request, CancellationToken cancellationToken)
        {
            var companyInfo = await _companyInfoRepository.FindByIdAsync(request.id);
            if (companyInfo == null)
            {
                return Result<string>.Failure("CompanyInfo id is not found");
            }
            await _companyInfoRepository.RemoveAsync(companyInfo, cancellationToken);
            await _companyInfoRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success($"Company with Id : {request.id} is hard deleted successfully");

        }
    }

}
