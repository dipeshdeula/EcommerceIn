using Application.Common;
using Application.Dto.CompanyDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.CompanyInfoFeat.Commands
{
    public record UpdateCompanyInfoCommand(
        int Id,
        UpdateCompanyInfoDTO updateCompanyInfoDto
        ) : IRequest<Result<CompanyInfoDTO>>;

    public class UpdateCompanyInfoCommandHandler : IRequestHandler<UpdateCompanyInfoCommand, Result<CompanyInfoDTO>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public UpdateCompanyInfoCommandHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
            
        }
        public async Task<Result<CompanyInfoDTO>> Handle(UpdateCompanyInfoCommand request, CancellationToken cancellationToken)
        {
            var companyInfo = await _companyInfoRepository.FindByIdAsync(request.Id);
            if (companyInfo == null)
            {
                return Result<CompanyInfoDTO>.Failure("CompanyInfo Id is not found");
            }

            companyInfo.Name = request.updateCompanyInfoDto.Name ?? companyInfo.Name;
            companyInfo.Email = request.updateCompanyInfoDto.Email ?? companyInfo.Email;
            companyInfo.Contact = request.updateCompanyInfoDto.Contact ?? companyInfo.Contact;
            companyInfo.RegistrationNumber = request.updateCompanyInfoDto.RegistrationNumber ?? companyInfo.RegistrationNumber;
            companyInfo.RegisteredPanVatNumber = request.updateCompanyInfoDto.RegisteredPanNumber ?? companyInfo.RegisteredPanVatNumber;
            companyInfo.Street = request.updateCompanyInfoDto.Street ?? companyInfo.Street;
            companyInfo.City = request.updateCompanyInfoDto.City ?? companyInfo.City;
            companyInfo.Province = request.updateCompanyInfoDto.Province ?? companyInfo.Province;
            companyInfo.PostalCode = request.updateCompanyInfoDto.PostalCode ?? companyInfo.PostalCode;
            companyInfo.WebsiteUrl = request.updateCompanyInfoDto.WebsiteUrl ?? companyInfo.WebsiteUrl;
            companyInfo.UpdateAt = DateTime.UtcNow;

            await _companyInfoRepository.UpdateAsync(companyInfo, cancellationToken);
            await _companyInfoRepository.SaveChangesAsync(cancellationToken);

            return Result<CompanyInfoDTO>.Success(companyInfo.ToDTO(), $"CompanyInfo with id : {request.Id} is updated successfully");

        }
    }

}
