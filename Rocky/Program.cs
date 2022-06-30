using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Rocky_DataAccess;
using Rocky_Utility;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));;

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddTransient<IEmailSender, EmailSender>();

// old startap
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// string connection = builder.Configuration.GetConnectionString("DefaultConnection");

// builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));
//builder.Services.AddDefaultIdentity<IdentityUser>().
//    AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(Options =>
{
    Options.IdleTimeout = TimeSpan.FromMinutes(10);
    Options.Cookie.HttpOnly = true;
    Options.Cookie.IsEssential = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// old startap end

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
