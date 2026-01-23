using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using MobyPark.Data;
using MobyPark.Services;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

using Scalar.AspNetCore;

QuestPDF.Settings.License = LicenseType.Community;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi(opts =>
{
    opts.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Components ??= new();
        doc.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        doc.Components.SecuritySchemes["BearerAuth"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "JWT Bearer token"
        };

        return Task.CompletedTask;
    });

    opts.AddOperationTransformer((op, ctx, ct) =>
    {
        var metadata = ctx.Description?.ActionDescriptor?.EndpointMetadata;
        var requiresAuth = metadata?.OfType<IAuthorizeData>()?.Any() == true;

        if (requiresAuth)
        {
            op.Security ??= new List<OpenApiSecurityRequirement>();
            op.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "BearerAuth" }
                }] = Array.Empty<string>()
            });
        }
        else
        {
            op.Security = new List<OpenApiSecurityRequirement>();
        }

        return Task.CompletedTask;
    });
});

var cs = builder.Configuration.GetConnectionString("UserDatabase")
         ?? throw new InvalidOperationException("Missing ConnectionStrings:UserDatabase");

var issuer = builder.Configuration["AppSettings:Issuer"]
            ?? throw new InvalidOperationException("Missing AppSettings:Issuer");

var audience = builder.Configuration["AppSettings:Audience"]
              ?? throw new InvalidOperationException("Missing AppSettings:Audience");

var tokenKey = builder.Configuration["AppSettings:Token"]
              ?? throw new InvalidOperationException("Missing AppSettings:Token");

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(cs));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey))
        };
    });

builder.Services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IParkingLotService, ParkingLotService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<DataMigrationService>();


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5280);
    options.ListenLocalhost(7197, listenOptions => listenOptions.UseHttps());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
