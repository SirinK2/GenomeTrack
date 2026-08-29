using System.Text;
using GenomeTrack.API.Authorization;
using GenomeTrack.API.Middleware;
using GenomeTrack.Application;
using GenomeTrack.Application.Constants;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Domain.Enums;
using GenomeTrack.Infrastructure;
using GenomeTrack.Infrastructure.Repository;
using GenomeTrack.Infrastructure.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, config) => config.ReadFrom.Configuration(context.Configuration).WriteTo.Console()
);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

// Fail at startup rather than at the first login. A missing signing key that surfaces as a
// 500 an hour after deploy is far more expensive to diagnose than a container that refuses
// to start and says why.
if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
    throw new InvalidOperationException("Jwt:SigningKey is not configured.");

if (jwtSettings.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters for HS256.");

builder.Services.Configure<JwtSettings>(jwtSection);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<ITokenIssuer, TokenIssuer>();

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey)
            ),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// The roles are a ladder, so each policy admits its own tier and everything above it. Listing
// the tiers explicitly beats a numeric comparison here because the policy names then read the
// same way in the controller as they do in the requirement.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(
        LabPolicy.TechnicianOrAbove,
        policy =>
            policy.RequireRole(
                nameof(LabRole.Technician),
                nameof(LabRole.Analyst),
                nameof(LabRole.PrincipalInvestigator)
            )
    )
    .AddPolicy(
        LabPolicy.AnalystOrAbove,
        policy => policy.RequireRole(nameof(LabRole.Analyst), nameof(LabRole.PrincipalInvestigator))
    )
    .AddPolicy(
        LabPolicy.PrincipalInvestigatorOnly,
        policy => policy.RequireRole(nameof(LabRole.PrincipalInvestigator))
    );

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "GenomeTrack API",
            Version = "v1",
            Description =
                "Sample chain of custody, sequencing runs, and variant calling for a genomics lab.",
        }
    );

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme,
        },
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

    var xml = Path.Combine(AppContext.BaseDirectory, "GenomeTrack.API.xml");
    if (File.Exists(xml))
        options.IncludeXmlComments(xml);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// The authentication and authorization middleware short-circuit with an empty 401/403 long
// before a controller runs, so those two responses would be the only ones in the API without
// the Result envelope. A client should not need a second parse path for exactly the cases it
// is most likely to hit.
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
        return;

    if (response.ContentLength > 0 || response.HasStarted)
        return;

    response.ContentType = "application/json";

    var message =
        response.StatusCode == StatusCodes.Status401Unauthorized
            ? "Authentication is required."
            : "Your role does not permit this action.";

    await response.WriteAsJsonAsync(GenomeTrack.Application.Response.Result.Failure(message));
});

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "GenomeTrack API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    await LabUserSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
        app.Environment.IsDevelopment()
    );
}

app.Run();

/// <summary>Exposed so the integration tests can drive the app through WebApplicationFactory.</summary>
public partial class Program { }
