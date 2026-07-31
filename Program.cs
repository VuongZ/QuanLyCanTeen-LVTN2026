using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Services;
using LuanVanTotNghiep.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// JWT CONFIGURATION
// ==============================
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Missing JWT configuration. " +
        "Please set Jwt:Issuer, Jwt:Audience, " +
        "and Jwt:Key in appsettings.json or user secrets.");
}

// ==============================
// DATABASE
// ==============================
var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)));

// ==============================
// CONTROLLERS VÀ JSON
// ==============================
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization
                .ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==============================
// AUTHENTICATION VÀ AUTHORIZATION
// ==============================
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
            };
    });

builder.Services.AddAuthorization();

// ==============================
// REPOSITORIES VÀ SERVICES
// ==============================

// Người dùng và phân quyền
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<RoleRepo>();
builder.Services.AddScoped<RoleService>();

builder.Services.AddScoped<EmailService>();

// Chi nhánh
builder.Services.AddScoped<BranchRepo>();
builder.Services.AddScoped<BranchService>();

// Ca làm và cấu hình ca
builder.Services.AddScoped<ShiftRepo>();
builder.Services.AddScoped<ShiftService>();

builder.Services.AddScoped<BranchShiftConfigRepo>();
builder.Services.AddScoped<BranchShiftConfigService>();

// Đợt đăng ký ca
builder.Services.AddScoped<SchedulePeriodRepo>();
builder.Services.AddScoped<SchedulePeriodService>();

// Đăng ký ca
builder.Services.AddScoped<StaffRegistrationRepo>();
builder.Services.AddScoped<StaffRegistrationService>();

// Lịch làm chính thức
builder.Services.AddScoped<FinalScheduleRepo>();
builder.Services.AddScoped<FinalScheduleService>();

// Nghỉ, vắng và thay ca khẩn cấp
builder.Services.AddScoped<EmergencyReplacementRepo>();
builder.Services.AddScoped<EmergencyReplacementService>();

// Ủy quyền ca
builder.Services.AddScoped<ShiftDelegationService>();

// Điểm danh
builder.Services.AddScoped<AttendanceRepo>();
builder.Services.AddScoped<AttendanceService>();

// Nhà cung cấp
builder.Services.AddScoped<SupplierRepo>();
builder.Services.AddScoped<SupplierService>();

// Kho chi nhánh
builder.Services.AddScoped<InventoryRepo>();
builder.Services.AddScoped<InventoryService>();

builder.Services.AddScoped<KhoImportRepo>();
builder.Services.AddScoped<KhoImportService>();

builder.Services.AddScoped<KhoExportRepo>();
builder.Services.AddScoped<KhoExportService>();

// Tồn quầy
builder.Services.AddScoped<FrontStockRepo>();
builder.Services.AddScoped<FrontStockService>();

// Báo cáo kết ca
builder.Services.AddScoped<ShiftClosingRepo>();
builder.Services.AddScoped<ShiftClosingService>();

// Quên checkout
builder.Services.AddScoped<CheckoutRequestService>();

// Nghiệp vụ lương
builder.Services.AddScoped<SalaryService>();

// Nghiệp vụ khiếu nại lương
builder.Services.AddScoped<SalaryComplaintService>();

// OCR hóa đơn
builder.Services.AddScoped<InvoiceOcrService>();

// Background jobs
builder.Services.AddHostedService<MissingCheckoutWorker>();
builder.Services.AddHostedService<SchedulePeriodDeadlineWorker>();

// ==============================
// CORS
// ==============================
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowReactDev",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// ==============================
// KIỂM TRA KẾT NỐI DATABASE
// ==============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<AppDbContext>();

        if (context.Database.CanConnect())
        {
            Console.WriteLine(
                "==================================================");
            Console.WriteLine(
                " CHÚC MỪNG: ĐÃ KẾT NỐI THÀNH CÔNG VỚI WAMPSERVER!");
            Console.WriteLine(
                "==================================================");
        }
        else
        {
            Console.WriteLine(
                "==================================================");
            Console.WriteLine(
                " THẤT BẠI: KHÔNG THỂ KẾT NỐI TỚI DATABASE.");
            Console.WriteLine(
                "==================================================");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "==================================================");
        Console.WriteLine(
            $" LỖI KẾT NỐI: {ex.Message}");
        Console.WriteLine(
            "==================================================");
    }
}

// ==============================
// HTTP PIPELINE
// ==============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "My API V1");

        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowReactDev");

// Authentication luôn đứng trước Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();