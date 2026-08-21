using FluentValidation;
using Npgsql;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Grpc;
using VocabularyService.Mappers;
using VocabularyService.Options;
using VocabularyService.Services;
using VocabularyService.Services.Study;
using VocabularyService.Validations;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string? connection = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connection);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<VocabularyServiceContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    })
);

// Конфигурация Options
builder.Services.Configure<VocabularyServiceOptions>(
    builder.Configuration.GetSection(VocabularyServiceOptions.SectionName));
builder.Services.Configure<InclusiveOptions>(
    builder.Configuration.GetSection(InclusiveOptions.SectionName));
builder.Services.Configure<MediaOptions>(
    builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<BillingOptions>(
    builder.Configuration.GetSection(BillingOptions.SectionName));

// Redis client
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(redisConnection);
});

// Регистрация AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMappingProfile));

// Регистрация сервисов
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INoteTypeService, NoteTypeService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<INoteTypeService, NoteTypeService>();
builder.Services.AddScoped<IAnkiStudyQueueService, AnkiStudyQueueService>();
builder.Services.AddScoped<IFsrsPreviewService, FsrsPreviewService>();
builder.Services.AddScoped<IStudyService, StudyService>();
builder.Services.AddScoped<ILemmaService, LemmaService>();
builder.Services.AddGrpcClient<Vocab.VocabService.VocabServiceClient>(o =>
{
    var address = builder.Configuration.GetValue<string>("Inclusive:GrpcAddress") ?? "http://localhost:40051";
    o.Address = new Uri(address);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    EnableMultipleHttp2Connections = true,
    UseProxy = false,
});
builder.Services.AddGrpcClient<Pvs.Media.Grpc.MediaService.MediaServiceClient>(o =>
{
    var address = builder.Configuration.GetValue<string>("Media:GrpcAddress") ?? "http://localhost:5121";
    o.Address = new Uri(address);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    EnableMultipleHttp2Connections = true,
    UseProxy = false,
});
builder.Services.AddGrpcClient<Pvs.Billing.Grpc.BillingService.BillingServiceClient>(o =>
{
    var address = builder.Configuration.GetValue<string>("Billing:GrpcAddress") ?? "http://localhost:5127";
    o.Address = new Uri(address);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    EnableMultipleHttp2Connections = true,
    UseProxy = false,
});
builder.Services.AddScoped<IBillingEntitlementClient, BillingEntitlementClient>();
builder.Services.AddScoped<IBillingLimitService, BillingLimitService>();
builder.Services.AddScoped<InclusiveFsrsScheduler>();
builder.Services.AddScoped<IFsrsScheduler, InclusiveFsrsScheduler>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IAutopilotService, AutopilotService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<IOllamaClient, OllamaClient>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<ITextService, TextService>();
builder.Services.AddScoped<ITermService, TermService>();
builder.Services.AddScoped<IMediaService, MediaGrpcClientAdapter>();

// Регистрация MemoryCache для AIService
builder.Services.AddMemoryCache();

// Регистрация FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();

// Настройка Kestrel для поддержки HTTP/2 без TLS (для локальной разработки)
// Без явного Protocols = Http2 gRPC-клиент получает HttpRequestException (RequestVersionExact HTTP/2).
// В контейнере слушаем на всех интерфейсах (0.0.0.0), иначе только loopback.
// ВАЖНО: настройка Kestrel должна быть ДО AddGrpc
builder.WebHost.ConfigureKestrel(options =>
{
    var inContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    var listenAddress = inContainer ? System.Net.IPAddress.Any : System.Net.IPAddress.Loopback;
    options.Listen(listenAddress, 5117, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.MaxSendMessageSize = 1000 * 1024 * 1024;
    options.MaxReceiveMessageSize = 1000 * 1024 * 1024;
    options.EnableDetailedErrors = true;
});

builder.Services.AddControllers();

var app = builder.Build();

// Apply EF Core migrations on startup (creates/updates schema "internal" and tables)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VocabularyServiceContext>();
    db.Database.Migrate();
    await LessonSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

// Map gRPC services
app.MapGrpcService<ContentService>();
app.MapGrpcService<CardGrpcService>();
app.MapGrpcService<StudyGrpcService>();
app.MapGrpcService<AnalyticsGrpcService>();
app.MapGrpcService<CommunityGrpcService>();
app.MapGrpcService<SyncGrpcService>();
app.MapGrpcService<AIGrpcService>();
app.MapGrpcService<TextGrpcService>();
app.MapGrpcService<TermGrpcService>();
app.MapGrpcService<SubscriptionGrpcService>();
app.MapGrpcService<LessonGrpcService>();

app.MapControllers();

app.Run();
