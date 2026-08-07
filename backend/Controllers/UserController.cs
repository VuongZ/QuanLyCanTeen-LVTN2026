using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
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
                    Username = GetLoginDisplay(u),
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = u.PhoneNumber,
                    PhoneNumber = u.PhoneNumber,
                    BankName = u.NsUserBankAccounts.FirstOrDefault()?.BankName,
                    BankAccountNumber = u.NsUserBankAccounts.FirstOrDefault()?.BankAccountNumber,
                    BankAccountName = u.NsUserBankAccounts.FirstOrDefault()?.BankAccountName,
                    BranchId = u.BranchId,
                    BranchName = u.Branch?.Name,
                    RoleId = u.RoleId,
                    RoleName = u.Role?.RoleName,
                    HireDate = u.HireDate,
                    EmploymentType = SalaryWagePolicy.NormalizeEmploymentType(u.EmploymentType),
                    SalaryCoefficient = SalaryWagePolicy.GetEffectiveSalaryCoefficient(
                        u,
                        DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))),
                    SalaryCoefficientIsManual = u.SalaryCoefficientIsManual
                }),

                Roles = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    HourlyWage = r.HourlyWage
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
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AddUser(UserDto user)
        {
            try
            {
                var created = await userService.AddUser(user);
                return Ok(ToUserResponse(created));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Email hoặc số điện thoại đã được sử dụng bởi tài khoản khác."
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateUser(int id, UserDto user)
        {
            if (id != user.Id)
                return BadRequest("ID không tồn tại");

            try
            {
                await userService.UpdateUser(user);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateUserProfileDto dto)
        {
            var user = await _context.NsUsers
                .Include(u => u.Branch)
                .Include(u => u.Role)
                .Include(u => u.NsUserBankAccounts)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            user.PhoneNumber = dto.PhoneNumber;
            var bank = user.NsUserBankAccounts.FirstOrDefault();
            if (bank == null)
            {
                bank = new NsUserBankAccount { UserId = user.Id };
                _context.NsUserBankAccounts.Add(bank);
            }
            bank.BankName = dto.BankName;
            bank.BankAccountNumber = dto.BankAccountNumber;
            bank.BankAccountName = dto.BankAccountName;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = user.Id,
                username = GetLoginDisplay(user),
                email = user.Email,
                fullName = user.FullName,
                phone = user.PhoneNumber,
                phoneNumber = user.PhoneNumber,
                bankName = bank.BankName,
                bankAccountNumber = bank.BankAccountNumber,
                bankAccountName = bank.BankAccountName,
                roleName = user.Role?.RoleName,
                branchId = user.BranchId,
                branchName = user.Branch?.Name,
                hireDate = user.HireDate,
                employmentType = SalaryWagePolicy.NormalizeEmploymentType(user.EmploymentType),
                salaryCoefficient = SalaryWagePolicy.GetEffectiveSalaryCoefficient(
                    user,
                    DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))),
                salaryCoefficientIsManual = user.SalaryCoefficientIsManual
            });
        }

        [HttpPut("{id}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            if (currentUser.Id != id)
                return Forbid();

            var result = await userService.ChangePasswordAsync(id, dto.CurrentPassword, dto.NewPassword, dto.Otp);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("{id}/password/otp")]
        [Authorize]
        public async Task<IActionResult> SendChangePasswordOtp(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            if (currentUser.Id != id)
                return Forbid();

            var result = await userService.SendChangePasswordOtpAsync(id);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            var result = await userService.SendPasswordResetOtpAsync(dto.Identifier);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await userService.ResetPasswordWithOtpAsync(dto.Identifier, dto.Otp, dto.NewPassword);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSinhVien(int id)
        {
            await userService.DeleteUser(id);
            return NoContent();
        }

        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var restored = await userService.RestoreUser(id);
            if (!restored)
                return NotFound(new { message = "Không tìm thấy nhân viên đã xóa." });

            return Ok(new { message = "Khôi phục nhân viên thành công." });
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetNhanVienDaXoa()
        {
            var users = await userService.GetDaXoa();
            var roles = await roleService.GetAllRole();
            var branch = await branchService.GettAllBranchAsync();
            var result = new UserPageDataDto
            {
                Users = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = GetLoginDisplay(u),
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = u.PhoneNumber,
                    PhoneNumber = u.PhoneNumber,
                    BankName = u.NsUserBankAccounts.FirstOrDefault()?.BankName,
                    BankAccountNumber = u.NsUserBankAccounts.FirstOrDefault()?.BankAccountNumber,
                    BankAccountName = u.NsUserBankAccounts.FirstOrDefault()?.BankAccountName,
                    BranchId = u.BranchId,
                    BranchName = u.Branch?.Name,
                    RoleId = u.RoleId,
                    RoleName = u.Role?.RoleName,
                    HireDate = u.HireDate,
                    EmploymentType = SalaryWagePolicy.NormalizeEmploymentType(u.EmploymentType),
                    SalaryCoefficient = SalaryWagePolicy.GetEffectiveSalaryCoefficient(
                        u,
                        DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))),
                    SalaryCoefficientIsManual = u.SalaryCoefficientIsManual
                }),
                Roles = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    HourlyWage = r.HourlyWage
                }),
            };
            return Ok(result);
        }
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var identifier = model.Identifier ?? model.Username;
            if (string.IsNullOrWhiteSpace(identifier))
                return Unauthorized(new { message = "Vui lòng nhập email hoặc số điện thoại." });

            var foundUser = await userService.FindByIdentifierAsync(identifier);
            var user = foundUser == null
                ? null
                : await _context.NsUsers
                    .Include(u => u.Branch)
                    .Include(u => u.Role)
                    .Include(u => u.NsUserBankAccounts)
                    .FirstOrDefaultAsync(u => u.Id == foundUser.Id && u.IsDeleted != true);

            if (user != null && UserService.VerifyPassword(model.Password, user.Password))
            {
                UpgradePlainTextPasswordIfNeeded(user, model.Password);
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "DayLaChuoiKhoaMacDinhChongLoiNull123456789!");

                string roleClaim = "STAFF";
                if (user.RoleId == 1) roleClaim = "ADMIN";
                else if (user.RoleId == 2) roleClaim = "MANAGER";

                // Tạo các claim bắt buộc của tài khoản.
                var claimList = new List<Claim>
{
    new Claim(
        ClaimTypes.NameIdentifier,
        user.Id.ToString()
    ),

    new Claim(
        ClaimTypes.Name,
        GetLoginDisplay(user) ??
            user.Id.ToString()
    ),

    new Claim(
        ClaimTypes.Role,
        roleClaim
    )
};

                // Chỉ thêm BranchId khi tài khoản thực sự
                // được gán vào một chi nhánh.
                if (
                    user.BranchId.HasValue &&
                    user.BranchId.Value > 0
                )
                {
                    claimList.Add(
                        new Claim(
                            "BranchId",
                            user.BranchId.Value.ToString()
                        )
                    );
                }

                // Tạo ClaimsIdentity từ danh sách claim.
                var claims = new ClaimsIdentity(
                    claimList
                );

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
                        username = GetLoginDisplay(user),
                        email = user.Email,
                        fullName = user.FullName,
                        phone = user.PhoneNumber,
                        phoneNumber = user.PhoneNumber,
                        bankName = user.NsUserBankAccounts.FirstOrDefault()?.BankName,
                        bankAccountNumber = user.NsUserBankAccounts.FirstOrDefault()?.BankAccountNumber,
                        bankAccountName = user.NsUserBankAccounts.FirstOrDefault()?.BankAccountName,
                        role = roleClaim,
                        roleName = user.Role != null ? user.Role.RoleName : roleClaim,
                        branchId = user.BranchId,
                        branchName = user.Branch != null ? user.Branch.Name : "Chưa có",
                        hireDate = user.HireDate,
                        employmentType = SalaryWagePolicy.NormalizeEmploymentType(user.EmploymentType),
                        salaryCoefficient = SalaryWagePolicy.GetEffectiveSalaryCoefficient(
                     user,
                     DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))),
                        salaryCoefficientIsManual = user.SalaryCoefficientIsManual
                    }
                });
            }

            return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
        }
        // Lớp hứng dữ liệu từ file React gửi lên
        public class LoginRequest
        {
            public string? Identifier { get; set; }
            public string? Username { get; set; }
            public string Password { get; set; } = null!;
        }

        private void UpgradePlainTextPasswordIfNeeded(NsUser user, string plainPassword)
        {
            if (UserService.IsBCryptHash(user.Password))
                return;

            user.Password = UserService.HashPassword(plainPassword);
            _context.SaveChanges();
        }

        private static string? GetLoginDisplay(NsUser user)
        {
            return user.Email ?? user.PhoneNumber;
        }

        private async Task<NsUser?> GetCurrentUserAsync()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var currentUserId)
                ? await _context.NsUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId)
                : await _context.NsUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email == username || u.PhoneNumber == username);
        }

        private static object ToUserResponse(NsUser user)
        {
            var bank = user.NsUserBankAccounts.FirstOrDefault();
            return new
            {
                id = user.Id,
                username = GetLoginDisplay(user),
                email = user.Email,
                fullName = user.FullName,
                phone = user.PhoneNumber,
                phoneNumber = user.PhoneNumber,
                bankName = bank?.BankName,
                bankAccountNumber = bank?.BankAccountNumber,
                bankAccountName = bank?.BankAccountName,
                roleName = user.Role?.RoleName,
                branchId = user.BranchId,
                branchName = user.Branch?.Name,
                hireDate = user.HireDate,
                employmentType = SalaryWagePolicy.NormalizeEmploymentType(user.EmploymentType),
                salaryCoefficient = SalaryWagePolicy.GetEffectiveSalaryCoefficient(
                    user,
                    DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))),
                salaryCoefficientIsManual = user.SalaryCoefficientIsManual
            };
        }
    }
}
