using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using TwelveDaily.Core.Application.Habits;
using TwelveDaily.Core.Application.Habits.CreateHabit;
using TwelveDaily.Core.Application.Habits.GetAllUserHabits;
using TwelveDaily.Core.Application.Habits.GetHabitById;
using TwelveDaily.Core.Application.Habits.UpdateHabit;
//using TwelveDaily.Core.Application.Habits.DeleteHabit;
using TwelveDaily.Core.Application.Interfaces;
using TwelveDaily.Core.Domains.Habits;
using TwelveDaily.Core.Infrastructure.Data;
using TwelveDaily.Core.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

Env.Load(); // Carrega variáveis de ambiente do arquivo .env

builder.Services.AddControllers(); // Registring Controllers
builder.Services.AddEndpointsApiExplorer(); // Add services to the container. Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen();

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddScoped<CreateHabitHandler>();
builder.Services.AddScoped<GetAllUserHabitsHandler>();
builder.Services.AddScoped<GetHabitByIdHandler>();
builder.Services.AddScoped<UpdateHabitHandler>();
//builder.Services.AddScoped<DeleteHabitHandler>();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IHabitRepository, HabitRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

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