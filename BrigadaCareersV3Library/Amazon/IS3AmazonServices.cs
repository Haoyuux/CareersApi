using BrigadaCareersV3Library.Dto.Enums;
using BrigadaCareersV3Library.Entities;

namespace BrigadaCareersV3Library.Amazon
{
    public interface IS3AmazonServices
    {
        Task<bool> DeleteFileAsync(Guid fileId, Guid userId);
        Task<TblAppbinary> GetFileByIdAsync(Guid fileId, Guid userId);
        string GetFileUrl(string s3Key);
        string GetFileUrl(TblAppbinary file);
        string GetPreSignedUrl(string s3Key, int expirationMinutes = 60);
        Task<TblAppbinary> GetUserFileByTypeAsync(Guid userId, FileTypeEnum fileType);
        Task<List<TblAppbinary>> GetUserFilesByTypeAsync(Guid userId, FileTypeEnum fileType);
        Task<TblAppbinary> UploadFileAsync(string base64Data, string fileName, string contentType, FileTypeEnum fileType, Guid userId, string description = "");
    }
}