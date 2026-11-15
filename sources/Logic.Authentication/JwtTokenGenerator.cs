using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Logic.Authentication
{
    public class JwtTokenGenerator
    {
        public string GenerateToken(List<Claim> claims, string secretKey, string issuer, string audience, int expirationMinutes)
        {
            ArgumentException.ThrowIfNullOrEmpty(secretKey, nameof(secretKey));

            if (claims == null || !claims.Any())
            {
                throw new ArgumentException("Claims cannot be null or empty.", nameof(claims));
            }
           
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
            
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds);
            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token, string secretKey, string issuer, string audience)
        {
            ArgumentException.ThrowIfNullOrEmpty(token, nameof(token));
            ArgumentException.ThrowIfNullOrEmpty(secretKey, nameof(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            
            var key = Encoding.UTF8.GetBytes(secretKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException("Invalid token", ex);
            }
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        }

        /// <summary>
        /// Refresh tokens are 256-bit random values, so a fast SHA-256 hash is sufficient (BCrypt is only needed
        /// for low-entropy passwords). The deterministic hash also allows a direct lookup via the DB index.
        /// </summary>
        public static string HashRefreshToken(string refreshToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(refreshToken, nameof(refreshToken));
            return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        }

        /// <summary>Audience for e-mail verification tokens, so they can never be used as API access tokens.</summary>
        public static string GetEmailVerificationAudience(string audience) => $"{audience}.EmailVerification";

        /// <summary>Audience for password reset tokens (separate from access and verification tokens).</summary>
        public static string GetPasswordResetAudience(string audience) => $"{audience}.PasswordReset";
    }
}
