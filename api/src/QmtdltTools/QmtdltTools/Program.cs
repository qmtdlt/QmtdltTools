using QmtdltTools;
using QmtdltTools.Domain.Data;
using QmtdltTools.Hubs;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Volo.Abp.AspNetCore.SignalR;

const string Cors = "VueApp";
InitLog();
var builder = WebApplication.CreateBuilder(args);           // got a web application builder

builder.Host
    .AddAppSettingsSecretsJson()        // add appsettings.json
    .UseAutofac()                       // use autofac
    .UseSerilog();                      // use serilog
    
builder.Services.AddOpenApi();
builder.Services.AddControllers();              // new add
builder.Services.AddEndpointsApiExplorer();     // new add 


var configuration = builder.Services.GetConfiguration();
var allowedCorsOrigins = configuration.GetSection("AllowedCorsOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(Cors, builder =>
    {
        builder.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


await builder.AddApplicationAsync<QmtdltToolsAPIModule>();  // add application

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.UseCors(Cors);
app.UseRouting();               // new add

// 修改部分：使用 UseEndpoints 并手动映射 SignalR Hub
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<BookContentHub>("/signalr-hubs/bookcontent");
    endpoints.MapControllers();
});

//app.UseAuthorization();         // new add
//app.MapControllers();           // new add
app.UseHttpsRedirection();
await app.InitializeApplicationAsync();             // init app
app.Run();


static void InitLog()
{
// init log
    Log.Logger = new LoggerConfiguration()
#if DEBUG
        .MinimumLevel.Debug()
#else
    .MinimumLevel.Information()
#endif
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Async(c => c.File(ApplicationConst.LogPath))
        .WriteTo.Async(c => c.Console())
        .CreateLogger();

    try
    {
        Log.Information("start web host.");
    }
    catch (Exception ex)
    {
        if (ex is HostAbortedException) throw;
        Log.Fatal(ex, "Host terminated unexpectedly!");
    }
    finally
    {
        Log.CloseAndFlush();
    }

}
// 以下是原生 dotnet 代码
    
// // Add services to the container.
// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

