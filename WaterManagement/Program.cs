using Microsoft.EntityFrameworkCore;
using WaterManagement.Data;
using WaterManagement.Handlers;
using WaterManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database context
builder.Services.AddDbContext<WaterQualityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<WaterQualityService>();

// Background service - ALLEEN als HostedService, NIET als Scoped
builder.Services.AddHostedService<ShipMessageHandler>();

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


InitializeDatabase(app);

app.Run();

void InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WaterQualityDbContext>();
    try
    {
        db.Database.Migrate(); // Gebruik Migrate() voor productie
        Console.WriteLine("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}

// // Database initialization
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<WaterQualityDbContext>();
//     try
//     {
//         db.Database.EnsureCreated();
//         Console.WriteLine("Database initialized successfully");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Database initialization failed: {ex.Message}");
//     }
// }

// app.Run();