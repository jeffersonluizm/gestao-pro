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

// A conta admin de demonstração tem senha conhecida: só existe em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<ApiAccessMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacaoHub>("/notificacaoHub");

app.Run();
