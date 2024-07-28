using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MTG_Cards.Data;
using MTG_Cards.Interfaces;
using MTG_Cards.Repositories;
using MTG_Cards.Services;
using StackExchange.Redis;
using System.Text;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


builder.Configuration.AddAzureKeyVault(new Uri("https://mtgcardsvault.vault.azure.net/"), new DefaultAzureCredential());

builder.Services.AddDbContext<DataContext>(options =>
{
	options.UseSqlServer(builder.Configuration["DbConnection"]);
});

var key = Encoding.ASCII.GetBytes(builder.Configuration["JWTKey"]!);

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
    var connection = builder.Configuration["Redis"];
    options.Configuration = connection;
});



builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	var configuration = builder.Configuration["Redis"];
	return ConnectionMultiplexer.Connect(configuration!);
});

builder.Services.AddTransient<ICacheHelper, CacheHelper>();

builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<IEditionRepository, EditionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<MailService>();

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
			string[] origins = ["https://magicgatherer.netlify.app"];
			//origins.Append("http://localhost:5173");

			builder
                    .WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
        });
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