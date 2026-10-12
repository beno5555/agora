using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Data;
using colosseumbackend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace colosseumbackend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationOptions(builder.Configuration);
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddSolana();
        builder.Services.AddWalletAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddApplicationRateLimiting();
        builder.Services.AddApplicationOpenApi();
        builder.Services.AddHosting(builder.Configuration);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AgoraDbContext>().Database.Migrate();
        }

        var hostingOptions = app.Services.GetRequiredService<IOptions<HostingOptions>>().Value;
        bool servesFrontend = app.Environment.WebRootFileProvider.GetFileInfo(Constants.FrontendIndexFile).Exists;

        // First, so everything after it (redirects, rate limits, logs) sees the client's IP and scheme.
        if (hostingOptions.ForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Agora API v1");
                options.RoutePrefix = "swagger";
            });
        }

        if (hostingOptions.HttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        // The Angular build, copied into wwwroot, is served from the same origin as the API (the
        // session cookie is same-site and there is no CORS). Before the rate limiter: page assets don't count.
        if (servesFrontend)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks(Constants.HealthCheckPath);

        if (servesFrontend)
        {
            app.MapFallback(Constants.ApiFallbackPattern, () => Results.NotFound());
            app.MapFallbackToFile(Constants.FrontendIndexFile);
        }

        app.Run();
    }
}
