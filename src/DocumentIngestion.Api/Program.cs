using DocumentIngestion.Api.Data;
using DocumentIngestion.Api.Services;
using DocumentIngestion.Api.Strategies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure OpenAPI (Swagger) support
builder.Services.AddOpenApi();

// Register SQLite Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=documents.db"));

// Register Ingestion Strategies
builder.Services.AddSingleton<IIngestionStrategy, TxtIngestionStrategy>();
builder.Services.AddSingleton<IIngestionStrategy, PdfIngestionStrategy>();
builder.Services.AddSingleton<IIngestionStrategy, DocxIngestionStrategy>();

// Register Image Ingestion for standard types (png, jpeg)
builder.Services.AddSingleton<IIngestionStrategy>(sp => new ImageIngestionStrategy("image/png"));
builder.Services.AddSingleton<IIngestionStrategy>(sp => new ImageIngestionStrategy("image/jpeg"));

// Register Strategy Resolver and Business Logic Services
builder.Services.AddSingleton<IngestionStrategyResolver>();
builder.Services.AddSingleton<IMetadataExtractor, MetadataExtractor>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddSingleton<SecurityHelper>();
builder.Services.AddHttpClient<GeminiFallbackService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure the database is created and schema applied at startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        Console.WriteLine("SQLite Database initialized and verified successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRITICAL ERROR: Failed to initialize SQLite database: {ex.Message}");
    }
}

app.Run();
