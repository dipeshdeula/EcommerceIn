using Application.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IFileServices
    {
        Task<string> SaveFileAsync(IFormFile File, FileType type);
        Task<bool> DeleteFileAsync(string fileName, FileType type);
        Task<string> GetFileUrlAsync(string fileName, FileType type);
        Task<string> UpdateFileAsync(string oldFileName, IFormFile newFile, FileType type);
    }
}
