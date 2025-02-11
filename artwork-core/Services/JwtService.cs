using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArtworkCore.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #region Generate login token
        public string GenerateJwtToken(string username, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

            var claims = new List<Claim>
            {
                    new Claim("username", Convert.ToBase64String(Encoding.UTF8.GetBytes(username))),
                    new Claim(ClaimTypes.Role, role)
            };

            if (key == null || key.Length == 0)
            {
                throw new Exception("JWT Secret Key is not configured.");
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        #endregion

        #region Generate reset token
        public string GenerateResetToken(string email)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            // Các claims lưu thông tin cần thiết như email
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Cấu hình JWT
            var tokenOptions = new JwtSecurityToken(
                claims: claims,
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }
        #endregion

        #region Cliams principal
        public ClaimsPrincipal ValidateResetToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
            /*var key = Encoding.ASCII.GetBytes("Jwt:SecretKey");*/ // Key phải khớp với key dùng để ký token

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidateLifetime = true
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null; // Token không hợp lệ
            }
        }
        #endregion
    }
}
