using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.Service.Services;
using Volo.Abp.AspNetCore.Mvc;

namespace QmtdltTools.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController : AbpController
    {
        IConfiguration _configuration;
        SysUserService _userService;
        public AuthController(IConfiguration configuration, SysUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [HttpPost("LoginAsGuest")]
        public async Task<IActionResult> LoginAsGuest(string visitorId)
        {
            SysUser user = new SysUser
            {
                Name = visitorId,
                Code = visitorId,
                PasswordHash = visitorId
            };

            var reg_result = await _userService.RegisterGuest(user);                          // 注册为游客用户

            var result = await _userService.CheckLogin(visitorId, visitorId);     // 获取游客登录
            // 简单示例：验证用户名和密码（实际项目中应使用数据库验证）
            if (result.code == 0)
            {
                var token = GenerateJwtToken(result.data);
                return Ok(new { token });
            }
            return Unauthorized();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _userService.CheckLogin(model.Username, model.Password);
            // 简单示例：验证用户名和密码（实际项目中应使用数据库验证）
            if (result.code == 0)
            {
                var token = GenerateJwtToken(result.data);
                return Ok(new { token });
            }
            return Unauthorized();
        }
        [HttpPost("Register")]
        public async Task<Response<bool>> Register(SysUser user)
        {
            return await _userService.Register(user);
        }
        private string GenerateJwtToken(Guid userId)
        {

            var Issuer = _configuration.GetSection("Jwt:Issuer").Get<string>();
            var Audience = _configuration.GetSection("Jwt:Audience").Get<string>();
            var SystenScurityKey = _configuration.GetSection("Jwt:SystenScurityKey").Get<string>();
            var ExpiresMinutes = _configuration.GetSection("Jwt:ExpiresMinutes").Get<double>();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SystenScurityKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),                  // 用户标识
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())   // token 唯一标识
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
        private static ConcurrentDictionary<string, DateTime> _tokenBlacklist = new();
        [Authorize]
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            // 获取 token 的 jti
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (!string.IsNullOrEmpty(jti))
            {
                // 加入黑名单，设置过期时间（建议与 token 有效期一致）
                _tokenBlacklist[jti] = DateTime.UtcNow.AddMinutes(
                    _configuration.GetSection("Jwt:ExpiresMinutes").Get<double>()
                );
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
