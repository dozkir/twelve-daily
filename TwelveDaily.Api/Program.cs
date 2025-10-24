using Microsoft.EntityFrameworkCore;
using DotNetEnv;

using TwelveDaily.Api.Models;
using TwelveDaily.Api.Data;

var builder = WebApplication.CreateBuilder(args);

Env.Load(); // Carrega variáveis de ambiente do arquivo .env

builder.Services.AddControllers(); // Registring Controllers
builder.Services.AddEndpointsApiExplorer(); // Add services to the container. Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen();

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // O que isso faz?
app.UseAuthorization(); // O que isso faz?
app.MapControllers(); // Ativando os Controllers

// app.MapGet("/testeUsuario", () =>
// {
//     var user = User.test();
//     return user;
// })
// .WithName("TesteUsuario")
// .WithOpenApi();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.CanConnect())
            Console.WriteLine("✅ Conexão com banco OK!");
        else
            Console.WriteLine("❌ Não foi possível conectar ao banco.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Erro ao conectar no banco: " + ex.Message);
    }
}

app.Run();