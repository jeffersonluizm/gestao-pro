using GestaoPro.API.Repositories;
using GestaoPro.API.Services;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
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
if (!string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
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
        });
}

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
builder.Services.AddScoped<UnitAppService>();
builder.Services.AddScoped<UserAppService>();
builder.Services.AddScoped<PetAppService>();
builder.Services.AddScoped<AuthAppService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OccurrenceAppService>();
builder.Services.AddScoped<IAssemblyService, AssemblyService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddHostedService<VoteIndexInitializer>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

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

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.UseCors();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method) ||
        HttpMethods.IsHead(context.Request.Method) ||
        HttpMethods.IsOptions(context.Request.Method))
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
    var isInitialCredentialCreation =
        HttpMethods.IsPost(context.Request.Method) &&
        path.StartsWith("/api/users/") &&
        path.EndsWith("/credentials");
    var hasAdministratorCredential =
        await context.RequestServices.GetRequiredService<CredentialRepository>().HasAnyAdministratorAsync();
    var isFirstUserCreation =
        HttpMethods.IsPost(context.Request.Method) &&
        path == "/api/users" &&
        !hasAdministratorCredential;
    var isFirstCondominiumCreation =
        HttpMethods.IsPost(context.Request.Method) &&
        path == "/api/condominiums" &&
        !hasAdministratorCredential;

    if (path.StartsWith("/api/auth/login") ||
        path.StartsWith("/api/auth/change-password") ||
        path.StartsWith("/api/auth/register") ||
        path.StartsWith("/api/auth/check-cpf") ||
        isInitialCredentialCreation ||
        isFirstUserCreation ||
        isFirstCondominiumCreation)
    {
        await next();
        return;
    }

    if (context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Login obrigatório para executar esta ação." });
        return;
    }

    var role = context.User.FindFirstValue(ClaimTypes.Role);
    var isAdministrator = role is "admin" or "sindico";
    var isDeliveryPickup =
        path.StartsWith("/api/delivery/") &&
        path.EndsWith("/pickup") &&
        HttpMethods.IsPut(context.Request.Method);
    var isDoormanDeliveryAction =
        role is "porteiro" &&
        path.StartsWith("/api/delivery") &&
        (HttpMethods.IsPost(context.Request.Method) ||
         HttpMethods.IsPut(context.Request.Method) ||
         HttpMethods.IsDelete(context.Request.Method));
    var isUserAllowedAction =
        path.StartsWith("/api/reservations") && HttpMethods.IsPost(context.Request.Method) ||
        path.StartsWith("/api/occurrences") && HttpMethods.IsPost(context.Request.Method) ||
        path.StartsWith("/api/vote") && HttpMethods.IsPost(context.Request.Method) ||
        path == "/api/visitor/register" && HttpMethods.IsPost(context.Request.Method) ||
        path.StartsWith("/api/visitor/resident/") && HttpMethods.IsGet(context.Request.Method) ||
        isDeliveryPickup ||
        isDoormanDeliveryAction;

    if (!isAdministrator && !isUserAllowedAction)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = "Ação permitida apenas para administrador ou síndico." });
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacaoHub>("/notificacaoHub");

app.Run();
