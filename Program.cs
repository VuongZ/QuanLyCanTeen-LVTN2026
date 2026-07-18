using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer; // 👉 Thư viện JWT
using Microsoft.IdentityModel.Tokens;               // 👉 Thư viện Token
using Microsoft.EntityFrameworkCore;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Missing JWT configuration. Please set Jwt:Issuer, Jwt:Audience, and Jwt:Key in appsettings.json or user secrets.");
}

// 1. Cấu hình kết nối Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. NÂNG CẤP: Chống lỗi lặp vô tận (Object Cycle) khi xuất dữ liệu JSON ra Swagger
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// 3. KHẮC PHỤC LỖI 500: Đăng ký cơ chế Xác thực (Authentication) JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(); // Kích hoạt phân quyền

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Đăng ký các "Nhân viên" (Services & Repositories)
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<RoleRepo>();
builder.Services.AddScoped<RoleService>();  
builder.Services.AddScoped<BranchRepo>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<ShiftRepo>();
builder.Services.AddScoped<ShiftService>();
builder.Services.AddScoped<SchedulePeriodRepo>();
builder.Services.AddScoped<SchedulePeriodService>();
builder.Services.AddScoped<BranchShiftConfigRepo>();
builder.Services.AddScoped<BranchShiftConfigService>();
builder.Services.AddScoped<StaffRegistrationService>();
builder.Services.AddScoped<KhoImportService>();
builder.Services.AddScoped<SupplierRepo>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<InventoryRepo>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<SalaryService>();
builder.Services.AddScoped<KhoExportService>();
builder.Services.AddScoped<FrontStockRepo>();
builder.Services.AddScoped<ShiftClosingService>();
builder.Services.AddScoped<InvoiceOcrService>();    

// Cho phép React/Vite frontend gọi API backend khi chạy local
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

// --- ĐOẠN TEST KẾT NỐI DATABASE TRÊN WAMP ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>(); 
        if (context.Database.CanConnect())
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" CHÚC MỪNG: ĐÃ KẾT NỐI THÀNH CÔNG VỚI WAMPSERVER !");
            Console.WriteLine("==================================================");
        }
        else 
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" THẤT BẠI: KHÔNG THỂ KẾT NỐI TỚI DATABASE.");
            Console.WriteLine("==================================================");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"==================================================");
        Console.WriteLine($" LỖI KẾT NỐI: {ex.Message}");
        Console.WriteLine($"==================================================");
    }
}
// ----------------------------------------------

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c=>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; 
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowReactDev");

// 5. THỨ TỰ MIDDLEWARE BẮT BUỘC: Authentication phải đứng trước Authorization
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();
app.Run();

