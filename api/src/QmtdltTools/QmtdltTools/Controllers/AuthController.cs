using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.AspNetCore.Mvc;

namespace QmtdltTools.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController : AbpController
    {
        IConfiguration _configuration;
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            // 简单示例：验证用户名和密码（实际项目中应使用数据库验证）
            if (model.Username == "user" && model.Password == "password")
            {
                var token = GenerateJwtToken();
                return Ok(new { token });
            }
            return Unauthorized();
        }

        private string GenerateJwtToken()
        {

            var Issuer = _configuration.GetSection("Jwt:Issuer").Get<string>();
            var Audience = _configuration.GetSection("Jwt:Audience").Get<string>();
            var SystenScurityKey = _configuration.GetSection("Jwt:SystenScurityKey").Get<string>();
            var ExpiresMinutes = _configuration.GetSection("Jwt:ExpiresMinutes").Get<double>();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SystenScurityKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "user_id"), // 用户标识
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // token 唯一标识
            };

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(ExpiresMinutes), // token 有效期 30 分钟
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
