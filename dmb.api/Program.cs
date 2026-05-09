using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Dmb.Data.Context;
using Dmb.Data.Mapper;
using Dmb.Data.Repository.Implementation;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Abstractions;
using Dmb.Service.Implementation;
using Dmb.Service.Interface;
using Dmb.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;

// Npgsql 7+ enforces UTC for timestamptz; allow legacy behavior to avoid Kind=Unspecified failures
// from date-only inputs (e.g. "YYYY-MM-DD") until the domain model is migrated to DateOnly/DateTimeOffset.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddMemoryCache();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "dmbapp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "dmbapp";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Missing jti.");
                    return;
                }

                // IMPORTANT: Do not call the database from auth middleware.
                // DB timeouts/cancellation can cause random 401s or disposed-object errors with poolers.
                // We only enforce in-memory revocations here (fast + reliable).
                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                if (cache.TryGetValue($"revoked_jti:{jti}", out bool isRevoked) && isRevoked)
                {
                    context.Fail("Token has been revoked.");
                }

            }
        };
    });

builder.Services.AddAuthorization();

// Register MVC controllers so Controllers/ProfileController.cs is discovered
builder.Services.AddControllers();

// Register DbContext - example using environment connection string "ConnectionStrings:DmbDb"
//var connectionString = builder.Configuration.GetConnectionString("DmbDb") ?? builder.Configuration["ConnectionStrings:DmbDb"];
//if (!string.IsNullOrEmpty(connectionString))
//{
//    // Choose provider (Npgsql for PostgreSQL) - ensure package is added to the projects using the provider
//        builder.Services.AddDbContext<DmbDbContext>(options =>
//        options.UseNpgsql(connectionString));
//}




var connectionString = builder.Configuration.GetConnectionString("DmbDb")
    ?? throw new InvalidOperationException("Connection string 'DmbDb' is not configured.");

builder.Services.AddDbContext<DmbDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    }));




var mapperLoggerFactory = LoggerFactory.Create(logging => { });
var mapperConfiguration = new MapperConfiguration(configuration =>
{
    configuration.AddProfile<DmbDetailsMapperProfile>();
}, mapperLoggerFactory);

builder.Services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<IDmbReadRepository, DmbReadRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<EmailTemplateProvider>();
builder.Services.AddScoped<SmtpHtmlEmailService>();
builder.Services.AddHttpClient<ResendHttpEmailService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<EmailService>());
builder.Services.AddScoped<IActivationEmailSender>(sp => sp.GetRequiredService<EmailService>());
builder.Services.AddScoped<IPasswordResetEmailSender>(sp => sp.GetRequiredService<EmailService>());
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDmbReadService, DmbReadService>();

var app = builder.Build();

// Startup safety patch for environments where schema scripts/migrations
// have not yet been applied after deploying new model fields.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DmbDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupSchemaPatch");
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "User"
                ADD COLUMN IF NOT EXISTS "IsViewable" BOOLEAN NOT NULL DEFAULT FALSE;
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "User"
                ADD COLUMN IF NOT EXISTS "Address" VARCHAR(255);
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_User_Username_FirstName_LastName"
            ON "User" ("Username", "FirstName", "LastName");
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "WorkHistory"
            (
                "WorkHistoryId" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "UserId"        INTEGER NOT NULL,
                "Company"       VARCHAR(200) NOT NULL,
                "Position"      VARCHAR(200) NOT NULL,
                "FromDate"      DATE,
                "ToDate"        DATE,
                "JobDescription" TEXT,
                "CreatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT "FK_WorkHistory_User"
                    FOREIGN KEY ("UserId") REFERENCES "User" ("UserId")
                    ON DELETE CASCADE
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Education"
            (
                "EducationId" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "UserId"      INTEGER NOT NULL,
                "School"      VARCHAR(200) NOT NULL,
                "Address"     VARCHAR(255),
                "CourseTaken" VARCHAR(255),
                "StartDate"   DATE,
                "EndDate"     DATE,
                "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT "FK_Education_User"
                    FOREIGN KEY ("UserId") REFERENCES "User" ("UserId")
                    ON DELETE CASCADE
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Affiliation"
            (
                "AffiliationId" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "UserId"        INTEGER NOT NULL,
                "Organization"  VARCHAR(200) NOT NULL,
                "Title"         VARCHAR(200) NOT NULL,
                "IssueDate"     DATE,
                "Details"       TEXT NOT NULL,
                "CreatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT "FK_Affiliation_User"
                    FOREIGN KEY ("UserId") REFERENCES "User" ("UserId")
                    ON DELETE CASCADE
            );
            """);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Startup schema patch for resume/public profile fields failed.");
    }
}

app.UseForwardedHeaders();

var configuredPathBase = builder.Configuration["App:PathBase"]
    ?? Environment.GetEnvironmentVariable("APP_PATH_BASE");
if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    configuredPathBase = configuredPathBase.Trim();
    if (!configuredPathBase.StartsWith('/'))
    {
        configuredPathBase = "/" + configuredPathBase;
    }

    configuredPathBase = configuredPathBase.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(configuredPathBase))
    {
        app.UsePathBase(configuredPathBase);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Map controllers with attribute routing
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
