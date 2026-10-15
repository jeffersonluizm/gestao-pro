using GestaoPro.API.Models;
using GestaoPro.API.Repositories;
using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Services;

// Dados do modo demonstração (só com Demo:Enabled=true; ver DemoCatalog). Idempotente: a cada execução
// recria o que falta e devolve as contas demo ao estado conhecido (senha da configuração, ativas, papel certo).
// Nenhuma senha fica no código nem vai para o log: elas vêm do DemoCatalog e são publicadas em GET /api/demo.
public class DataSeeder
{
    private static readonly (string Name, string Description, int Capacity, string Open, string Close)[] CommonAreas =
    [
        ("Salão de festas", "Salão com cozinha e mesas para 80 pessoas.", 80, "10:00", "23:00"),
        ("Churrasqueira", "Área coberta com duas churrasqueiras.", 30, "10:00", "22:00"),
        ("Quadra poliesportiva", "Quadra com iluminação para vôlei, futebol e basquete.", 20, "08:00", "21:00"),
    ];

    private static readonly (string Title, string Content, string Category)[] Announcements =
    [
        ("Bem-vindo à demonstração", "Este é um condomínio de exemplo. Os dados voltam ao estado inicial periodicamente.", "Geral"),
        ("Manutenção do elevador social", "Na sexta-feira, das 9h às 12h, use o elevador de serviço.", "Manutenção"),
        ("Assembleia geral ordinária", "Dia 20, às 19h, no salão de festas. Pauta: orçamento e portão da garagem.", "Assembleia"),
    ];

