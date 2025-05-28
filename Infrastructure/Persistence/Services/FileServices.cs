using Application.Enums;

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
           var filePath = Path.Combine(
               Directory.GetCurrentDirectory(),
               _fileSettings.Root,
                _fileSettings.FileLocation,
                type.ToString(),
                fileName);

            if(File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<string> GetFileUrlAsync(string fileName, FileType type)
        {
            throw new NotImplementedException();
        }

        public async Task<string> UpdateFileAsync(string oldFileName, IFormFile newFile, FileType type)
        {
            // Step 1: If no new file is provided, return the old file path
            if (newFile == null || newFile.Length == 0)
            {
                return oldFileName; // Return the old file name as it is
            }

            // Step 2: Delete the old file if it exists
            if (!string.IsNullOrEmpty(oldFileName))
            {
                var oldFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    _fileSettings.Root,
                    _fileSettings.FileLocation,
                    type.ToString(),
                    oldFileName);

                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            // Step 3: Save the new file
            string newFilePath = string.Empty;
            string newFileName = string.Empty;

            if (newFile != null && newFile.Length > 0)
            {
                newFileName = Guid.NewGuid() + Path.GetExtension(newFile.FileName);
                newFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    _fileSettings.Root,
                    _fileSettings.FileLocation,
                    type.ToString(),
                    newFileName);

                var directory = Path.GetDirectoryName(newFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await newFile.CopyToAsync(stream);
                }
            }

            // Step 4: Return the relative path of the new file
            return $"{_fileSettings.FileLocation}/{type.ToString()}/{newFileName}";
        }

        public Task<string> GetImage(string fileName, string folderName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot",
                _fileSettings.FileLocation,
                folderName,
                fileName);
            return Task.FromResult(filePath);
           //return Task.FromResult($"{_fileSettings.FileLocation}/{type}/{fileName}");

        }
    }
}
