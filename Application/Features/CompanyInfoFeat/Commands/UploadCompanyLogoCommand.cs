using Application.Common;
using Application.Dto.CompanyDTOs;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CompanyInfoFeat.Commands
{
    public record UploadCompanyLogoCommand ( int Id, IFormFile File) : IRequest<Result<CompanyInfoDTO>>;


    public class UploadCompanyLogoCommandHandler : IRequestHandler<UploadCompanyLogoCommand, Result<CompanyInfoDTO>>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        private readonly IFileServices _fileService;
        public UploadCompanyLogoCommandHandler(ICompanyInfoRepository companyInfoRepository, IFileServices fileService)
        {
            _companyInfoRepository = companyInfoRepository;
            _fileService = fileService;
            
        }
        public async Task<Result<CompanyInfoDTO>> Handle(UploadCompanyLogoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var companyInfo = await _companyInfoRepository.FindByIdAsync(request.Id);
                if (companyInfo == null)
                {
                    return Result<CompanyInfoDTO>.Failure("Company Id not found");

                }

                string fileUrl = null;
                if (request.File != null)
                {
                    fileUrl = await _fileService.SaveFileAsync(request.File, FileType.CompanyLog);
                }

                companyInfo.LogoUrl = fileUrl ?? "";
                await _companyInfoRepository.UpdateAsync(companyInfo, cancellationToken);
                await _companyInfoRepository.SaveChangesAsync(cancellationToken);

                return Result<CompanyInfoDTO>.Success(companyInfo.ToDTO(), "User image updated successfully");

            }
            catch (Exception ex)
            {
                return Result<CompanyInfoDTO>.Failure("faile to create company log", ex.Message);
            }
        }
    }
}
