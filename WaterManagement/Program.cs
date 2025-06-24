using Microsoft.EntityFrameworkCore;
using WaterManagement.Data;
using WaterManagement.Handlers;
using WaterManagement.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<ShipMessageHandler>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<WaterQualityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<WaterQualityService>();
builder.Services.AddScoped<ShipMessageHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WaterQualityDbContext>();
    db.Database.EnsureCreated(); // Of db.Database.Migrate() als je migrations gebruikt
}

app.Run();
