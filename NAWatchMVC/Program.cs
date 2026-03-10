using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm dòng này để dùng Cookie
using Microsoft.AspNetCore.Identity; // Thêm dòng này để dùng PasswordHasher

var builder = WebApplication.CreateBuilder(args);

// --- ĐĂNG KÝ SERVICES (DI CONTAINER) ---
builder.Services.AddControllersWithViews();

// 1. Đăng ký kết nối SQL Server
builder.Services.AddDbContext<NawatchMvcContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("NAWatchMVC"));
});

// 2. Đăng ký Authentication bằng Cookie (BẮT BUỘC để Login chạy được)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/KhachHang/DangNhap"; // Đường dẫn nếu khách chưa login mà đòi vào trang cấm
        options.AccessDeniedPath = "/";          // Đường dẫn nếu vào trang không đủ quyền
        options.ExpireTimeSpan = TimeSpan.FromHours(1); // Cookie sống trong 1 tiếng
    });

// 3. Đăng ký Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Đăng ký các Helper "xịn" của ní
builder.Services.AddTransient<NAWatchMVC.Helpers.MyEmailSender>(); // Gửi mail OTP

// 5. Đăng ký Password Hasher (BẮT BUỘC để fix lỗi controller không băm được pass)
builder.Services.AddScoped<IPasswordHasher<KhachHang>, PasswordHasher<KhachHang>>();

var app = builder.Build();

// --- CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// THỨ TỰ BẮT BUỘC: Session -> Authentication -> Authorization
app.UseSession();         // Phải nằm trước Auth
app.UseAuthentication();  // Phải nằm trước Authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();