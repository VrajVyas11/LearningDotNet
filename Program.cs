using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LearningDotNet", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 👇 Put these in correct order
app.UseDefaultFiles();  // Enables serving index.html at root "/"
app.UseStaticFiles();   // Serves static files like index.html

app.UseAuthorization(); // Only if you need it

app.MapControllers();   // Map your API controllers

app.Run();
