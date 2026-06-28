using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Exceptions;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Repository.Repositories;
using EnterprisePlatform.Service.Abstractions;
using EnterprisePlatform.Utils.Logging;
using EnterprisePlatform.Utils.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EnterprisePlatform.Service.Services;

public sealed class AuthService : ServiceBase, IAuthService
{
    private readonly ISysUserRepository _userRepository;
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISysMenuService _menuService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        ISysUserRepository userRepository,
        ISysRoleRepository roleRepository,
        ISysMenuService menuService,
        IOptions<JwtOptions> jwtOptions,
        IFileLogWriter fileLogWriter)
        : base(fileLogWriter)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _menuService = menuService;
        _jwtOptions = jwtOptions.Value;
    }

    public Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(LoginAsync), async () =>
        {
            var user = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);
            if (user is null || !user.IsActive)
                throw new BusinessException("用户名或密码错误", ApiStatusCode.Unauthorized);

            var passwordHash = HashHelper.Sha256($"{request.Password}:{user.UserName}");
            if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
                throw new BusinessException("用户名或密码错误", ApiStatusCode.Unauthorized);

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
            var roles = await _roleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
            var token = CreateToken(user, roles, expiresAt);
            var userDto = await BuildUserDtoAsync(user, roles, cancellationToken);

            return new LoginResponseDto
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                User = userDto
            };
        });

    public Task<SysUserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetCurrentUserAsync), async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null || !user.IsActive)
                throw new BusinessException("用户不存在", ApiStatusCode.NotFound);

            var roles = await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
            return await BuildUserDtoAsync(user, roles, cancellationToken);
        });

    private string CreateToken(SysUser user, IReadOnlyList<SysRole> roles, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.Name, user.UserName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role.RoleCode));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<SysUserDto> BuildUserDtoAsync(
        SysUser user,
        IReadOnlyList<SysRole> roles,
        CancellationToken cancellationToken)
    {
        var permissions = await _menuService.GetUserPermissionCodesAsync(user.Id, cancellationToken);
        var menus = await _menuService.GetUserMenusAsync(user.Id, cancellationToken);

        return new SysUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles.Select(r => r.RoleCode).ToList(),
            Permissions = permissions.ToList(),
            Menus = menus.ToList()
        };
    }
}
