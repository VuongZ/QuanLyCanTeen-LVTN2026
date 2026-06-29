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
using Microsoft.EntityFrameworkCore;

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
                    HireDate = u.HireDate
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

        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
        {
            var user = await _context.NsUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Khong tim thay nguoi dung." });

            if (user.Password != dto.CurrentPassword)
                return BadRequest(new { message = "Mat khau hien tai khong dung." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
                return BadRequest(new { message = "Mat khau moi can toi thieu 4 ky tu." });

            user.Password = dto.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Da cap nhat mat khau thanh cong." });
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
            var user = _context.NsUsers
        .Include(u => u.Branch)
        .Include(u => u.Role)
        .FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);

            if (user != null)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "DayLaChuoiKhoaMacDinhChongLoiNull123456789!");

               string roleClaim = "STAFF";
        if (user.RoleId == 1) roleClaim = "ADMIN";
        else if (user.RoleId == 2) roleClaim = "MANAGER";

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
                roleName = user.Role != null ? user.Role.RoleName : roleClaim,
                branchId = user.BranchId,
                branchName = user.Branch != null ? user.Branch.Name : "Chưa có",
                hireDate = user.HireDate 
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
