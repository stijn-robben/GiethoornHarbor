using HarborManagementService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddHostedService<SimpleTestSubscriberService>();

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddDbContext<HarborContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HarborContext>();

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

app.Run();
