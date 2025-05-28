using Application.Common;
using Application.Enums;
using Application.Interfaces.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ImageFeat.Queries
{
    public record GetImageQuery(string fileName, string folderName) : IRequest<Stream?>;

    public class GetImageQueryHandler : IRequestHandler<GetImageQuery, Stream?>
    {
        private readonly IFileServices _fileService;
        public GetImageQueryHandler(IFileServices fileService)
        {
            _fileService = fileService;
        }

        public async Task<Stream?> Handle(GetImageQuery request, CancellationToken cancellationToken)
        {
            var filePath = await _fileService.GetImage(request.fileName, request.folderName);
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }
            return System.IO.File.OpenRead(filePath);
        }
    }

}
