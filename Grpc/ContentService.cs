using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Pvs.Content.Grpc;
using VocabularyService.Dtos;
using VocabularyService.Helpers;
using VocabularyService.Options;
using VocabularyService.Services;
using static Pvs.Content.Grpc.ContentService;
using JsonTypes = VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Grpc
{
    /// <summary>
    /// gRPC сервис для работы с контентом (проекты, колоды, настройки)
    /// </summary>
    public class ContentService : ContentServiceBase
    {
        private readonly ILogger<ContentService> _logger;
        private readonly IProjectService _projectService;
        private readonly IDeckService _deckService;
        private readonly IUserSettingsService _userSettingsService;
        private readonly IBillingLimitService _billingLimitService;
        private readonly VocabularyServiceOptions _options;
        private readonly IValidator<CreateProjectRequest> _createProjectValidator;
        private readonly IMapper _mapper;

        public ContentService(
            ILogger<ContentService> logger,
            IProjectService projectService,
            IDeckService deckService,
            IUserSettingsService userSettingsService,
            IBillingLimitService billingLimitService,
            IOptions<VocabularyServiceOptions> options,
            IValidator<CreateProjectRequest> createProjectValidator,
            IMapper mapper)
        {
            _logger = logger;
            _projectService = projectService;
            _deckService = deckService;
            _userSettingsService = userSettingsService;
            _billingLimitService = billingLimitService;
            _options = options.Value;
            _createProjectValidator = createProjectValidator;
            _mapper = mapper;
        }

        //===== SR-STR-01: Создание проекта =====
        /// <summary>
        /// Создание нового языкового проекта (SR-STR-01)
        /// </summary>
        /// <param name="request">Запрос на создание проекта.</param>
        /// <param name="context">Контекст вызова на сервере.</param>
        /// <returns>Возвращает созданный проект.</returns>
        public override async Task<ProjectResponse> CreateProject(
            CreateProjectRequest request,
            ServerCallContext context)
        {
            // Получаем user_id из контекста (от агрегатора)
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "CreateProject request from user {UserId} with roles: {Roles}",
                userId,
                string.Join(", ", roles));

            // Валидация: проверяем, что user_id из запроса совпадает с user_id из контекста
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            // Валидация с помощью FluentValidation
            var validationResult = await _createProjectValidator.ValidateAsync(request, context.CancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, $"Validation failed: {errors}"));
            }

            // Проверка лимита проектов по SaaS-тарифу
            var canCreate = await _billingLimitService.CanCreateProjectAsync(userId, context.CancellationToken);
            if (!canCreate)
            {
                var maxProjects = await _billingLimitService.GetMaxProjectsAsync(userId, context.CancellationToken);
                throw new RpcException(
                    new Status(StatusCode.ResourceExhausted,
                        "Billing limit exceeded: maxProjects"));
            }

            // Проверка уникальности названия
            var titleExists = await _projectService.ProjectTitleExistsAsync(userId, request.Title, context.CancellationToken);
            if (titleExists)
            {
                throw new RpcException(
                    new Status(StatusCode.AlreadyExists,
                        $"Project with title '{request.Title}' already exists"));
            }

            // Преобразуем gRPC запрос в DTO используя AutoMapper
            var createProjectDto = _mapper.Map<VocabularyService.Dtos.CreateProjectDto>(request);
            // Убеждаемся, что UserId в DTO совпадает с UserId из контекста
            createProjectDto.UserId = userId;

            // Создаем проект
            var project = await _projectService.CreateProjectAsync(createProjectDto, context.CancellationToken);

            // Преобразуем в ответ используя AutoMapper
            var response = _mapper.Map<ProjectResponse>(project);

            _logger.LogInformation(
                "Project {ProjectId} created successfully for user {UserId}",
                project.Id,
                userId);

            return response;
        }

        //===== SR-STR-01: Получение списка проектов =====
        /// <summary>
        /// Получение списка всех проектов пользователя с краткой статистикой (SR-STR-01)
        /// </summary>
        /// <param name="request">Запрос на получение списка проектов.</param>
        /// <param name="context">Контекст вызова на сервере.</param>
        /// <returns>Возвращает список проектов пользователя.</returns>
        public override async Task<GetProjectsResponse> GetProjects(
            GetProjectsRequest request,
            ServerCallContext context)
        {
            // Получаем user_id из контекста (от агрегатора)
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "GetProjects request from user {UserId} with roles: {Roles}, includeArchived: {IncludeArchived}",
                userId,
                string.Join(", ", roles),
                request.IncludeArchived);

            // Валидация: проверяем, что user_id из запроса совпадает с user_id из контекста (если передан)
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            // Получаем список проектов
            var projects = await _projectService.GetProjectsAsync(
                userId,
                request.IncludeArchived,
                context.CancellationToken);

            // Преобразуем в ответ используя AutoMapper
            var projectResponses = projects
                .Select(project => _mapper.Map<ProjectResponse>(project))
                .ToList();

            var response = new GetProjectsResponse();
            response.Projects.AddRange(projectResponses);

            _logger.LogInformation(
                "Retrieved {Count} projects for user {UserId}",
                projectResponses.Count,
                userId);

            return response;
        }

        //===== SR-STR-02: Получение деталей проекта =====
        /// <summary>
        /// Получение полной информации о проекте, включая настройки FSRS (SR-STR-02)
        /// </summary>
        /// <param name="request">Запрос на получение деталей проекта.</param>
        /// <param name="context">Контекст вызова на сервере.</param>
        /// <returns>Возвращает детали проекта.</returns>
        public override async Task<ProjectResponse> GetProjectDetails(
            GetProjectDetailsRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "GetProjectDetails request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            // Валидация UUID
            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
            }

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            // Получаем проект
            var project = await _projectService.GetProjectByIdAsync(projectId, userId, context.CancellationToken);

            if (project == null)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Project {projectId} not found"));
            }

            // Преобразуем в ответ
            var response = _mapper.Map<ProjectResponse>(project);

            _logger.LogInformation(
                "Project {ProjectId} retrieved successfully for user {UserId}",
                projectId,
                userId);

            return response;
        }

        //===== SR-STR-02: Обновление настроек проекта =====
        /// <summary>
        /// Обновление метаданных и настроек алгоритма обучения (SR-STR-02)
        /// </summary>
        /// <param name="request">Запрос на обновление проекта.</param>
        /// <param name="context">Контекст вызова на сервере.</param>
        /// <returns>Возвращает обновленный проект.</returns>
        public override async Task<ProjectResponse> UpdateProject(
            UpdateProjectRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "UpdateProject request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            // Валидация UUID
            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
            }

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            // Преобразуем gRPC запрос в параметры
            // StringValue и BoolValue могут быть null, если поле не было установлено
            string? title = null;
            if (request.Title != null && !string.IsNullOrEmpty(request.Title))
            {
                title = request.Title;
            }

            bool? isArchived = null;
            if (request.IsArchived != null)
            {
                isArchived = request.IsArchived;
            }
            JsonTypes.FsrsSettings? fsrsSettings = request.Settings != null
                ? _mapper.Map<JsonTypes.FsrsSettings>(request.Settings)
                : null;
            JsonTypes.TtsSettings? ttsSettings = request.TtsSettings != null
                ? _mapper.Map<JsonTypes.TtsSettings>(request.TtsSettings)
                : null;

            try
            {
                // Обновляем проект
                var project = await _projectService.UpdateProjectAsync(
                    projectId,
                    userId,
                    title,
                    isArchived,
                    fsrsSettings,
                    ttsSettings,
                    context.CancellationToken);

                // Преобразуем в ответ
                var response = _mapper.Map<ProjectResponse>(project);

                _logger.LogInformation(
                    "Project {ProjectId} updated successfully by user {UserId}",
                    projectId,
                    userId);

                return response;
            }
            catch (KeyNotFoundException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.PermissionDenied, ex.Message));
            }
        }

        /// <summary>
        /// Безвозвратное удаление проекта и всех связанных данных (SR-STR-02)
        /// </summary>
        public override async Task<Empty> DeleteProject(
            DeleteProjectRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "DeleteProject request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
            }

            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            try
            {
                await _projectService.DeleteProjectAsync(projectId, userId, context.CancellationToken);

                _logger.LogInformation(
                    "Project {ProjectId} deleted successfully by user {UserId}",
                    projectId,
                    userId);

                return new Empty();
            }
            catch (KeyNotFoundException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.PermissionDenied, ex.Message));
            }
        }

        //===== SR-SETT-01: Получение настроек пользователя =====
        /// <summary>
        /// Получение глобальных настроек пользователя (SR-SETT-01)
        /// </summary>
        public override async Task<UserSettingsResponse> GetUserSettings(
            GetUserSettingsRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);

            _logger.LogInformation("GetUserSettings request from user {UserId}", userId);

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            try
            {
                var settings = await _userSettingsService.GetUserSettingsAsync(userId, context.CancellationToken);
                return _mapper.Map<UserSettingsResponse>(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user settings");
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        //===== SR-SETT-01: Обновление настроек пользователя =====
        /// <summary>
        /// Обновление глобальных предпочтений пользователя (SR-SETT-01)
        /// </summary>
        public override async Task<UserSettingsResponse> UpdateUserSettings(
            UpdateUserSettingsRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);

            _logger.LogInformation("UpdateUserSettings request from user {UserId}", userId);

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            var dto = _mapper.Map<UpdateUserSettingsDto>(request);

            try
            {
                var settings = await _userSettingsService.UpdateUserSettingsAsync(userId, dto, context.CancellationToken);
                return _mapper.Map<UserSettingsResponse>(settings);
            }
            catch (KeyNotFoundException ex)
            {
                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        //===== SR-STR-03: Получение дерева колод =====
        /// <summary>
        /// Получение дерева колод для проекта (SR-STR-03)
        /// </summary>
        public override async Task<GetDeckTreeResponse> GetDeckTree(
            GetDeckTreeRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "GetDeckTree request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            // Валидация UUID
            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
            }

            var libraryFilter = MapLibraryFilter(request.LibraryFilter);

            try
            {
                var treeItems = await _deckService.GetDeckTreeAsync(projectId, userId, libraryFilter, context.CancellationToken);

                var response = new GetDeckTreeResponse();
                foreach (var item in treeItems)
                {
                    response.RootDecks.Add(_mapper.Map<Pvs.Content.Grpc.DeckTreeItem>(item));
                }

                _logger.LogInformation(
                    "Deck tree retrieved successfully for project {ProjectId}",
                    projectId);

                return response;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deck tree");
                throw new RpcException(
                    new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        private static IDeckService.LibraryFilterKind MapLibraryFilter(LibraryFilter filter)
        {
            return filter switch
            {
                LibraryFilter.Mine => IDeckService.LibraryFilterKind.Mine,
                LibraryFilter.Downloaded => IDeckService.LibraryFilterKind.Downloaded,
                LibraryFilter.Public => IDeckService.LibraryFilterKind.Public,
                _ => IDeckService.LibraryFilterKind.Unspecified
            };
        }

        //===== Получение детальной информации о колоде =====
        /// <summary>
        /// Получение детальной информации о колоде (Id, Title, Description, Stats, ParentDeckId)
        /// </summary>
        public override async Task<GetDeckDetailResponse> GetDeckDetail(
            GetDeckDetailRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "GetDeckDetail request from user {UserId} for deck {DeckId}",
                userId,
                request.DeckId);

            if (!Guid.TryParse(request.DeckId, out var deckId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid deck ID format"));
            }

            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId)
                && requestUserId != userId)
            {
                throw new RpcException(
                    new Status(StatusCode.PermissionDenied, "User ID mismatch"));
            }

            try
            {
                var detail = await _deckService.GetDeckDetailAsync(deckId, userId, context.CancellationToken);

                if (detail == null)
                {
                    throw new RpcException(
                        new Status(StatusCode.NotFound, "Deck not found or access denied"));
                }

                var response = new GetDeckDetailResponse
                {
                    Id = detail.Id.ToString(),
                    Title = detail.Title,
                    Description = detail.Description ?? string.Empty,
                    ParentDeckId = detail.ParentDeckId.HasValue ? detail.ParentDeckId.Value.ToString() : string.Empty,
                    ProjectId = detail.ProjectId.ToString(),
                    OwnerId = detail.OwnerId.ToString(),
                    CoverImageUrl = detail.CoverImageUrl ?? string.Empty,
                    IsPublic = detail.IsPublic,
                    ContributionPolicy = detail.ContributionPolicy ?? string.Empty,
                    LicenseType = detail.LicenseType ?? string.Empty,
                    ForkedFromId = detail.ForkedFromId.HasValue ? detail.ForkedFromId.Value.ToString() : string.Empty,
                    CreatedAt = Timestamp.FromDateTime(detail.CreatedAt.ToUniversalTime()),
                    CardCount = detail.CardCount,
                    Stats = new DeckDetailStats
                    {
                        NewCardsCount = detail.Stats.NewCardsCount,
                        LearningCardsCount = detail.Stats.LearningCardsCount,
                        DueCardsCount = detail.Stats.DueCardsCount,
                        TotalCardsCount = detail.Stats.TotalCardsCount,
                        StudyableNowCount = detail.Stats.StudyableNowCount
                    }
                };

                _logger.LogInformation(
                    "Deck detail {DeckId} retrieved successfully for user {UserId}",
                    request.DeckId,
                    userId);

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deck detail");
                throw new RpcException(
                    new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        //===== SR-VOC-01: Создание колоды =====
        /// <summary>
        /// Создание новой колоды (SR-VOC-01)
        /// </summary>
        public override async Task<DeckResponse> CreateDeck(
            CreateDeckRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "CreateDeck request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            // Валидация UUID
            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
            }

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            try
            {
                // Маппим вручную, так как AutoMapper может не справиться с StringValue
                var dto = new CreateDeckDto
                {
                    UserId = userId,
                    ProjectId = projectId,
                    Title = request.Title,
                    IsPublic = request.IsPublic,
                    CoverImageUrl = string.IsNullOrEmpty(request.CoverImageUrl) ? null : request.CoverImageUrl,
                    ParentDeckId = request.ParentDeckId != null && !string.IsNullOrEmpty(request.ParentDeckId)
                        ? Guid.Parse(request.ParentDeckId)
                        : null,
                    Description = request.Description != null && !string.IsNullOrEmpty(request.Description)
                        ? request.Description
                        : null
                };

                var deck = await _deckService.CreateDeckAsync(dto, context.CancellationToken);
                var response = _mapper.Map<DeckResponse>(deck);

                _logger.LogInformation(
                    "Deck {DeckId} created successfully by user {UserId}",
                    deck.Id,
                    userId);

                return response;
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.FailedPrecondition, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deck");
                throw new RpcException(
                    new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        //===== SR-VOC-01: Обновление колоды =====
        /// <summary>
        /// Обновление колоды (SR-VOC-01)
        /// </summary>
        public override async Task<DeckResponse> UpdateDeck(
            UpdateDeckRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "UpdateDeck request from user {UserId} for deck {DeckId}",
                userId,
                request.DeckId);

            // Валидация UUID
            if (!Guid.TryParse(request.DeckId, out var deckId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid deck ID format"));
            }

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            try
            {
                // Маппим вручную, так как AutoMapper может не справиться с StringValue
                var dto = new UpdateDeckDto
                {
                    Title = request.Title != null && !string.IsNullOrEmpty(request.Title)
                        ? request.Title
                        : null,
                    Description = request.Description != null && !string.IsNullOrEmpty(request.Description)
                        ? request.Description
                        : null,
                    CoverImageUrl = request.CoverImageUrl != null && !string.IsNullOrEmpty(request.CoverImageUrl)
                        ? request.CoverImageUrl
                        : null,
                    ParentDeckId = request.ParentDeckId != null && !string.IsNullOrEmpty(request.ParentDeckId)
                        ? Guid.Parse(request.ParentDeckId)
                        : null,
                    IsPublic = request.IsPublic != null ? request.IsPublic : null,
                    ContributionPolicy = request.PolicyUpdateCase == UpdateDeckRequest.PolicyUpdateOneofCase.ContributionPolicy
                        ? request.ContributionPolicy.ToString()
                        : null,
                    LicenseType = request.LicenseUpdateCase == UpdateDeckRequest.LicenseUpdateOneofCase.LicenseType
                        ? request.LicenseType.ToString()
                        : null
                };

                var deck = await _deckService.UpdateDeckAsync(deckId, userId, dto, context.CancellationToken);
                var response = _mapper.Map<DeckResponse>(deck);

                _logger.LogInformation(
                    "Deck {DeckId} updated successfully by user {UserId}",
                    deckId,
                    userId);

                return response;
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deck");
                throw new RpcException(
                    new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        //===== SR-BG-02: Удаление колоды =====
        /// <summary>
        /// Удаление колоды (SR-BG-02)
        /// </summary>
        public override async Task<Google.Protobuf.WellKnownTypes.Empty> DeleteDeck(
            DeleteDeckRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            var roles = GrpcContextHelper.GetRoles(context);

            _logger.LogInformation(
                "DeleteDeck request from user {UserId} for deck {DeckId}",
                userId,
                request.DeckId);

            // Валидация UUID
            if (!Guid.TryParse(request.DeckId, out var deckId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid deck ID format"));
            }

            // Проверка user_id
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
            {
                if (requestUserId != userId)
                {
                    throw new RpcException(
                        new Status(StatusCode.PermissionDenied, "User ID mismatch"));
                }
            }

            try
            {
                await _deckService.DeleteDeckAsync(deckId, userId, context.CancellationToken);

                _logger.LogInformation(
                    "Deck {DeckId} deleted successfully by user {UserId}",
                    deckId,
                    userId);

                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deck");
                throw new RpcException(
                    new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<GetUserUsageStatsResponse> GetUserUsageStats(
            GetUserUsageStatsRequest request,
            ServerCallContext context)
        {
            var userId = GrpcContextHelper.GetUserId(context);
            if (userId == Guid.Empty && !string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            _logger.LogInformation("GetUserUsageStats request for user {UserId}", userId);

            try
            {
                var stats = await _billingLimitService.GetUserUsageStatsAsync(userId, context.CancellationToken);

                return new GetUserUsageStatsResponse
                {
                    ProjectsUsed = stats.ProjectsUsed,
                    CardsUsed = stats.CardsUsed,
                    AiRequestsTodayUsed = stats.AiRequestsTodayUsed,
                    BooksUsed = stats.BooksUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user usage stats for user {UserId}", userId);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }
    }
}

