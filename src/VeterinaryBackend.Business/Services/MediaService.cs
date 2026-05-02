using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Media;
using VeterinaryBackend.Business.Mappers;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Entities;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class MediaService : IMediaService
{
    private readonly IMediaFileRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IUnitOfWork _unitOfWork;

    public MediaService(
        IMediaFileRepository repository,
        IFileStorageService storage,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _storage = storage;
        _unitOfWork = unitOfWork;
    }

    public async Task<MediaFileDto> UploadAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "Original file name is required." }
            });

        var saved = await _storage.SaveAsync(content, originalFileName, contentType, sizeBytes, ct);

        var entity = new MediaFile
        {
            OriginalFileName = Path.GetFileName(originalFileName),
            StoredFileName = saved.StoredFileName,
            RelativePath = saved.RelativePath,
            Url = saved.Url,
            ContentType = saved.ContentType,
            Extension = saved.Extension,
            SizeBytes = saved.SizeBytes
        };

        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDto();
    }

    public async Task<PagedResultDto<MediaFileDto>> GetAllAsync(PaginationQuery query, CancellationToken ct = default)
    {
        var paged = await _repository.GetPagedNewestAsync(query.Page, query.PageSize, ct);
        return paged.ToPagedDto();
    }

    public async Task<MediaFileDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(MediaFile), id);
        return entity.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(MediaFile), id);

        _storage.DeletePhysical(entity.RelativePath);
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
