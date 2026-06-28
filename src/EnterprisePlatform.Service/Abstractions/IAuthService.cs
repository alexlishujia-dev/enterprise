using EnterprisePlatform.Core.Dtos;

namespace EnterprisePlatform.Service.Abstractions;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<SysUserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
}
