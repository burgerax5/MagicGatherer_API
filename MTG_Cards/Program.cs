using Microsoft.EntityFrameworkCore;
using MTG_Cards.Data;
using MTG_Cards.Interfaces;
using MTG_Cards.Repositories;
using MTG_Cards.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAuthentication("auth").AddCookie("auth", options =>
{
    options.Cookie.Name = "auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<IEditionRepository, EditionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddHttpClient();
builder.Services.AddTransient<IScryfallAPI, ScryfallAPI>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
