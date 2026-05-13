using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using UnifiPlus.Web.Authentication;
using UnifiPlus.Web.Options;
using UnifiPlus.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var dataStorageOptions = builder.Configuration
    .GetSection(DataStorageOptions.SectionName)
    .Get<DataStorageOptions>() ?? new DataStorageOptions();

builder.Services.Configure<UniFiOptions>(builder.Configuration.GetSection(UniFiOptions.SectionName));
builder.Services.Configure<DataStorageOptions>(builder.Configuration.GetSection(DataStorageOptions.SectionName));
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataStorageOptions.RootPath, "keys")));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.SchemeName, _ => { });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IUniFiConfigurationStore, FileUniFiConfigurationStore>();
builder.Services.AddSingleton<ILocalUserStore, FileLocalUserStore>();
builder.Services.AddSingleton<IApiKeyStore, FileApiKeyStore>();
builder.Services.AddSingleton<IBandwidthTemplateStore, FileBandwidthTemplateStore>();
builder.Services.AddSingleton<IIdentityBootstrapService, IdentityBootstrapService>();
builder.Services.AddSingleton<ILocalIdentityService, LocalIdentityService>();
builder.Services.AddSingleton<ILocalUserManagementService, LocalUserManagementService>();
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();
builder.Services.AddSingleton<IAdminSetupService, AdminSetupService>();
builder.Services.AddHttpClient<IUniFiApiClient, UniFiApiClient>();
builder.Services.AddSingleton<IUniFiClientAssignmentService, UniFiClientAssignmentService>();
builder.Services.AddSingleton<IClaimsTransformation, DemoClaimsTransformation>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}

app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.Equals("/dashboard", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/dashboard/index", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/uplink-switcher");
        return;
    }

    if (path.Equals("/dashboard/addclient", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/uplink-switcher/add-client");
        return;
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    var bootstrap = context.RequestServices.GetRequiredService<IIdentityBootstrapService>();
    var adminSetupRequired = await bootstrap.IsAdminSetupRequiredAsync(context.RequestAborted);
    var connectionSetupRequired = await bootstrap.IsUniFiConnectionSetupRequiredAsync(context.RequestAborted);
    var path = context.Request.Path.Value ?? string.Empty;
    var isStatic =
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/assets", StringComparison.OrdinalIgnoreCase);

    if (adminSetupRequired)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        if (!path.StartsWith("/setup/admin", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/account/logout", StringComparison.OrdinalIgnoreCase) &&
            !isStatic)
        {
            context.Response.Redirect("/setup/admin");
            return;
        }
    }
    else if (connectionSetupRequired &&
             context.User.Identity?.IsAuthenticated == true &&
             context.User.IsInRole("Admin") &&
             !path.StartsWith("/admin/connection", StringComparison.OrdinalIgnoreCase) &&
             !path.StartsWith("/admin/save", StringComparison.OrdinalIgnoreCase) &&
             !path.StartsWith("/admin/test", StringComparison.OrdinalIgnoreCase) &&
             !path.StartsWith("/account/logout", StringComparison.OrdinalIgnoreCase) &&
             !isStatic)
    {
        context.Response.Redirect("/admin/connection");
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapControllerRoute(
    name: "uplink-switcher-add-client",
    pattern: "uplink-switcher/add-client",
    defaults: new { controller = "Dashboard", action = "AddClient" });

app.MapControllerRoute(
    name: "uplink-switcher-assign",
    pattern: "uplink-switcher/assign",
    defaults: new { controller = "Dashboard", action = "Assign" });

app.MapControllerRoute(
    name: "uplink-switcher-update-wan",
    pattern: "uplink-switcher/update-wan",
    defaults: new { controller = "Dashboard", action = "UpdateWan" });

app.MapControllerRoute(
    name: "uplink-switcher",
    pattern: "uplink-switcher",
    defaults: new { controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "bandwidth-limiter-add-client",
    pattern: "bandwidth-limiter/add-client",
    defaults: new { controller = "Bandwidth", action = "AddClient" });

app.MapControllerRoute(
    name: "bandwidth-limiter-assign",
    pattern: "bandwidth-limiter/assign",
    defaults: new { controller = "Bandwidth", action = "Assign" });

app.MapControllerRoute(
    name: "bandwidth-limiter-update",
    pattern: "bandwidth-limiter/update",
    defaults: new { controller = "Bandwidth", action = "Update" });

app.MapControllerRoute(
    name: "bandwidth-limiter",
    pattern: "bandwidth-limiter",
    defaults: new { controller = "Bandwidth", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
