using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        options.SignIn.RequireConfirmedEmail = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<BillingService>();
builder.Services.AddHostedService<BillingBackgroundService>();
builder.Services.AddSingleton<UnitImageStorage>();
builder.Services.AddSingleton<PaymentSlipStorage>();
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddSingleton<ReportExportService>();
builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<MailgunOptions>(builder.Configuration.GetSection("Mailgun"));
builder.Services.AddHttpClient<MailgunEmailSender>();
builder.Services.AddSingleton<IEmailSender>(sp =>
{
    // Mailgun is used when configured; otherwise fall back to plain SMTP (which itself just logs
    // when no host is set either) — see Services/MailgunEmailSender.cs and SmtpEmailSender.cs.
    var mailgun = sp.GetRequiredService<IOptions<MailgunOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(mailgun.Domain) && !string.IsNullOrWhiteSpace(mailgun.ApiKey))
    {
        return sp.GetRequiredService<MailgunEmailSender>();
    }

    return ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// MapStaticAssets below only serves build-time wwwroot content; uploaded unit photos are written
// at runtime, so they need the regular static file middleware.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
