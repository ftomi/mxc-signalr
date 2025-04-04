using MongoDB.Driver;
using SignalRChatAppBackend.Hubs;
using SignalRChatAppBackend.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
    new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton<MessageService>();
builder.Services.AddSignalR();
builder.Services.AddControllers();

// Register ChatHub as a service
builder.Services.AddSingleton<ChatHub>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Swagger services
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Chat API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat API V1");
    });
}

app.UseRouting();

// Use CORS middleware
app.UseCors();

app.MapHub<ChatHub>("/chathub");
app.MapControllers();

app.Run();