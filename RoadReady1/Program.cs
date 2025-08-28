// Program.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoadReady1.Context;
using RoadReady1.Filters;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Repositories;
using RoadReady1.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- DbContext ----------
builder.Services.AddDbContext<RoadReadyDbContext>(opts =>
    opts.UseSqlServer(config.GetConnectionString("defaultConnection")));

// ---------- Identity helpers ----------
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ---------- AutoMapper ----------
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ---------- Repositories ----------
builder.Services.AddScoped<IRepository<int, User>, UserRepository>();
builder.Services.AddScoped<IRepository<int, Role>, RoleRepository>();
builder.Services.AddScoped<IRepository<int, Car>, CarRepository>();
builder.Services.AddScoped<IRepository<int, CarBrand>, CarBrandRepository>();
builder.Services.AddScoped<IRepository<int, CarStatus>, CarStatusRepository>();
builder.Services.AddScoped<IRepository<int, Booking>, BookingRepository>();
builder.Services.AddScoped<IRepository<int, BookingStatus>, BookingStatusRepository>();
builder.Services.AddScoped<IRepository<int, Location>, LocationRepository>();
builder.Services.AddScoped<IRepository<int, Payment>, PaymentRepository>();
builder.Services.AddScoped<IRepository<int, PaymentMethod>, PaymentMethodRepository>();
builder.Services.AddScoped<IRepository<int, Refund>, RefundRepository>();
builder.Services.AddScoped<IRepository<int, Review>, ReviewRepository>();
builder.Services.AddScoped<IRepository<int, MaintenanceRequest>, MaintenanceRequestRepository>();
builder.Services.AddScoped<IRepository<int, BookingIssue>, BookingIssueRepository>();

// ---------- Domain Services ----------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMaintenanceRequestService, MaintenanceRequestService>();
builder.Services.AddScoped<IBookingIssueService, BookingIssueService>();

// ---------- CORS ----------
const string MyCors = "DefaultCORS";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyCors, p => p
        .WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001",
            "http://localhost:3002" // your current React origin
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    // Only add .AllowCredentials() if you use cookies; if you do, keep explicit origins
    // .AllowCredentials()
    );
});

// ---------- JWT ----------
var jwtSection = config.GetSection("JwtSettings");
var secret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");
if (secret.Length < 32) throw new InvalidOperationException("Jwt SecretKey must be at least 32 characters.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services
    .AddAuthentication(o =>
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
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ---------- MVC + Filters ----------
builder.Services.AddControllers(o =>
{
    o.Filters.Add<CustomExceptionFilter>();
});

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RoadReady API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste your JWT token here (without the 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- Seed Roles (and optional Admin) ----------
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoadReadyDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    db.Database.EnsureCreated();

    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "RentalAgent" },
            new Role { RoleId = 3, RoleName = "Customer" }
        );
        db.SaveChanges();
    }

    // OPTIONAL: create a default admin if none exists
    if (!db.Users.Any(u => u.Email == "admin@roadready.local"))
    {
        var admin = new User
        {
            FirstName = "System",
            LastName = "Admin",
            Email = "admin@roadready.local",
            PhoneNumber = "0000000000",
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@12345");
        db.Users.Add(admin);
        db.SaveChanges();
    }
}

var app = builder.Build();

// ---------- Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Do NOT force HTTPS in dev if your frontend is running on http:// (avoids mixed-content/CORS traps)
// app.UseHttpsRedirection();

app.UseCors(MyCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
