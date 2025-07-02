using ShipService.Messaging;
using ShipService.Services;
using ShipService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ShipServiceContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)
    )
);

builder.Services.AddHostedService<RabbitMqSubscriberService>();
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddScoped<ShipServiceManager>();
builder.Services.AddScoped<RefuelService>();
builder.Services.AddScoped<ElectricityService>();
builder.Services.AddScoped<UnloadLoadService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShipServiceContext>();

    int retries = 0;
    while (true)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception ex)
        {
            retries++;
            if (retries >= 5)
                throw new Exception("Failed to connect to SQL Server after 5 attempts", ex);

            Console.WriteLine($"[Startup] SQL Server not ready yet. Retrying... ({retries}/5)");
            Thread.Sleep(2000);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
