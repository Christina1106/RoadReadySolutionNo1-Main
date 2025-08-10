using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Repositories;
using RoadReady1.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// 1) EF Core
builder.Services.AddDbContext<RoadReadyDbContext>(opts =>
    opts.UseSqlServer(config.GetConnectionString("defaultConnection")));

// 2) CORS (dev)
builder.Services.AddCors(o =>
{
    o.AddPolicy("DevAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// 3) Identity utils
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// 4) Repositories (concrete per entity)
builder.Services.AddScoped<IRepository<int, User>, UserRepository>();
builder.Services.AddScoped<IRepository<int, Car>, CarRepository>();
builder.Services.AddScoped<IRepository<int, Booking>, BookingRepository>();
builder.Services.AddScoped<IRepository<int, BookingStatus>, BookingStatusRepository>();
builder.Services.AddScoped<IRepository<int, Location>, LocationRepository>();
builder.Services.AddScoped<IRepository<int, Payment>, PaymentRepository>();
builder.Services.AddScoped<IRepository<int, PaymentMethod>, PaymentMethodRepository>();
builder.Services.AddScoped<IRepository<int, Refund>, RefundRepository>();
builder.Services.AddScoped<IRepository<int, Review>, ReviewRepository>();
builder.Services.AddScoped<IRepository<int, MaintenanceRequest>, MaintenanceRequestRepository>();
builder.Services.AddScoped<IRepository<int, BookingIssue>, BookingIssueRepository>();

// 5) Domain services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, ConsoleEmailService>(); // swap later to SendGrid/Smtp
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMaintenanceRequestService, MaintenanceRequestService>();
builder.Services.AddScoped<IBookingIssueService, BookingIssueService>();

builder.Services.AddScoped<IRepository<int, Role>, RoleRepository>();
builder.Services.AddScoped<IRepository<int, PasswordResetToken>, PasswordResetTokenRepository>();
builder.Services.AddScoped<IRepository<int, CarBrand>, CarBrandRepository>();

builder.Services.AddScoped<IRepository<int, CarStatus>, CarStatusRepository>();

// 6) AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 7) JWT
var jwt = config.GetSection("JwtSettings");
var secret = jwt["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");
if (secret.Length < 32) throw new InvalidOperationException("Jwt SecretKey must be at least 32 chars.");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 8) Controllers + Swagger (Bearer)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RoadReady1", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the raw JWT here (no 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("RoadReady API started...");

app.Run();

