using Hangfire;
using Rentlyo.API.Extensions;
using Rentlyo.API.Middleware;
using Rentlyo.Infrastructure;
using Rentlyo.Infrastructure.Hangfire;
using Rentlyo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

HangfireJobs.RegisterRecurringJobs(app.Services.GetRequiredService<IRecurringJobManager>());

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rentlyo API v1");
        options.RoutePrefix = "swagger";
    });
    app.UseHangfireDashboard("/hangfire");
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
app.MapControllers();

app.Run();
