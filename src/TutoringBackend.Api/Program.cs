using TutoringBackend.Api.Data;
using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Extensions;
using TutoringBackend.Api.Middleware;
using TutoringBackend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Konfiguracja ---
builder.Configuration["IsDevelopment"] = builder.Environment.IsDevelopment().ToString();

// --- Usługi ---
builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddAppIdentity();
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddAppRateLimiting();
builder.Services.AddAppCors(builder.Configuration);

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IScheduleGenerationService, ScheduleGenerationService>();
builder.Services.AddHostedService<ScheduleGenerationBackgroundService>();

builder.Services.AddControllers()
    // Domyślnie ASP.NET Core koduje znaki niebezpieczne dla HTML w wyjściu JSON
    // (System.Text.Json ma bezpieczne domyślne escapowanie < > & itd.) - nie wyłączać tego.
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = false);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Migracje + seed przy starcie
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
