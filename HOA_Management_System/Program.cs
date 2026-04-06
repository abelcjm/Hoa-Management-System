using Microsoft.EntityFrameworkCore;
using HOA_Management_System.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HoaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Authentication Services 
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "HOA.Session";
        options.LoginPath = "/Account/Login"; // Redirect here if not logged in
        options.AccessDeniedPath = "/Account/AccessDenied";

        // --- ADDED: Syncs the login cookie with the 10-minute session limit ---
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = true; // Resets the 10 minutes every time they click a link
    });

builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddSession(options =>
{
    // --- UPDATED: 10 minutes of inactivity timeout ---
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register the Email Service
builder.Services.AddTransient<HOA_Management_System.Services.EmailService>();

// Register the Auto-Email Background Robot
builder.Services.AddHostedService<HOA_Management_System.Services.AutoReminderService>();

// Register the Background Service
builder.Services.AddHostedService<HOA_Management_System.Services.BackupHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();