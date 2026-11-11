using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        public ClaimsPrincipal ValidateToken(string token, string secretKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(token, nameof(token));
            ArgumentException.ThrowIfNullOrEmpty(secretKey, nameof(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            
            var key = System.Text.Encoding.UTF8.GetBytes(secretKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero // Optional: Adjust for clock skew if needed
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
            var randomNumber = new byte[32];

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
