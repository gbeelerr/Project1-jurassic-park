var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy.WithOrigins("http://localhost:5044", "https://localhost:7022")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.UseCors("AllowWeb");

app.MapHealthChecks("/api/health");

var movies = new[]
{
    new Movie(1, "Jurassic Park", 127),
    new Movie(2, "The Lost World: Jurassic Park", 129),
    new Movie(3, "Jurassic Park III", 92),
    new Movie(4, "Jurassic World", 124),
    new Movie(5, "Jurassic World: Fallen Kingdom", 128),
    new Movie(6, "Jurassic World Dominion", 147)
};

app.MapGet("/weatherforecast", () =>
{
    return new { message = "API is running" };
});

app.MapGet("/movies", () => movies);

app.Run();

record Movie(int Id, string Title, int Runtime);