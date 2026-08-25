namespace TutoringBackend.Api.Middleware;

/// Standardowe nagłówki obronne. Dla czystego API (JSON, konsumowanego przez Next.js)
/// CSP jest minimalny (default-src 'none'), bo API nie serwuje żadnego HTML/JS samo z siebie.
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        // HSTS jest dodatkowo włączony przez app.UseHsts() w Program.cs (poza Development).

        await _next(context);
    }
}
