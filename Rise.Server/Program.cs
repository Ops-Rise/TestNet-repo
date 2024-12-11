using System.Reflection;
using System.Security.Claims;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mollie.Api;
using Mollie.Api.Framework;
using Rise.Persistence;
using Rise.Persistence.Triggers;
using Rise.Server.Middleware;
using Rise.Services.Batteries;
using Rise.Services.Batteries.Services;
using Rise.Services.Boats;
using Rise.Services.Bookings;
using Rise.Services.Emails;
using Rise.Services.Payments;
using Rise.Services.Prices;
using Rise.Services.ProfileImages;
using Rise.Services.TimeSlots;
using Rise.Services.Users;
using Rise.Services.Weather;
using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Payments;
using Rise.Shared.Prices;
using Rise.Shared.ProfileImages;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;
using Rise.Shared.Weather;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Load environment variables from a .env file if it exists
Env.Load();

// Add environment variables to the configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    // Aangepaste schema ID configuratie om unieke namen te genereren
    c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
});

builder.Services.AddMollieApi(options =>
{
    options.ApiKey = builder.Configuration["ApiToken_PaymentProvider"] ?? "Defaultkey";
    options.RetryPolicy = MollieHttpRetryPolicies.TransientHttpErrorRetryPolicy();
});

//exceptionHandlers
builder.Services.AddExceptionHandler<UnauthorizedAccessExceptionHandler>();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<MollieApiExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier,
        };
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
    options.UseTriggers(options => options.AddTrigger<EntityBeforeSaveTrigger>());
});

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBoatService, BoatService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();

// custom authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, OwnDataOrAdminHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "OwnDataOrAdmin",
        policy => policy.Requirements.Add(new OwnDataOrAdminRequirement())
    );
});
builder.Services.AddScoped<IBatteryService, BatteryService>();

builder.Services.AddScoped<IBatteryAndBoatAssignmentProcessor, BatteryAndBoatAssignmentProcessor>();
builder.Services.AddHostedService<BatteryAndBoatAssignmentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IPaymentService, DefaultPaymentService>();
}
else
{
    builder.Services.AddScoped<IPaymentService, MolliePaymentService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            dbContext.Database.Migrate();

            Seeder seeder = new(dbContext);
            seeder.Seed();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while setting up the database: {ex.Message}");
        }
    }
}


app.UseHttpsRedirection();


app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.UseExceptionHandler();

/*Seeder*/
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    using (var scope = app.Services.CreateScope())
    { // Require a DbContext from the service provider and seed the database.
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Seeder seeder = new(dbContext);
        seeder.Seed();
    }
}

// This enables all authorization on every endpoint, so no more use without authorization
app.MapControllers().RequireAuthorization();

app.MapHealthChecks("/healthcheck");
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Logger.LogInformation("Starting application");
await app.RunAsync(); //  SonarFeedback csharpsquid:S6966

// for Integration tests
public partial class Program { }
