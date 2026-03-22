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

// 2. Gộp Authentication vào một chỗ duy nhất (Dùng chung cho cả khách và nhân viên)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/KhachHang/DangNhap";    // Nếu chưa login -> Đá về đây
        options.AccessDeniedPath = "/KhachHang/TuChoi"; // Nếu khách chọc vào Admin -> Đá về đây
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Cho phép làm việc 8 tiếng
        options.Cookie.Name = "NAWatch_Auth";           // Đặt tên Cookie cho chuyên nghiệp
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
builder.Services.AddScoped<IPasswordHasher<NhanVien>, PasswordHasher<NhanVien>>(); // Thêm ông này nữa ní nhé


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
app.UseAuthentication();  // Phải nằm trước Authorization Ai là người đang truy cập?
app.UseAuthorization();  // Người đó có quyền vào đây không?



// 1. Route dành cho Areas (Phải nằm TRÊN)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// 2. Route mặc định cho khách hàng (Nằm DƯỚI)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();