using InventorySystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using InventorySystem.Domain.Constants;
using InventorySystem.Application.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;


namespace InventorySystem.Infrastructure.Services
{
    public class AuthService(IConfiguration configuration) : IAuthService
    {
        private static readonly Dictionary<string, (string Password, string Role)> Users = new()
        {
            ["admin"] = ("Admin@123",Role.WarehouseAdmin),
            ["auditor"] = ("Audit@123", Role.Auditor)
        };

        public Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            if (!Users.TryGetValue(request.Username.ToLowerInvariant(), out var user))
                return Task.FromResult<LoginResponse?>(null);

            if (user.Password != request.Password)
                return Task.FromResult<LoginResponse?>(null);

            var token = GenerateJwtToken(request.Username, user.Role);
            return Task.FromResult<LoginResponse?>(token);
        }

        private LoginResponse GenerateJwtToken(string username, string role)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryHours = int.TryParse(jwtSettings["ExpiryHours"], out var h) ? h : 8;
            var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

            var claims = new[]
            {
            new Claim(ClaimTypes.Name,           username),
            new Claim(ClaimTypes.Role,           role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // Unique token ID
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            return new LoginResponse(tokenString, role, expiresAt);
        }

    }
}