    private readonly DemoCatalog _catalog;
    private readonly UserRepository _users;
    private readonly CredentialRepository _credentials;
    private readonly CondominiumRepository _condominiums;
    private readonly ICommonAreaRepository _commonAreas;
    private readonly UnitRepository _units;
    private readonly IAnnouncementRepository _announcements;
    private readonly IBillRepository _bills;
    private readonly IReservationRepository _reservations;
    private readonly IVisitorRepository _visitors;
    private readonly OccurrenceRepository _occurrences;
    private readonly IAssemblyRepository _assemblies;
    private readonly IDeliveryService _deliveries;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        DemoCatalog catalog,
        UserRepository users,
        CredentialRepository credentials,
        CondominiumRepository condominiums,
        ICommonAreaRepository commonAreas,
        UnitRepository units,
        IAnnouncementRepository announcements,
        IBillRepository bills,
        IReservationRepository reservations,
        IVisitorRepository visitors,
        OccurrenceRepository occurrences,
        IAssemblyRepository assemblies,
        IDeliveryService deliveries,
        ILogger<DataSeeder> logger)
    {
        _catalog = catalog;
        _users = users;
        _credentials = credentials;
        _condominiums = condominiums;
        _commonAreas = commonAreas;
        _units = units;
        _announcements = announcements;
        _bills = bills;
        _reservations = reservations;
        _visitors = visitors;
        _occurrences = occurrences;
        _assemblies = assemblies;
        _deliveries = deliveries;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (!_catalog.Enabled) return;

        var condominium = await EnsureCondominiumAsync();
        _catalog.CondominiumId = condominium.Id;

        await EnsureCommonAreasAsync(condominium.Id!);
        var unit = await EnsureUnitAsync(condominium.Id!, "101");
        var accounts = new Dictionary<string, User>();
        foreach (var account in _catalog.Accounts)
            accounts[account.Role] = await EnsureAccountAsync(condominium.Id!, account, account.Role == "morador" ? unit.Id : null);

        await EnsurePreRegisteredResidentsAsync(condominium.Id!);
        await EnsureAnnouncementsAsync(condominium.Id!, accounts["sindico"].Id!);
        await EnsureBillAsync(condominium.Id!, accounts["morador"].Id!);
        await EnsureActivityAsync(condominium.Id!, unit.Id!, accounts["sindico"].Id!, accounts["morador"].Id!);

        _logger.LogInformation("Modo demonstração pronto: condomínio {Name} com {Count} contas (credenciais em GET /api/demo).",
            DemoCatalog.CondominiumName, _catalog.Accounts.Count);
    }

    private async Task<Condominium> EnsureCondominiumAsync()
    {
        var codeHash = BCrypt.Net.BCrypt.HashPassword(_catalog.CondominiumCode);
        var condominium = await _condominiums.GetByCnpjAsync(DemoCatalog.CondominiumCnpj);
        if (condominium is null)
        {
            return await _condominiums.CreateAsync(new Condominium
            {
                Name = DemoCatalog.CondominiumName,
                Cnpj = DemoCatalog.CondominiumCnpj,
                Address = new Address
                {
                    Street = "Rua das Acácias", Number = "100", Neighborhood = "Centro",
                    City = "Belo Horizonte", State = "MG", ZipCode = "30110-000",
                },
                Rules = "Silêncio das 22h às 7h. Reservas pelo app ou pelo painel.",
                DefaultPassword = codeHash,
            });
        }

        // O código pode ter mudado na configuração (ou ter sido sorteado de novo): volta ao da partida.
        condominium.Name = DemoCatalog.CondominiumName;
        condominium.DefaultPassword = codeHash;
        await _condominiums.UpdateAsync(condominium.Id!, condominium);
        return condominium;
    }

    private async Task EnsureCommonAreasAsync(string condominiumId)
    {
        var existing = await _commonAreas.GetByCondominiumAsync(condominiumId);
        foreach (var (name, description, capacity, open, close) in CommonAreas)
        {
            if (existing.Any(area => area.Name == name)) continue;
            await _commonAreas.CreateAsync(new CommonArea
            {
                CondominiumId = condominiumId, Name = name, Description = description, Capacity = capacity,
                OpenTime = open, CloseTime = close, Rules = "Deixe o espaço limpo ao final do uso.", IsActive = true,
            });
        }
    }

    private async Task<Unit> EnsureUnitAsync(string condominiumId, string number) =>
        await _units.GetByNumberAndBlockAsync(condominiumId, number, "A")
        ?? await _units.CreateAsync(new Unit { CondominiumId = condominiumId, Number = number, Block = "A", Floor = 1, Type = UnitType.apt });

    // Conta demo: senha da configuração, ativa, sem troca obrigatória, papel e condomínio certos.
    private async Task<User> EnsureAccountAsync(string condominiumId, DemoAccount account, string? unitId)
    {
        var role = Enum.Parse<UserRole>(account.Role);
        var hash = BCrypt.Net.BCrypt.HashPassword(account.Password);
        var user = await _users.GetByEmailAsync(account.Email);
        if (user is null)
        {
            user = await _users.CreateAsync(new User
            {
                CondominiumId = condominiumId, UnitId = unitId, Name = account.Name, Email = account.Email,
                RegistrationNumber = account.Email, PasswordHash = hash, Phone = "3133330000",
                Cpf = DemoCpf(account.Role), Role = role, MustChangePassword = false, IsActive = true,
            });
            if (unitId is not null) await _units.AddResidentAsync(unitId, user.Id!);
        }
        else
        {
            user.CondominiumId = condominiumId;
            user.Name = account.Name;
            user.Role = role;
            user.IsActive = true;
            user.MustChangePassword = false;
            user.PasswordHash = hash;
            await _users.UpdateAsync(user.Id!, user);
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id!);
        if (credential is null)
        {
            await _credentials.CreateAsync(new Credential
            {
                UserId = user.Id!, RegistrationNumber = account.Email, PasswordHash = hash, Role = account.Role,
                CondominiumId = condominiumId, MustChangePassword = false, IsActive = true,
            });
        }
        else
        {
            await _credentials.UpdatePasswordAsync(user.Id!, hash, false);
            if (!credential.IsActive) await _credentials.ReactivateAsync(user.Id!);
        }
        return user;
    }

    private static string DemoCpf(string role) => role switch
    {
        "sindico" => "90000000001",
        "morador" => "90000000002",
        _ => "90000000003",
    };

    // CPFs pré-cadastrados (sem acesso) para experimentar o "Ative o seu acesso" com o código do condomínio.
    private async Task EnsurePreRegisteredResidentsAsync(string condominiumId)
    {
        foreach (var (cpf, name) in DemoCatalog.PreRegisteredResidents)
        {
            var existing = await _users.GetByCpfAsync(cpf);
            if (existing is not null)
            {
                if (existing.CondominiumId != condominiumId)
                {
                    existing.CondominiumId = condominiumId;
                    await _users.UpdateAsync(existing.Id!, existing);
                }
                continue;
            }
            await _users.CreateAsync(new User
            {
                CondominiumId = condominiumId, Name = name, Email = $"{cpf}@pending.gestaopro.local", Phone = "3133330000",
                Cpf = cpf, RegistrationNumber = $"PENDING-{cpf}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role = UserRole.morador, MustChangePassword = true, IsActive = true,
            });
        }
    }

    private async Task EnsureAnnouncementsAsync(string condominiumId, string authorId)
    {
        var existing = await _announcements.GetByCondominiumAsync(condominiumId);
        foreach (var (title, content, category) in Announcements)
        {
            if (existing.Any(item => item.Title == title)) continue;
            await _announcements.CreateAsync(new Announcement
            {
                CondominiumId = condominiumId, AuthorId = authorId, Title = title, Content = content, Category = category,
            });
        }
    }

    // Movimento do condomínio para os fluxos parecerem vivos: reserva, encomendas, visita, ocorrência e assembleia.
    private async Task EnsureActivityAsync(string condominiumId, string unitId, string sindicoId, string moradorId)
    {
        if ((await _reservations.GetByCondominiumAsync(condominiumId)).Count > 0) return;

        var areas = await _commonAreas.GetByCondominiumAsync(condominiumId);
        var salao = areas.FirstOrDefault(area => area.Name == "Salão de festas") ?? areas.First();
        await _reservations.CreateAsync(new Reservation
        {
            CommonAreaId = salao.Id!, UserId = moradorId, CondominiumId = condominiumId,
            Date = BrazilTime.Today().AddDays(6).ToString("yyyy-MM-dd"), StartTime = "14:00", EndTime = "18:00",
            Status = ReservationStatus.Pendente, Observations = "Aniversário da Laura",
        });

        foreach (var (code, description) in new[] { ("BR123456789", "Caixa média, loja online"), ("LX998877", "Envelope de documentos") })
        {
            await _deliveries.CreateAsync(new Delivery
            {
                TrackingCode = code, Description = description, Type = "encomenda", ResidentId = moradorId,
                UnitId = unitId, CondominiumId = condominiumId, Status = "pending",
            });
        }

        await _visitors.CreateAsync(new Visitor
        {
            FullName = "Paulo Mendes", Document = "MG-12.345.678", Phone = "31999998888", ResidentId = moradorId,
            CondominiumId = condominiumId, UnitId = unitId, Reason = "Visita", Status = "checked-in", CheckInAt = DateTime.UtcNow.AddHours(-1),
        });

        await _occurrences.CreateAsync(new Occurrence
        {
            CondominiumId = condominiumId, UserId = moradorId, Title = "Vazamento na garagem",
            Description = "Goteira perto da vaga 12, no subsolo.", Category = OccurrenceCategory.Maintenance,
            Priority = OccurrencePriority.Medium, Status = OccurrenceStatus.Open,
        });

        await _assemblies.CreateAsync(new AssemblyModel
        {
            CondominiumId = condominiumId, CreatedBy = sindicoId, Title = "Assembleia geral ordinária",
            Description = "Previsão orçamentária e troca do portão da garagem.",
            StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow.AddDays(14), Status = "em_andamento",
            Topics =
            {
                new TopicModel { Title = "Aprovar a previsão orçamentária de 2027", VoteOptions = { "sim", "não" } },
                new TopicModel { Title = "Trocar o portão da garagem", VoteOptions = { "sim", "não" } },
            },
        });
    }

    private async Task EnsureBillAsync(string condominiumId, string residentId)
    {
        if ((await _bills.GetByUserIdAsync(residentId)).Any()) return;
        var due = DateTime.UtcNow.Date.AddDays(10);
        await _bills.CreateAsync(new BillModel
        {
            CondominiumId = condominiumId, UserId = residentId, Description = $"Taxa condominial {due:MM/yyyy}",
            Amount = 620m, DueDate = due, Status = "pending", Barcode = $"DEMO-{due:yyyyMMdd}",
        });
    }
}
