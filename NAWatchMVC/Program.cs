using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//dang ki ket noi
builder.Services.AddDbContext<NawatchMvcContext>(options => { options.UseSqlServer(builder.Configuration.GetConnectionString("NAWatchMVC")); });
// 1. Đăng ký dịch vụ Session vào hệ thống
builder.Services.AddDistributedMemoryCache(); // Cần thiết để lưu trữ Session vào bộ nhớ
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session sẽ hết hạn sau 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
// 2. Kích hoạt Session cho ứng dụng (BẮT BUỘC đặt ở đây)
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
