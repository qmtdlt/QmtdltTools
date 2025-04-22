using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Polly;
using QmtdltTools;
using QmtdltTools.Domain.Data;
using QmtdltTools.Hubs;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Volo.Abp.AspNetCore.SignalR;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

const string Cors = "VueApp";
InitLog();
var builder = WebApplication.CreateBuilder(args);           // got a web application builder

builder.Host
    .AddAppSettingsSecretsJson()        // add appsettings.json
    .UseAutofac()                       // use autofac
    .UseSerilog();                      // use serilog

//builder.Services.AddOpenApi();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
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
// jwt 

ApplicationConst.SPEECH_KEY = configuration.GetSection("MySecret:SPEECH_KEY").Get<string>();
ApplicationConst.SPEECH_REGION = configuration.GetSection("MySecret:SPEECH_REGION").Get<string>();
ApplicationConst.GROK_KEY = configuration.GetSection("MySecret:GROK_KEY").Get<string>();
ApplicationConst.GEMINI_KEY = configuration.GetSection("MySecret:GEMINI_KEY").Get<string>();
ApplicationConst.DOU_BAO = configuration.GetSection("MySecret:DOU_BAO").Get<string>();
ApplicationConst.QIAN_WEN = configuration.GetSection("MySecret:QIAN_WEN").Get<string>();

var Issuer = configuration.GetSection("Jwt:Issuer").Get<string>();
var Audience = configuration.GetSection("Jwt:Audience").Get<string>(); 
var SystenScurityKey = configuration.GetSection("Jwt:SystenScurityKey").Get<string>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,              // 验证发行者
        ValidateAudience = true,            // 验证受众
        ValidateLifetime = true,            // 验证 token 有效期
        ValidateIssuerSigningKey = true,    // 验证签名密钥
        ValidIssuer = Issuer,               // 指定发行者
        ValidAudience = Audience,           // 指定受众
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SystenScurityKey)) // 签名密钥
    };
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
app.UseAuthentication();
app.UseAuthorization();         // new add

// 修改部分：使用 UseEndpoints 并手动映射 SignalR Hub
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<BookContentHub>("/signalr-hubs/bookcontent");
    endpoints.MapControllers();
});

//app.MapControllers();           // new add
app.UseHttpsRedirection();
await app.InitializeApplicationAsync();             // init app

RedisHelper.Initialization(new CSRedis.CSRedisClient("127.0.0.1:6379"));

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


internal sealed class BearerSecuritySchemeTransformer(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                });
            }
        }
    }
}