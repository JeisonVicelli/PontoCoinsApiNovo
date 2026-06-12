using ProjetoPontos.Data; // Assumindo que seu DbContext está neste namespace
using Microsoft.EntityFrameworkCore; // Essencial para métodos de extensão do EF Core, como AddDbContext
using Microsoft.Extensions.DependencyInjection; // Necessário para IServiceCollection
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // NOVIDADE: Namespace correto para ServerVersion no .NET 8 com Pomelo
using Pomelo.EntityFrameworkCore.MySql.Storage; // NOVIDADE: Namespace para ServerVersion.AutoDetect (às vezes necessário, dependendo da versão)
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// --- Criação do Builder da Aplicação ---
var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do DbContext para MySQL
builder.Services.AddDbContext<LojaDbContext>(options =>
{
    // Obtém a string de conexão chamada "DefaultConnection" do seu appsettings.json.
    // Certifique-se de que esta string de conexão está configurada no seu appsettings.json
    // do NOVO projeto (PontoCoinsApiNovo).
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Configura o Entity Framework Core para usar MySQL.
    // Usamos o pacote Pomelo.EntityFrameworkCore.MySql para isso.
    // ServerVersion.AutoDetect tenta identificar a versão do seu MySQL para melhor compatibilidade.
    options.UseMySql(connectionString,
        new MySqlServerVersion(ServerVersion.AutoDetect(connectionString)), // Nova sintaxe para ServerVersion no .NET 8 com Pomelo
        mysqlOptions => mysqlOptions.EnableRetryOnFailure() // Opcional: Adiciona resiliência a falhas de conexão temporárias
    );
});

// 2. Adiciona suporte a controladores para APIs
builder.Services.AddControllers();
builder.Services.AddScoped<ProjetoPontos.Services.CashbackService>();
builder.Services.AddSingleton<ProjetoPontos.Services.TokenService>();
builder.Services.AddHttpClient<ProjetoPontos.Services.WhatsAppService>();
builder.Services.AddHostedService<ProjetoPontos.Services.AlertaExpiracaoService>();

// 2.1 Configuração de autenticação JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });
builder.Services.AddAuthorization();

// 3. Adiciona Endpoints API Explorer (necessário para Swagger/OpenAPI)
builder.Services.AddEndpointsApiExplorer();

// 4. Adiciona gerador Swagger/OpenAPI
builder.Services.AddSwaggerGen();

// 5. Configuração CORS (Cross-Origin Resource Sharing)
// A ordem aqui é importante, o Cors deve ser adicionado aos serviços.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin() // Permite requisições de qualquer origem
                  .AllowAnyHeader()  // Permite qualquer cabeçalho na requisição
                  .AllowAnyMethod(); // Permite qualquer método HTTP (GET, POST, PUT, DELETE, etc.)
        });
});

// --- Construção da Aplicação ---
var app = builder.Build();

// --- Configuração dos Middlewares (Pipeline de Requisições) ---

// 1. Configuração do Swagger/SwaggerUI para ambiente de Desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Habilita o middleware do Swagger
    app.UseSwaggerUI(); // Habilita o middleware do Swagger UI (interface gráfica)
}

// 2. Redireciona requisições HTTP para HTTPS (prática de segurança)
app.UseHttpsRedirection();

// 3. Habilita CORS (deve vir antes de UseAuthorization e MapControllers)
// Usamos a política padrão que definimos acima.
app.UseCors(); // Não passe parâmetros aqui se você configurou uma política padrão

// 4. Habilita os middlewares de autenticação e autorização (nesta ordem)
app.UseAuthentication();
app.UseAuthorization();

// 5. Mapeia os endpoints dos controladores (rotas da sua API)
app.MapControllers();

// --- Garante que o banco e as tabelas existem (cria se não existir) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LojaDbContext>();
    db.Database.EnsureCreated(); // cria todas as tabelas mapeadas se o banco estiver vazio
}

// --- Executa a Aplicação ---
app.Run();