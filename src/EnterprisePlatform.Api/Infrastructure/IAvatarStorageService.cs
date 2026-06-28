namespace EnterprisePlatform.Api.Infrastructure;

public interface IAvatarStorageService
{
    Task<string> SaveAvatarAsync(IFormFile file, CancellationToken cancellationToken = default);
}
