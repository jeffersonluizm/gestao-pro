using GestaoPro.API.Controllers;
using GestaoPro.API.Middleware;
using GestaoPro.API.Repositories;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Demo;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Segredos locais com prioridade abaixo das variáveis de ambiente e da linha de comando (ver LocalSettings).
builder.Configuration.AddLocalSettingsFile(builder.Environment.ContentRootPath);

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
builder.Services.AddSingleton<IAdministratorLookup>(provider =>
    new CachedAdministratorLookup(provider.GetRequiredService<CredentialRepository>()));
builder.Services.AddSingleton<OccurrenceRepository>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<SmsService>();

builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddSingleton<IReservationSlotLock, MongoReservationSlotLock>();
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
builder.Services.AddScoped<IAuthAccounts, MongoAuthAccounts>();
builder.Services.AddScoped<AuthAppService>();
builder.Services.AddScoped<ISelfServiceAccounts, MongoSelfServiceAccounts>();
builder.Services.AddSingleton<IPasswordVerifier, BcryptPasswordVerifier>();
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
builder.Services.AddSingleton(DemoCatalog.FromConfiguration(builder.Configuration));
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<DemoDataCleaner>();
builder.Services.AddHostedService<DemoResetService>();
builder.Services.AddHostedService<VoteIndexInitializer>();
// Preenche o condomínio de visitantes e vagas antigos (em segundo plano; falha só gera log).
builder.Services.AddSingleton<ILegacyCondominiumStore, MongoLegacyCondominiumStore>();
builder.Services.AddScoped<LegacyCondominiumBackfill>();
builder.Services.AddHostedService<LegacyCondominiumBackfillInitializer>();

// Id de rota malformado responde 400 em vez de 500 (MalformedIdFilter).
builder.Services.AddControllers(options => options.Filters.Add<MalformedIdFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
// Erro inesperado vira ProblemDetails (application/problem+json) sem mensagem nem stack da exceção.
builder.Services.AddProblemDetails();

// Limites das rotas anônimas de autenticação (por IP e por condomínio; ver AuthRateLimit).
builder.Services.AddAuthRateLimits(builder.Configuration);

// Atrás de proxy (Render), o IP do cliente vem no X-Forwarded-For. Só é lido com Proxy:ConfiarNoProxy=true:
// sem proxy na frente, qualquer cliente poderia forjar o cabeçalho e escapar dos limites por IP.
var trustProxy = builder.Configuration.GetValue("Proxy:ConfiarNoProxy", false);
if (trustProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// CORS só para as origens do painel configuradas (Cors:AllowedOrigins). Sem configuração, em
// desenvolvimento valem as portas locais do painel e do Expo web; em produção nenhuma origem é liberada.
// O app nativo não envia Origin e não depende de CORS.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment()
        ? new[] { "http://localhost:3000", "http://localhost:8081", "http://localhost:19006" }
        : Array.Empty<string>());
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Retry-After");
    });
});

var app = builder.Build();

// Modo demonstração (Demo:Enabled=true, independente do ambiente): cria o condomínio e as contas demo.
// Sem a flag, nenhuma conta de demonstração existe.
if (app.Services.GetRequiredService<DemoCatalog>().Enabled)
{
    // A cada partida a demonstração volta ao estado inicial (apaga o que visitantes criaram e recria).
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DemoDataCleaner>().ResetAsync();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
}

if (trustProxy) app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
// Documentação interativa só em desenvolvimento (ou com Swagger:Enabled=true explícito).
if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
