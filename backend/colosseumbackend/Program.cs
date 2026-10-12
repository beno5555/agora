using colosseumbackend.Data;
using colosseumbackend.Extensions;
using Microsoft.EntityFrameworkCore;

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
        builder.Services.AddOpenApi();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AgoraDbContext>().Database.Migrate();
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

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
