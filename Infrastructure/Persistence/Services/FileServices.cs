using Application.Enums;
using Application.Interfaces.Services;
using Domain.Settings;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services
{
    public class FileServices(FileSettings _fileSettings) : IFileServices
    {

        public async Task<string> SaveFileAsync(IFormFile File, FileType type)
        {
            string filePath = string.Empty;
            string fileName = string.Empty;
            var imageFile = File;
            if (imageFile != null && imageFile.Length > 0) {
                fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    _fileSettings.Root,  // wwwroot
                    _fileSettings.FileLocation, // uploads
                    type.ToString(),    // File type: UserImage,ProductImage
                    fileName);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Console.WriteLine($"Creating directory: {directory}");
                    Directory.CreateDirectory(directory);
                }
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

            }
            return $"{_fileSettings.FileLocation}/{type.ToString()}/{fileName}";
        }

        public Task<bool> DeleteFileAsync(string fileName, FileType type)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFileUrlAsync(string fileName, FileType type)
        {
            throw new NotImplementedException();
        }

      
        public Task<string> SaveFileAsync(string filePath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateFileAsync(string oldFileName, IFormFile newFile, FileType type)
        {
            throw new NotImplementedException();
        }
    }
}
