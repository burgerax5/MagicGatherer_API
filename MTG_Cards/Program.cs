using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using MTG_Cards.Data;
using MTG_Cards.Interfaces;
using MTG_Cards.Repositories;
using MTG_Cards.Services;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

DotNetEnv.Env.Load();
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var key = Encoding.ASCII.GetBytes(secretKey ?? "not_so_secret_key");

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
	options.RequireHttpsMetadata = false;
	options.SaveToken = true;
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(key),
		ValidateIssuer = false,
		ValidateAudience = false,
		ValidateLifetime = true,
		ClockSkew = TimeSpan.Zero
	};
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    var connection = builder.Configuration.GetConnectionString("Redis");
    options.Configuration = connection;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	var configuration = builder.Configuration.GetConnectionString("Redis");
	return ConnectionMultiplexer.Connect(configuration!);
});

builder.Services.AddTransient<ICacheHelper, CacheHelper>();

builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<IEditionRepository, EditionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
        });
});
builder.Services.AddStackExchangeRedisCache(option =>
{
	option.Configuration = builder.Configuration["Redis"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowSpecificOrigin");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
