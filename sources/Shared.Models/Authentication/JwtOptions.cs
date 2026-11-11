using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Authentication
{
    public class JwtOptions
    {
        [Required, MinLength(32)]
        public string Key { get; set; } = string.Empty;
        [Required]
        public string Issuer { get; set; } = string.Empty;
        [Required]
        public string Audience { get; set; } = string.Empty;
        /// <summary>Lifetime of the access token in minutes.</summary>
        [Range(1, 1440)]
        public int ExpireMinutes { get; set; }
        [Range(1, 365)]
        public int RefreshTokenExpireDays { get; set; }
    }
}
