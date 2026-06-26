using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization; // 👉 Thư viện JWT
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;     // 👉 Thư viện Token
using System.IdentityModel.Tokens.Jwt;    // 👉 Thư viện JWT
using System.Security.Claims;             // 👉 Thư viện Claim (Quyền)
using System.Text;

namespace LuanVanTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(
        UserService userService,
        RoleService roleService,
        BranchService branchService,
        IConfiguration configuration,
        AppDbContext _context) : ControllerBase // 👉 Bổ sung IConfiguration để đọc appsettings.json
      
    {
        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            var users = await userService.GetAllUser();
            var roles = await roleService.GetAllRole();
            var branch = await branchService.GettAllBranchAsync();
            var result = new UserPageDataDto
            {
                Users = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    BranchId = u.BranchId,
                    BranchName = u.Branch?.Name,
                    RoleId = u.RoleId,
                    RoleName = u.Role?.RoleName,
                    HireDate = u.HireDate,
                    Password = u.Password
                }),

                Roles = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    HourlyWage = r.HourlyWage,
                    SeniorWage = r.SeniorWage
                }),

            };
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await userService.GettUserbyId(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(NsUser user)
        {
            await userService.AddUser(user);
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, NsUser user)
        {
            if (id != user.Id)
                return BadRequest("ID Khong Ton Tai");
            await userService.UpdateUser(user);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSinhVien(int id)
        {
            await userService.DeleteUser(id);
            return NoContent();
        }

        // =========================================================
        // 👉 BỔ SUNG API LOGIN BÊN DƯỚI
        // =========================================================

        [HttpPost("login")]
[AllowAnonymous]
public IActionResult Login([FromBody] LoginRequest model)
{
    // Truy vấn chính xác thông qua biến _context cục bộ
    var user = _context.NsUsers.FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);

    if (user != null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "DayLaChuoiKhoaMacDinhChongLoiNull123456789!");

        string roleClaim = user.RoleId == 1 ? "ADMIN" : "MANAGER";

        // Khởi tạo ClaimsIdentity mạch lạc, không bị thừa rác
        var claims = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, roleClaim),             
            new Claim("BranchId", user.BranchId.ToString() ?? "1") 
        });

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            user = new
            {
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                role = roleClaim,
                branchId = user.BranchId
            }
        });
    }

    return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
}
        // 👉 Lớp hứng dữ liệu từ file React gửi lên
        public class LoginRequest
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
        }
    }
}