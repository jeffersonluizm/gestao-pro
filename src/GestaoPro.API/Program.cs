using GestaoPro.API.Controllers;
using GestaoPro.API.Middleware;
using GestaoPro.API.Repositories;
using GestaoPro.API.Services;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<IMongoClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton(provider =>
{
    var settings = provider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var client = provider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

// Os segredos não ficam no appsettings.json: sem eles a API não sobe (ver README, "Configuração da API").
if (string.IsNullOrWhiteSpace(builder.Configuration["MongoDbSettings:ConnectionString"]))
    throw new InvalidOperationException(
        "Connection string do MongoDB ausente. Defina MongoDbSettings__ConnectionString ou crie appsettings.Local.json.");
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "Chave JWT ausente ou com menos de 32 caracteres. Defina JwtSettings__SecretKey ou crie appsettings.Local.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
        // WebSocket do navegador não envia cabeçalho: o cliente SignalR manda o token em access_token.
        // Aceito só no caminho do hub, para não expor tokens em URLs do resto da API.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/notificacaoHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<ICommonAreaRepository, CommonAreaRepository>();
builder.Services.AddSingleton<IVisitorRepository, VisitorRepository>();
builder.Services.AddSingleton<IParkingRepository, ParkingRepository>();
builder.Services.AddSingleton<IPackageRepository, PackageRepository>();
builder.Services.AddSingleton<IAssemblyRepository, AssemblyRepository>();
builder.Services.AddSingleton<IVoteRepository, VoteRepository>();
builder.Services.AddSingleton<IProxyVoteRepository, ProxyVoteRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IBillRepository, BillRepository>();
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<UnitRepository>();
builder.Services.AddSingleton<CondominiumRepository>();
builder.Services.AddSingleton<PetRepository>();
builder.Services.AddSingleton<CredentialRepository>();
builder.Services.AddSingleton<IAdministratorLookup>(provider => provider.GetRequiredService<CredentialRepository>());
builder.Services.AddSingleton<OccurrenceRepository>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<SmsService>();

builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ICommonAreaService, CommonAreaService>();
builder.Services.AddScoped<VisitorAppService>();
builder.Services.AddScoped<ParkingAppService>();
builder.Services.AddScoped<PackageAppService>();
builder.Services.AddScoped<CondominiumAppService>();
builder.Services.AddScoped<ICondominiumService>(provider => provider.GetRequiredService<CondominiumAppService>());
builder.Services.AddScoped<UnitAppService>();
builder.Services.AddScoped<IUnitService>(provider => provider.GetRequiredService<UnitAppService>());
builder.Services.AddScoped<UserAppService>();
builder.Services.AddScoped<IUserDirectory>(provider => provider.GetRequiredService<UserAppService>());
builder.Services.AddScoped<PetAppService>();
builder.Services.AddScoped<IPetService>(provider => provider.GetRequiredService<PetAppService>());
builder.Services.AddScoped<AuthAppService>();
builder.Services.AddScoped<ISelfServiceAccounts, MongoSelfServiceAccounts>();
builder.Services.AddScoped<SelfServiceAuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OccurrenceAppService>();
builder.Services.AddScoped<IOccurrenceService>(provider => provider.GetRequiredService<OccurrenceAppService>());
builder.Services.AddScoped<IAssemblyService, AssemblyService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddScoped<IDeliveryService>(provider => provider.GetRequiredService<DeliveryService>());
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddHostedService<VoteIndexInitializer>();
// Preenche o condomínio de visitantes e vagas antigos (em segundo plano; falha só gera log).
builder.Services.AddSingleton<ILegacyCondominiumStore, MongoLegacyCondominiumStore>();
builder.Services.AddScoped<LegacyCondominiumBackfill>();
builder.Services.AddHostedService<LegacyCondominiumBackfillInitializer>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Rotas anônimas de autenticação (login, check-cpf, register, change-password): poucas tentativas por IP
// em janela fixa, contra força bruta e varredura de CPF. Acima do limite: 429.
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
var authWindowSeconds = builder.Configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthRateLimit.Policy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "sem-ip",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromSeconds(authWindowSeconds),
            QueueLimit = 0
        }));
    options.OnRejected = async (context, cancellationToken) =>
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Muitas tentativas. Aguarde um minuto e tente de novo." }, cancellationToken);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// A conta admin de demonstração tem senha conhecida: só existe em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ApiAccessMiddleware>();
app.UseAuthorization();
app.MapControllers();
// Conexão aberta não sobrevive ao JWT: ao expirar, o cliente precisa reconectar com token novo.
app.MapHub<NotificacaoHub>("/notificacaoHub", options => options.CloseOnAuthenticationExpiration = true)
    .RequireAuthorization();

app.Run();
