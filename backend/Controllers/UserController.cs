using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization; // 👉 Thư viện JWT
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;     // 👉 Thư viện Token
using System.IdentityModel.Tokens.Jwt;    // 👉 Thư viện JWT
using System.Security.Claims;             // Thư viện Claim (Quyền)
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
        AppDbContext _context) : ControllerBase // Bổ sung IConfiguration để đọc appsettings.json

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
                    Phone = u.PhoneNumber,
                    PhoneNumber = u.PhoneNumber,
                    BankName = u.BankName,
                    BankAccountNumber = u.BankAccountNumber,
                    BankAccountName = u.BankAccountName,
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
                return BadRequest("ID không tồn tại");
            await userService.UpdateUser(user);

            return NoContent();
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateUserProfileDto dto)
        {
            var user = await _context.NsUsers
                .Include(u => u.Branch)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            user.PhoneNumber = dto.PhoneNumber;
            user.BankName = dto.BankName;
            user.BankAccountNumber = dto.BankAccountNumber;
            user.BankAccountName = dto.BankAccountName;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                phone = user.PhoneNumber,
                phoneNumber = user.PhoneNumber,
                bankName = user.BankName,
                bankAccountNumber = user.BankAccountNumber,
                bankAccountName = user.BankAccountName,
                roleName = user.Role?.RoleName,
                branchId = user.BranchId,
                branchName = user.Branch?.Name,
                hireDate = user.HireDate
            });
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
        {
            var user = await _context.NsUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            if (!VerifyPassword(dto.CurrentPassword, user.Password))
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
                return BadRequest(new { message = "Mật khẩu mới cần tối thiểu 4 ký tự." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật mật khẩu thành công." });
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
            // Truy vấn chính xác thông qua _context
            var user = _context.NsUsers
        .Include(u => u.Branch)
        .Include(u => u.Role)
        .FirstOrDefault(u => u.Username == model.Username);

            if (user != null && VerifyPassword(model.Password, user.Password))
            {
                UpgradePlainTextPasswordIfNeeded(user, model.Password);
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "DayLaChuoiKhoaMacDinhChongLoiNull123456789!");

               string roleClaim = "STAFF";
        if (user.RoleId == 1) roleClaim = "ADMIN";
        else if (user.RoleId == 2) roleClaim = "MANAGER";

                // Khởi tạo ClaimsIdentity mạch lạc
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
                phone = user.PhoneNumber,
                phoneNumber = user.PhoneNumber,
                bankName = user.BankName,
                bankAccountNumber = user.BankAccountNumber,
                bankAccountName = user.BankAccountName,
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
        // Lớp hứng dữ liệu từ file React gửi lên
        public class LoginRequest
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        private static bool IsBCryptHash(string password)
        {
            return password.StartsWith("$2a$") || password.StartsWith("$2b$") || password.StartsWith("$2y$");
        }

        private static bool VerifyPassword(string plainPassword, string storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
                return false;

            if (IsBCryptHash(storedPassword))
                return BCrypt.Net.BCrypt.Verify(plainPassword, storedPassword);

            return storedPassword == plainPassword;
        }

        private void UpgradePlainTextPasswordIfNeeded(NsUser user, string plainPassword)
        {
            if (IsBCryptHash(user.Password))
                return;

            user.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            _context.SaveChanges();
        }
    }
}

