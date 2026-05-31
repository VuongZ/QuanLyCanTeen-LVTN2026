using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. NÂNG CẤP: Chống lỗi lặp vô tận (Object Cycle) khi xuất dữ liệu JSON ra Swagger
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Đăng ký các "Nhân viên" (Services & Repositories)
builder.Services.AddScoped<UserService>();
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
app.UseAuthorization();
app.MapControllers();
app.Run();