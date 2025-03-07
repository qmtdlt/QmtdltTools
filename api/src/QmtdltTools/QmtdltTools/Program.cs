using QmtdltTools;
using QmtdltTools.Domain.Data;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);           // got a web application builder
// 使用 abp

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
    builder.Host
        .AddAppSettingsSecretsJson()        // add appsettings.json
        .UseAutofac()                       // use autofac
        .UseSerilog();                      // use serilog
    
    await builder.AddApplicationAsync<QmtdltToolsAPIModule>();  // add application
    var app = builder.Build();              // build app

    //app.MapStaticAssets();                              // map static assets

    await app.InitializeApplicationAsync();             // init app
    await app.RunAsync();                               // run app
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
// 以下是原生 dotnet 代码
    
// // Add services to the container.
// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
// builder.Services.AddControllers();              // new add
// builder.Services.AddEndpointsApiExplorer();     // new add 
//     
// var app = builder.Build();
//
// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
//     app.MapScalarApiReference();
// }
// app.UseRouting();               // new add
// //app.UseAuthorization();         // new add
// app.MapControllers();           // new add
// app.UseHttpsRedirection();
// app.Run();
