using Application.Common;
using Application.Dto.CompanyDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.CompanyInfoFeat.Commands
{
    public record CreateCompanyInfoCommand(AddCompanyInfoDTO addCompanyInfo) : IRequest<Result<CompanyInfoDTO>>;

    public class CreateCompanyInfoCommandHandler : IRequestHandler<CreateCompanyInfoCommand, Result<CompanyInfoDTO>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public CreateCompanyInfoCommandHandler(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;
            
        }
        public async Task<Result<CompanyInfoDTO>> Handle(CreateCompanyInfoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var companyInfo = new CompanyInfo
                {
                    Name = request.addCompanyInfo.Name,
                    Email = request.addCompanyInfo.Email,
                    Contact = request.addCompanyInfo.Contact,
                    RegistrationNumber = request.addCompanyInfo.RegistrationNumber,
                    RegisteredPanVatNumber = request.addCompanyInfo.RegisteredPanNumber,
                    Street = request.addCompanyInfo.Street,
                    City = request.addCompanyInfo.City,
                    Province = request.addCompanyInfo.Province,
                    PostalCode = request.addCompanyInfo.PostalCode,
                    WebsiteUrl = request.addCompanyInfo.WebsiteUrl,
                    CreatedAt = request.addCompanyInfo.CreatedAt,

                };

                await _companyInfoRepository.AddAsync(companyInfo, cancellationToken);
                await _companyInfoRepository.SaveChangesAsync(cancellationToken);

               return Result<CompanyInfoDTO>.Success(companyInfo.ToDTO(),"New company Created Successfully");
            }
            catch (Exception ex)
            {
                return Result<CompanyInfoDTO>.Failure("Failed to create a new company",ex.Message);
            }
        }
    }

}
