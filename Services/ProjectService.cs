using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos;
using VocabularyService.Options;
using JsonTypes = VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для работы с проектами
/// </summary>
    public class ProjectService : IProjectService
    {
        private readonly VocabularyServiceContext _context;
        private readonly VocabularyServiceOptions _options;
        private readonly ILogger<ProjectService> _logger;
        private readonly IMapper _mapper;

        public ProjectService(
            VocabularyServiceContext context,
            IOptions<VocabularyServiceOptions> options,
            ILogger<ProjectService> logger,
            IMapper mapper)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;
            _mapper = mapper;
        }

    /// <summary>
    /// Создает новый проект и системную колоду "Inbox"
    /// </summary>
    public async Task<Project> CreateProjectAsync(
        CreateProjectDto dto,
        CancellationToken cancellationToken = default)
    {
        // Определяем настройки FSRS (используем из DTO или дефолтные)
        var finalFsrsSettings = dto.FsrsSettings ?? GetDefaultFsrsSettings(dto.TargetLang);

        // Создаем проект (Id будет сгенерирован БД через uuid_generate_v4())
        var project = new Project
        {
            // Id не указываем - будет сгенерирован БД автоматически благодаря ValueGeneratedOnAdd()
            UserId = dto.UserId,
            Title = dto.Title,
            SourceLang = dto.SourceLang,
            TargetLang = dto.TargetLang,
            FsrsSettings = finalFsrsSettings,
            Stats = new JsonTypes.ProjectStats
            {
                TotalLemmas = 0,
                MatureLemmas = 0
            },
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Создаем системную колоду "Inbox" (Id будет сгенерирован БД автоматически)
        // Используем navigation property для связывания с Project
        var inboxDeck = new Deck
        {
            // Id не указываем - будет сгенерирован БД автоматически благодаря ValueGeneratedOnAdd()
            Project = project, // Используем navigation property вместо ProjectId
            ParentDeckId = null,
            OwnerId = dto.UserId, // Используем UserId из DTO
            Title = _options.InboxDeckTitle,
            Description = null,
            CoverImageUrl = null,
            IsPublic = false,
            ContributionPolicy = "CLOSED",
            LicenseType = "PRIVATE",
            ForkedFromId = null,
            CardCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Добавляем оба объекта в контекст
        _context.Projects.Add(project);
        _context.Decks.Add(inboxDeck);

        // Сохраняем одним SaveChanges - EF Core автоматически установит ProjectId у Deck
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created project {ProjectId} with title '{Title}' for user {UserId}",
            project.Id,
            dto.Title,
            dto.UserId);

        return project;
    }

    /// <summary>
    /// Проверяет, не превышен ли лимит проектов для пользователя
    /// </summary>
    public async Task<bool> CanCreateProjectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var projectCount = await _context.Projects
            .CountAsync(p => p.UserId == userId && !p.IsArchived, cancellationToken);

        return projectCount < _options.MaxProjectsPerUser;
    }

    /// <summary>
    /// Проверяет, существует ли проект с таким названием у пользователя
    /// </summary>
    public async Task<bool> ProjectTitleExistsAsync(
        Guid userId,
        string title,
        CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.UserId == userId && p.Title == title, cancellationToken);
    }

    /// <summary>
    /// Получает список всех проектов пользователя с краткой статистикой
    /// </summary>
    public async Task<List<Project>> GetProjectsAsync(
        Guid userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Projects
            .Where(p => p.UserId == userId);

        // Фильтрация архивных проектов
        if (!includeArchived)
        {
            query = query.Where(p => !p.IsArchived);
        }

        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        if (projects.Count > 0)
        {
            var projectIds = projects.Select(p => p.Id).ToList();

            var termCounts = await _context.ProjectTerms
                .AsNoTracking()
                .Where(pt => projectIds.Contains(pt.ProjectId))
                .GroupBy(pt => pt.ProjectId)
                .Select(g => new { ProjectId = g.Key, TotalTerms = g.Count() })
                .ToDictionaryAsync(x => x.ProjectId, x => x.TotalTerms, cancellationToken);

            var knownCounts = await _context.UserTermStatuses
                .AsNoTracking()
                .Where(uts => uts.UserId == userId && projectIds.Contains(uts.ProjectId) && uts.Status == "KNOWN")
                .GroupBy(uts => uts.ProjectId)
                .Select(g => new { ProjectId = g.Key, KnownTerms = g.Count() })
                .ToDictionaryAsync(x => x.ProjectId, x => x.KnownTerms, cancellationToken);

            foreach (var project in projects)
            {
                var total = termCounts.TryGetValue(project.Id, out var t) ? t : 0;
                var known = knownCounts.TryGetValue(project.Id, out var k) ? k : 0;

                project.Stats = new JsonTypes.ProjectStats
                {
                    TotalLemmas = total,
                    MatureLemmas = known
                };
            }
        }

        _logger.LogInformation(
            "Retrieved {Count} projects for user {UserId} (includeArchived: {IncludeArchived})",
            projects.Count,
            userId,
            includeArchived);

        return projects;
    }

    /// <summary>
    /// Получает настройки FSRS по умолчанию для указанного языка
    /// </summary>
    private JsonTypes.FsrsSettings GetDefaultFsrsSettings(string targetLang)
    {
        // Пытаемся найти пресет для языка
        if (_options.FsrsPresets.TryGetValue(targetLang, out var preset))
        {
            // Используем AutoMapper для преобразования FsrsPreset в FsrsSettings
            var fsrsSettings = _mapper.Map<JsonTypes.FsrsSettings>(preset);
            
            // Если веса не указаны в пресете, используем дефолтные
            if (fsrsSettings.W.Length == 0)
            {
                fsrsSettings.W = GetDefaultWeights();
            }
            
            return fsrsSettings;
        }

        // Используем дефолтные настройки
        return new JsonTypes.FsrsSettings
        {
            RequestRetention = 0.9,
            MaximumInterval = 36500,
            W = GetDefaultWeights()
        };
    }

    /// <summary>
    /// Возвращает дефолтные веса FSRS (18 значений)
    /// </summary>
    private static double[] GetDefaultWeights()
    {
        // Стандартные веса FSRS v5
        return
        [
            0.4, 0.6, 2.4, 5.8, 4.93, 0.94, 0.86, 0.01, 1.49, 0.14, 0.94,
            2.18, 0.05, 0.34, 1.26, 0.29, 2.61, 0.0
        ];
    }

    /// <summary>
    /// Получает детали проекта по идентификатору
    /// </summary>
    public async Task<Project?> GetProjectByIdAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return null;
        }

        // Проверка прав доступа: проект должен принадлежать пользователю
        if (project.UserId != userId)
        {
            throw new UnauthorizedAccessException("Project does not belong to user");
        }

        var totalTerms = await _context.ProjectTerms
            .AsNoTracking()
            .CountAsync(pt => pt.ProjectId == projectId, cancellationToken);

        var knownTerms = await _context.UserTermStatuses
            .AsNoTracking()
            .CountAsync(uts => uts.UserId == userId && uts.ProjectId == projectId && uts.Status == "KNOWN", cancellationToken);

        project.Stats = new JsonTypes.ProjectStats
        {
            TotalLemmas = totalTerms,
            MatureLemmas = knownTerms
        };

        return project;
    }

    /// <summary>
    /// Обновляет проект
    /// </summary>
    public async Task<Project> UpdateProjectAsync(
        Guid projectId,
        Guid userId,
        string? title = null,
        bool? isArchived = null,
        JsonTypes.FsrsSettings? fsrsSettings = null,
        JsonTypes.TtsSettings? ttsSettings = null,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectByIdAsync(projectId, userId, cancellationToken);
        
        if (project == null)
        {
            throw new KeyNotFoundException($"Project {projectId} not found");
        }

        // Обновляем только переданные поля
        if (title != null)
        {
            project.Title = title;
        }

        if (isArchived.HasValue)
        {
            project.IsArchived = isArchived.Value;
        }

        if (fsrsSettings != null)
        {
            // Валидация настроек FSRS
            ValidateFsrsSettings(fsrsSettings);
            project.FsrsSettings = fsrsSettings;
        }

        if (ttsSettings != null)
        {
            project.TtsSettings = ttsSettings;
        }

        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Project {ProjectId} updated successfully by user {UserId}",
            projectId,
            userId);

        return project;
    }

    /// <summary>
    /// Валидирует настройки FSRS
    /// </summary>
    private void ValidateFsrsSettings(JsonTypes.FsrsSettings settings)
    {
        if (settings.RequestRetention < 0.7 || settings.RequestRetention > 0.99)
        {
            throw new ArgumentException("RequestRetention must be between 0.7 and 0.99");
        }

        if (settings.MaximumInterval < 1 || settings.MaximumInterval > 36500)
        {
            throw new ArgumentException("MaximumInterval must be between 1 and 36500");
        }

        if (settings.W != null && settings.W.Length != 18)
        {
            throw new ArgumentException("FSRS weights array must contain exactly 18 values");
        }

        ValidateStepSeconds(settings.LearningStepsSeconds, "LearningStepsSeconds");
        ValidateStepSeconds(settings.RelearningStepsSeconds, "RelearningStepsSeconds");
    }

    private static void ValidateStepSeconds(int[]? steps, string name)
    {
        if (steps == null || steps.Length == 0)
            return;
        if (steps.Length > 10)
            throw new ArgumentException($"{name}: не больше 10 шагов (как в Anki)");
        foreach (var s in steps)
        {
            if (s < 1 || s > 120_9600)
                throw new ArgumentException($"{name}: каждый шаг 1…1209600 секунд");
        }
    }

    /// <summary>
    /// Безвозвратно удаляет проект и каскадно чистит связанные сущности
    /// </summary>
    public async Task DeleteProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectByIdAsync(projectId, userId, cancellationToken);
        if (project == null)
        {
            throw new KeyNotFoundException($"Project {projectId} not found");
        }

        // Создаем запись в deleted_objects (Tombstone) для синхронизации клиентов
        var deletedObject = new DeletedObject
        {
            Id = Guid.NewGuid(),
            EntityId = projectId,
            EntityType = "Project",
            UserId = userId,
            ParentId = null,
            DeletedAt = DateTime.UtcNow
        };

        _context.DeletedObjects.Add(deletedObject);

        // Удаляем проект (PostgreSQL CASCADE чистит decks, cards, project_terms, project_lemmas, user_card_progress и др.)
        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted project {ProjectId} by user {UserId}",
            projectId,
            userId);
    }
}

