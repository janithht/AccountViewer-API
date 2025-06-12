using AccountsViewer.Data;
using AccountsViewer.Repositories;
using AccountsViewer.Repositories.Interfaces;
using AccountsViewer.Services;
using AccountsViewer.Services.Interfaces;
using Azure.Storage.Blobs;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy
          .WithOrigins("https://happy-beach-04dd4c010.6.azurestaticapps.net")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        options => options.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    )
);

var blobConn = builder.Configuration.GetConnectionString("BlobStorage");
var containerName = builder.Configuration["BlobContainerName"];

builder.Services.AddSingleton(sp =>
    new BlobContainerClient(blobConn, containerName)
);

builder.Services.AddScoped<IUploadService, UploadService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IMonthlyBalanceRepository, MonthlyBalanceRepository>();
builder.Services.AddScoped<IUploadAuditRepository, UploadAuditRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();

builder.Services.AddControllers();

// JWT Authentication
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowAngularDev"); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
