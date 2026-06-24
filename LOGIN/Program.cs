using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using LOGIN.Data;

var builder = WebApplication.CreateBuilder(args);

// Forzamos directamente la cadena de conexión nativa de Aiven en el código
var connectionString = "Server=mysql-725ce67-lazaroni-proyecto.l.aivencloud.com;Port=16219;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_mvfq3NkIGAuQeWc7jU5;SslMode=Required;";

var serverVersion = new MySqlServerVersion(new Version(8, 4, 8));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions => 
        mysqlOptions.EnableRetryOnFailure()));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.Name = "UserLoginCookie";
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapControllers();

app.Run();
