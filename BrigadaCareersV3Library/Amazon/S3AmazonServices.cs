using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Dto.Enums;
using BrigadaCareersV3Library.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Amazon
{
    public class S3AmazonServices : IS3AmazonServices
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsSettings _awsSettings;
        private readonly BrigadaCareersDbv3Context _appDbContext;

        public S3AmazonServices(
            IAmazonS3 s3Client,
            IOptions<AwsSettings> awsOptions,
            BrigadaCareersDbv3Context appDbContext)
        {
            _s3Client = s3Client;
            _awsSettings = awsOptions.Value;
            _appDbContext = appDbContext;
        }

        // -----------------------------
        // PRIVATE: Upload to S3 only
        // -----------------------------
        private async Task<UploadResult> UploadToS3Async(
            Stream fileStream,
            string fileName,
            string contentType,
            string folderPath)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream cannot be null or empty", nameof(fileStream));

            var sanitizedFileName = SanitizeFileName(fileName);
            var fileExtension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
            var originalFileName = Path.GetFileNameWithoutExtension(sanitizedFileName);
            var ulid = Ulid.NewUlid().ToString();

            var uniqueFileName = $"{ulid}{fileExtension}";
            var key = $"{folderPath}/{uniqueFileName}";

            // Clone stream into a fresh MemoryStream
            var awsStream = new MemoryStream();
            await fileStream.CopyToAsync(awsStream);
            awsStream.Position = 0;

            long fileSize = awsStream.Length; // ✅ record length before upload

            var uploadRequest = new PutObjectRequest
            {
                InputStream = awsStream,
                Key = key,
                BucketName = _awsSettings.BucketName,
                ContentType = contentType,
                Metadata =
        {
            ["original-filename"] = originalFileName
        }
            };

            try
            {
                await _s3Client.PutObjectAsync(uploadRequest);

                var objectUrl = $"https://{_awsSettings.BucketName}.s3.{_awsSettings.Region}.amazonaws.com/{key}";

                return new UploadResult
                {
                    Url = objectUrl,
                    Key = key,
                    UniqueId = ulid,
                    OriginalFileName = originalFileName,
                    ContentType = contentType,
                    FileSize = fileSize, // ✅ use cached length
                    UploadedAt = DateTimeOffset.UtcNow
                };
            }
            catch (AmazonS3Exception ex)
            {
                throw new InvalidOperationException($"Error uploading file to S3: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error occurred: {ex.Message}", ex);
            }
            finally
            {
                await awsStream.DisposeAsync();
            }
        }



        // -----------------------------
        // PUBLIC: Upload and Save
        // -----------------------------
        public async Task<Guid> UploadFileAsync(
            string base64Data,
            string fileName,
            string contentType,
            FileTypeEnum fileType,
            Guid userId,
            string description = "")
        {
            if (string.IsNullOrWhiteSpace(base64Data))
                throw new ArgumentException("Base64 file data cannot be empty", nameof(base64Data));

            // Clean data URL prefix if present
            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

            base64Data = base64Data.Trim();

            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("The input string is not valid Base64 data. Ensure you send only the encoded portion.");
            }

            var fileStream = new MemoryStream(fileBytes);

            try
            {
                string folderPath = GetFolderPath(fileType);

                var uploadResult = await UploadToS3Async(
                    fileStream,
                    fileName,
                    contentType,
                    folderPath
                );

                var fileEntity = new TblAppbinary
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    S3key = uploadResult.Key,
                    FileName = uploadResult.OriginalFileName,
                    FilzeSize = uploadResult.FileSize,
                    FileType = (int)fileType,
                    TypeEnum = (int)fileType,
                    Byte = null,
                    DateUpload = DateTime.UtcNow,
                    IsDeleted = false,
                    Description = description,
                    CreationTime = DateTime.UtcNow
                };

                await _appDbContext.TblAppbinaries.AddAsync(fileEntity);
                await _appDbContext.SaveChangesAsync();

                // ✅ Return just the Guid
                return fileEntity.Id;
            }
            finally
            {
                await fileStream.DisposeAsync();
            }
        }


        // -----------------------------
        // PRIVATE: Delete from S3
        // -----------------------------
        private async Task DeleteFromS3Async(string s3Key)
        {
            if (string.IsNullOrEmpty(s3Key))
                throw new ArgumentException("S3 key cannot be null or empty", nameof(s3Key));

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = s3Key
            };

            try
            {
                await _s3Client.DeleteObjectAsync(deleteRequest);
            }
            catch (AmazonS3Exception ex)
            {
                throw new InvalidOperationException($"Error deleting file from S3: {ex.Message}", ex);
            }
        }

        // -----------------------------
        // PUBLIC: Delete file (soft)
        // -----------------------------
        public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
        {
            var file = await _appDbContext.TblAppbinaries
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId && !f.IsDeleted);

            if (file == null)
                return false;

            if (!string.IsNullOrEmpty(file.S3key))
                await DeleteFromS3Async(file.S3key);

            file.IsDeleted = true;
            await _appDbContext.SaveChangesAsync();

            return true;
        }

        // -----------------------------
        // PUBLIC: Retrieve Helpers
        // -----------------------------
        public async Task<TblAppbinary> GetFileByIdAsync(Guid fileId, Guid userId)
        {
            return await _appDbContext.TblAppbinaries
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId && !f.IsDeleted);
        }

        public async Task<TblAppbinary> GetUserFileByTypeAsync(Guid userId, FileTypeEnum fileType)
        {
            return await _appDbContext.TblAppbinaries
                .Where(f => f.UserId == userId && f.TypeEnum == (int)fileType && !f.IsDeleted)
                .OrderByDescending(f => f.DateUpload)
                .FirstOrDefaultAsync();
        }

        public async Task<List<TblAppbinary>> GetUserFilesByTypeAsync(Guid userId, FileTypeEnum fileType)
        {
            return await _appDbContext.TblAppbinaries
                .Where(f => f.UserId == userId && f.TypeEnum == (int)fileType && !f.IsDeleted)
                .OrderByDescending(f => f.DateUpload)
                .ToListAsync();
        }

        // -----------------------------
        // PUBLIC: URLs
        // -----------------------------
        public string GetFileUrl(string s3Key)
        {
            if (string.IsNullOrEmpty(s3Key))
                return null;

            return $"https://{_awsSettings.BucketName}.s3.{_awsSettings.Region}.amazonaws.com/{s3Key}";
        }

        public string GetFileUrl(TblAppbinary file)
        {
            if (string.IsNullOrEmpty(file.S3key))
                return null;

            return GetFileUrl(file.S3key);
        }

        // -----------------------------
        // PUBLIC: Generate Pre-signed URL
        // -----------------------------
        public string GetPreSignedUrl(string s3Key, int expirationMinutes = 60)
        {
            if (string.IsNullOrEmpty(s3Key))
                throw new ArgumentException("S3 key cannot be null or empty", nameof(s3Key));

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = s3Key,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            return _s3Client.GetPreSignedURL(request);
        }

        // -----------------------------
        // PRIVATE: Folder logic
        // -----------------------------
        private string GetFolderPath(FileTypeEnum fileType)
        {
            var baseFolder = "BrigadaCareers";

            var subFolder = fileType switch
            {
                FileTypeEnum.ProfileImage => "profile-images",
                FileTypeEnum.CoverImage => "cover-images",
                FileTypeEnum.Resume => "resumes",
                FileTypeEnum.Certificate => "certificates",
                FileTypeEnum.Attachment => "attachments",
                _ => "uploads"
            };

            return $"{baseFolder}/{subFolder}";
        }   

        // -----------------------------
        // PRIVATE: Sanitize file names
        // -----------------------------
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            sanitized = sanitized.Replace(" ", "_")
                                 .Replace("#", "")
                                 .Replace("&", "and")
                                 .Replace("%", "");

            return sanitized;
        }
    }

    // -----------------------------
    // SUPPORT: Upload result class
    // -----------------------------
    public class UploadResult
    {
        public string Url { get; set; }
        public string Key { get; set; }
        public string UniqueId { get; set; }
        public string OriginalFileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }
}
