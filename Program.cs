using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAvaliacaoDancando.Configuration;
using WebAvaliacaoDancando.Data;
using WebAvaliacaoDancando.Repositories;
using WebAvaliacaoDancando.Services;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "festivaldancando.auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.Configure<AudioProcessingOptions>(builder.Configuration.GetSection("AudioProcessing"));
builder.Services.Configure<WhisperServerOptions>(builder.Configuration.GetSection("Whisper"));
builder.Services.Configure<SupabaseStorageOptions>(builder.Configuration.GetSection("SupabaseStorage"));

builder.Services.AddDbContext<FestivalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection")));

builder.Services.AddScoped<IJuradoRepository, JuradoRepository>();
builder.Services.AddScoped<IApresentacaoRepository, ApresentacaoRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAvaliacaoService, AvaliacaoService>();
builder.Services.AddScoped<IAudioConversionService, FfmpegAudioConversionService>();
builder.Services.AddSingleton<IFestivalSessionService, FestivalSessionService>();
builder.Services.AddHttpClient<IAudioTranscriptionService, WhisperAudioTranscriptionService>((serviceProvider, httpClient) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<WhisperServerOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl);
    httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(30, options.TimeoutSeconds));
});
builder.Services.AddHttpClient<ISupabaseStorageService, SupabaseStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
