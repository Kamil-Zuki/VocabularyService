using AutoMapper;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using Pvs.Content.Grpc;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos;
using VocabularyService.Dtos.AI;
using VocabularyService.Dtos.Analytics;
using VocabularyService.Dtos.Community;
using VocabularyService.Dtos.Cards;
using VocabularyService.Dtos.Study;
using VocabularyService.Dtos.Sync;
using VocabularyService.Dtos.Text;
using VocabularyService.Helpers;
using VocabularyService.Options;
using VocabularyService.Services;
using JsonTypes = VocabularyService.Data.Entities.JsonTypes;
using GrpcContributionDto = Pvs.Content.Grpc.ContributionDto;
using GrpcPublishedDeckDto = Pvs.Content.Grpc.PublishedDeckDto;
using GrpcProductDto = Pvs.Content.Grpc.ProductDto;
using GrpcTokenStatus = Pvs.Content.Grpc.TokenStatus;
using GrpcTokenType = Pvs.Content.Grpc.TokenType;

namespace VocabularyService.Mappers;

/// <summary>
/// AutoMapper профиль для маппинга проектов
/// </summary>
public partial class AutoMappingProfile : Profile
{
    public AutoMappingProfile()
    {
        // FsrsSettings -> SrsSettings (gRPC)
        CreateMap<JsonTypes.FsrsSettings, SrsSettings>()
            .ForMember(dest => dest.W, opt => opt.MapFrom(src => src.W.ToList()))
            .ForMember(dest => dest.EnableShortTerm, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.LearningStepsSeconds, opt => opt.MapFrom(src =>
                src.LearningStepsSeconds != null && src.LearningStepsSeconds.Length > 0
                    ? src.LearningStepsSeconds.ToList()
                    : GetDefaultFsrsSettings().LearningStepsSeconds!.ToList()))
            .ForMember(dest => dest.RelearningStepsSeconds, opt => opt.MapFrom(src =>
                src.RelearningStepsSeconds != null && src.RelearningStepsSeconds.Length > 0
                    ? src.RelearningStepsSeconds.ToList()
                    : GetDefaultFsrsSettings().RelearningStepsSeconds!.ToList()))
            .ForMember(dest => dest.EnableFuzzing, opt => opt.MapFrom(src => src.EnableFuzzing ?? true));

        // SrsSettings (gRPC) -> FsrsSettings (Entity)
        CreateMap<SrsSettings, JsonTypes.FsrsSettings>()
            .ForMember(dest => dest.W, opt => opt.MapFrom(src => src.W.ToArray()))
            .ForMember(dest => dest.LearningStepsSeconds, opt => opt.MapFrom(src =>
                src.LearningStepsSeconds.Count > 0 ? src.LearningStepsSeconds.ToArray() : null))
            .ForMember(dest => dest.RelearningStepsSeconds, opt => opt.MapFrom(src =>
                src.RelearningStepsSeconds.Count > 0 ? src.RelearningStepsSeconds.ToArray() : null))
            .ForMember(dest => dest.EnableFuzzing, opt => opt.MapFrom(src => (bool?)src.EnableFuzzing));

        // FsrsPreset (Options) -> FsrsSettings (Entity)
        CreateMap<FsrsPreset, JsonTypes.FsrsSettings>();

        // CreateProjectRequest (gRPC) -> CreateProjectDto
        CreateMap<CreateProjectRequest, CreateProjectDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.UserId)))
            .ForMember(dest => dest.FsrsSettings, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.Settings != null ? context.Mapper.Map<JsonTypes.FsrsSettings>(src.Settings) : null));

        // ProjectStats (Entity) -> ProjectStats (gRPC)
        CreateMap<JsonTypes.ProjectStats, ProjectStats>();

        // TtsSettings (Entity) <-> TtsSettings (gRPC)
        CreateMap<JsonTypes.TtsSettings, TtsSettings>();
        CreateMap<TtsSettings, JsonTypes.TtsSettings>();

        // Project (Entity) -> ProjectResponse (gRPC)
        CreateMap<Project, ProjectResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
            .ForMember(dest => dest.Settings, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.FsrsSettings != null 
                    ? context.Mapper.Map<SrsSettings>(src.FsrsSettings) 
                    : context.Mapper.Map<SrsSettings>(GetDefaultFsrsSettings())))
            .ForMember(dest => dest.TtsSettings, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.TtsSettings != null 
                    ? context.Mapper.Map<TtsSettings>(src.TtsSettings) 
                    : null))
            .ForMember(dest => dest.Stats, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.Stats != null 
                    ? context.Mapper.Map<ProjectStats>(src.Stats) 
                    : context.Mapper.Map<ProjectStats>(GetDefaultProjectStats())))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime())));

        // ========== User Settings ==========

        // UserSetting (Entity) -> UserSettingsResponse (gRPC)
        CreateMap<UserSetting, UserSettingsResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()));

        // UpdateUserSettingsRequest (gRPC) -> UpdateUserSettingsDto
        CreateMap<UpdateUserSettingsRequest, UpdateUserSettingsDto>()
            .ForMember(dest => dest.RolloverHour, opt => opt.MapFrom(src => src.RolloverHour))
            .ForMember(dest => dest.DailyGoalNew, opt => opt.MapFrom(src => src.DailyGoalNew))
            .ForMember(dest => dest.DailyGoalReview, opt => opt.MapFrom(src => src.DailyGoalReview))
            .ForMember(dest => dest.InterfaceLanguage, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.InterfaceLanguage) 
                    ? src.InterfaceLanguage 
                    : null));

        // ========== Колоды ==========

        // CreateDeckRequest (gRPC) -> CreateDeckDto
        CreateMap<CreateDeckRequest, CreateDeckDto>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в gRPC методе
            .ForMember(dest => dest.ProjectId, opt => opt.Ignore()) // Устанавливается отдельно в gRPC методе
            .ForMember(dest => dest.ParentDeckId, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.ParentDeckId) 
                    ? Guid.Parse(src.ParentDeckId) 
                    : (Guid?)null))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Description) 
                    ? src.Description 
                    : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CoverImageUrl) 
                    ? src.CoverImageUrl 
                    : null));

        // UpdateDeckRequest (gRPC) -> UpdateDeckDto
        CreateMap<UpdateDeckRequest, UpdateDeckDto>()
            .ForMember(dest => dest.ContributionPolicy, opt => opt.MapFrom(src => 
                src.PolicyUpdateCase == UpdateDeckRequest.PolicyUpdateOneofCase.ContributionPolicy 
                    ? src.ContributionPolicy.ToString() 
                    : null))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src =>
                src.LicenseUpdateCase == UpdateDeckRequest.LicenseUpdateOneofCase.LicenseType
                    ? src.LicenseType.ToString()
                    : null));

        // Deck (Entity) -> DeckResponse (gRPC)
        CreateMap<Deck, DeckResponse>()
            .ForMember(dest => dest.ContributionPolicy, opt => opt.MapFrom(src =>
                ParseContributionPolicy(src.ContributionPolicy)))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src => 
                ParseLicenseType(src.LicenseType)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime())));

        // DeckTreeItem (Service) -> DeckTreeItem (gRPC) - рекурсивный маппинг
        CreateMap<VocabularyService.Services.DeckTreeItem, Pvs.Content.Grpc.DeckTreeItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CardCount, opt => opt.MapFrom(src => src.CardCount))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId.ToString()))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
            .ForMember(dest => dest.ForkedFromId, opt => opt.MapFrom(src => src.ForkedFromId.HasValue ? src.ForkedFromId.Value.ToString() : string.Empty))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl ?? string.Empty))
            .ForMember(dest => dest.Stats, opt => opt.MapFrom(src => new Pvs.Content.Grpc.DeckDetailStats
            {
                NewCardsCount = src.Stats.NewCardsCount,
                LearningCardsCount = src.Stats.LearningCardsCount,
                DueCardsCount = src.Stats.DueCardsCount,
                TotalCardsCount = src.Stats.TotalCardsCount,
                StudyableNowCount = src.Stats.StudyableNowCount
            }))
            .ForMember(dest => dest.Children, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.Children.Select(child => context.Mapper.Map<Pvs.Content.Grpc.DeckTreeItem>(child)).ToList()));

        // ========== Карточки ==========

        // TargetIndex (Json) <-> TargetIndex (gRPC)
        CreateMap<JsonTypes.TargetIndex, Pvs.Content.Grpc.TargetIndex>().ReverseMap();

        // SourceMeta (Json) <-> SourceMeta (gRPC)
        CreateMap<JsonTypes.SourceMeta, Pvs.Content.Grpc.SourceMeta>().ReverseMap();

        // CardMedia (Json) -> CardMedia (gRPC)
        CreateMap<JsonTypes.CardMedia, Pvs.Content.Grpc.CardMedia>()
            .ForMember(dest => dest.AudioId, opt => opt.MapFrom(src => src.AudioId.HasValue ? src.AudioId.Value.ToString() : string.Empty))
            .ForMember(dest => dest.ImageId, opt => opt.MapFrom(src => src.ImageId.HasValue ? src.ImageId.Value.ToString() : string.Empty))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl ?? string.Empty))
            .ForMember(dest => dest.AudioUrl, opt => opt.MapFrom(src => src.AudioUrl ?? string.Empty));

        // CardMedia (gRPC) -> CardMedia (Json)
        CreateMap<Pvs.Content.Grpc.CardMedia, JsonTypes.CardMedia>()
            .ForMember(dest => dest.AudioId, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.AudioId) ? Guid.Parse(src.AudioId) : (Guid?)null))
            .ForMember(dest => dest.ImageId, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ImageId) ? Guid.Parse(src.ImageId) : (Guid?)null));

        // CreateCardRequest (gRPC) -> VocabularyService.Dtos.Cards.CreateCardDto
        CreateMap<CreateCardRequest, VocabularyService.Dtos.Cards.CreateCardDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => ParseGuidFromGrpcString(src.UserId)))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => ParseGuidFromGrpcString(src.DeckId)))
            .ForMember(dest => dest.FieldValues, opt => opt.MapFrom(src => NoteFieldMapHelper.FromProtoMap(src.FieldValues)));

        // CaptureCardRequest (gRPC) -> VocabularyService.Dtos.Cards.CaptureCardDto
        CreateMap<CaptureCardRequest, CaptureCardDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.UserId)))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => Guid.Parse(src.ProjectId)))
            .ForMember(dest => dest.FieldValues, opt => opt.MapFrom(src => NoteFieldMapHelper.FromProtoMap(src.FieldValues)))
            .ForMember(dest => dest.ScreenshotBase64, opt => opt.MapFrom(src =>
                src.ScreenshotBase64 == null || string.IsNullOrEmpty(src.ScreenshotBase64) ? null : src.ScreenshotBase64))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => ParseOptionalDeckGuid(src.DeckId)));

        CreateMap<CardDuplicatePreviewDto, CardPreview>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .ForMember(dest => dest.HasAudio, opt => opt.MapFrom(src => src.HasAudio))
            .ForMember(dest => dest.DeckTitle, opt => opt.MapFrom(src => src.DeckTitle))
            .ForMember(dest => dest.SrsStatus, opt => opt.MapFrom(src => MapDuplicatePreviewSrsToGrpc(src.SrsStatus)))
            .AfterMap((src, dest) =>
            {
                dest.Note = BuildGrpcNotePayload(src.NoteId, src.NoteTypeId, src.ProjectTermId, src.FieldValues);
            });

        // UpdateCardRequest (gRPC) -> VocabularyService.Dtos.Cards.UpdateCardDto
        CreateMap<UpdateCardRequest, VocabularyService.Dtos.Cards.UpdateCardDto>()
            .ForMember(dest => dest.FieldValues, opt => opt.MapFrom(src => NoteFieldMapHelper.FromProtoMap(src.FieldValues)));

        // Card (Entity) -> CardResponse (gRPC)
        CreateMap<Card, CardResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()))
            .ForMember(dest => dest.CreatorId, opt => opt.MapFrom(src => src.CreatorId.ToString()))
            .ForMember(dest => dest.ProjectTermId, opt => opt.MapFrom(src =>
                src.ProjectTermId.HasValue ? new StringValue { Value = src.ProjectTermId.Value.ToString() } : null))
            .ForMember(dest => dest.SrsStatus, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime())))
            // Navigations: filled manually in AfterMap (no CreateMap<Note, NotePayload> / CardTemplate payload).
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .ForMember(dest => dest.ActiveCardTemplate, opt => opt.Ignore())
            .ForMember(dest => dest.SrsState, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.Note != null)
                {
                    dest.Note = BuildGrpcNotePayload(src.Note.Id, src.Note.NoteTypeId,
                        src.Note.ProjectTermId?.ToString("D"), src.Note.FieldValues);
                }

                if (src.CardTemplate != null)
                {
                    dest.ActiveCardTemplate = new CardTemplatePayload
                    {
                        Id = src.CardTemplate.Id.ToString(),
                        TemplateKey = src.CardTemplate.TemplateKey,
                        Name = src.CardTemplate.Name,
                        FrontTemplate = src.CardTemplate.FrontTemplate,
                        BackTemplate = src.CardTemplate.BackTemplate,
                        SortOrder = src.CardTemplate.SortOrder,
                        Enabled = src.CardTemplate.Enabled,
                    };
                }

                var progress = src.UserCardProgresses.FirstOrDefault();
                if (progress != null)
                {
                    dest.SrsStatus = MapProgressStateToGrpc(progress);
                    dest.SrsState = new SrsState
                    {
                        State = MapProgressStateToGrpc(progress),
                        CurrentInterval = progress.ScheduledDays,
                        Step = progress.Step,
                        DueUtc = Timestamp.FromDateTime(progress.Due.ToUniversalTime()),
                        Lapses = progress.Lapses,
                        Stability = progress.Stability,
                        Difficulty = progress.Difficulty,
                        ScheduledDays = progress.ScheduledDays,
                        ElapsedDays = progress.ElapsedDays,
                    };
                }
                else
                {
                    dest.SrsStatus = SrsStatus.New;
                    dest.SrsState = new SrsState { State = SrsStatus.New, CurrentInterval = 0 };
                }
            });

        CreateMap<Card, CardPreview>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .ForMember(dest => dest.HasAudio, opt => opt.MapFrom(src =>
                src.Note != null && NoteFieldMapHelper.HasAudio(src.Note.FieldValues)))
            .ForMember(dest => dest.DeckTitle, opt => opt.MapFrom(src => src.Deck != null ? src.Deck.Title : string.Empty))
            .ForMember(dest => dest.SrsStatus, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.Note == null) return;
                dest.Note = BuildGrpcNotePayload(src.Note.Id, src.Note.NoteTypeId,
                    src.Note.ProjectTermId?.ToString("D"), src.Note.FieldValues);
            });

        // ============================================================================
        // Study Service Mappings
        // ============================================================================

        // StartStudySessionRequest (gRPC) -> StartStudySessionDto
        CreateMap<StartStudySessionRequest, StartStudySessionDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.UserId)))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => Guid.Parse(src.ProjectId)))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.DeckId) ? Guid.Parse(src.DeckId) : (Guid?)null));

        // StudySessionDto -> StartStudySessionResponse (gRPC)
        CreateMap<StudySessionDto, StartStudySessionResponse>()
            .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.StartTime.ToUniversalTime())))
            .ForMember(dest => dest.CardsReviewed, opt => opt.MapFrom(src => src.CardsReviewed))
            .ForMember(dest => dest.QueueStats, opt => opt.MapFrom(src => src.QueueStats));

        // QueueStatsDto -> QueueStats (gRPC)
        CreateMap<QueueStatsDto, QueueStats>();

        // CardStudyDto -> GetNextCardResponse (gRPC)
        CreateMap<VocabularyService.Dtos.Study.CardStudyDto, Pvs.Content.Grpc.CardStudyDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.SourceMeta, opt => opt.MapFrom(src => src.SourceMeta))
            .ForMember(dest => dest.Media, opt => opt.MapFrom(src => src.Media))
            .ForMember(dest => dest.SrsState, opt => opt.MapFrom(src => src.SrsState))
            .ForMember(dest => dest.NextIntervals, opt => opt.MapFrom(src => src.NextIntervals))
            .ForMember(dest => dest.SiblingsCount, opt => opt.MapFrom(src => src.SiblingsCount));

        // CardStudyContentDto -> CardStudyContent (gRPC)
        CreateMap<CardStudyContentDto, CardStudyContent>()
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .ForMember(dest => dest.TargetIndex, opt => opt.MapFrom(src => src.TargetIndex))
            .AfterMap((src, dest) =>
            {
                dest.Note = BuildGrpcNotePayload(src.NoteId, src.NoteTypeId, src.ProjectTermId, src.FieldValues);
            });

        // SrsStateDto -> SrsState (gRPC)
        CreateMap<SrsStateDto, SrsState>()
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => MapSrsStatusStringToEnum(src.State)))
            .ForMember(dest => dest.CurrentInterval, opt => opt.MapFrom(src => src.CurrentInterval))
            .ForMember(dest => dest.Step, opt => opt.MapFrom(src => src.Step))
            .ForMember(dest => dest.DueUtc, opt => opt.MapFrom(src => 
                src.DueUtc.HasValue 
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(src.DueUtc.Value, DateTimeKind.Utc)) 
                    : null));

        // SubmitReviewRequest (gRPC) -> SubmitReviewDto
        CreateMap<SubmitReviewRequest, SubmitReviewDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.UserId)))
            .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => Guid.Parse(src.SessionId)))
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => Guid.Parse(src.CardId)))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.DurationMs, opt => opt.MapFrom(src => src.DurationMs))
            .ForMember(dest => dest.UserAnswer, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.UserAnswer) 
                    ? src.UserAnswer 
                    : null));

        // AnswerValidationResultDto -> AnswerValidationResult (gRPC)
        CreateMap<AnswerValidationResultDto, Pvs.Content.Grpc.AnswerValidationResult>()
            .ForMember(dest => dest.IsCorrect, opt => opt.MapFrom(src => src.IsCorrect))
            .ForMember(dest => dest.IsFuzzyMatch, opt => opt.MapFrom(src => src.IsFuzzyMatch))
            .ForMember(dest => dest.MatchedSynonym, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.MatchedSynonym) 
                    ? src.MatchedSynonym 
                    : null))
            .ForMember(dest => dest.SimilarityScore, opt => opt.MapFrom(src => src.SimilarityScore))
            .ForMember(dest => dest.Suggestion, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Suggestion) 
                    ? src.Suggestion 
                    : null));

        // ReviewResponseDto -> SubmitReviewResponse (gRPC)
        CreateMap<ReviewResponseDto, SubmitReviewResponse>()
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => src.CardId.ToString()))
            .ForMember(dest => dest.NextReviewDate, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.NextReviewDate.ToUniversalTime())))
            .ForMember(dest => dest.Interval, opt => opt.MapFrom(src => src.Interval))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => MapSrsStatusStringToEnum(src.State)))
            .ForMember(dest => dest.Stability, opt => opt.MapFrom(src => src.Stability))
            .ForMember(dest => dest.IsLeech, opt => opt.MapFrom(src => src.IsLeech))
            .ForMember(dest => dest.BuriedSiblingsCount, opt => opt.MapFrom(src => src.BuriedSiblingsCount))
            .ForMember(dest => dest.AnswerValidation, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.AnswerValidation != null 
                    ? context.Mapper.Map<Pvs.Content.Grpc.AnswerValidationResult>(src.AnswerValidation) 
                    : null));

        // UndoReviewRequest (gRPC) -> UndoReviewDto (no mapping needed, handled in service)
        // UndoReviewDto -> UndoReviewResponse (gRPC)
        CreateMap<UndoReviewDto, UndoReviewResponse>()
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.Success))
            .ForMember(dest => dest.RestoredCardId, opt => opt.MapFrom(src => src.RestoredCardId.ToString()))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message));

        // ============================================================================
        // Analytics Service Mappings
        // ============================================================================

        // VocabularyStatsDto -> GetVocabularyStatsResponse (gRPC)
        CreateMap<VocabularyStatsDto, GetVocabularyStatsResponse>()
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.CefrLevel, opt => opt.MapFrom(src => src.CefrLevel));

        // CefrLevelDto -> CefrLevel (gRPC)
        CreateMap<CefrLevelDto, CefrLevel>();

        // HeatmapDto -> GetHeatmapResponse (gRPC)
        // Activity is a get-only MapField; populate it in AfterMap instead of assigning a Dictionary.
        CreateMap<HeatmapDto, GetHeatmapResponse>()
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src =>
                src.ProjectId.HasValue
                    ? new StringValue { Value = src.ProjectId.Value.ToString() }
                    : null))
            .ForMember(dest => dest.TotalTimeSpentSeconds, opt => opt.MapFrom(src => src.TotalTimeSpentSeconds))
            .ForMember(dest => dest.Activity, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.Activity == null) return;
                dest.Activity.Clear();
                foreach (var kvp in src.Activity)
                {
                    dest.Activity[kvp.Key.ToString("yyyy-MM-dd")] = new ActivityDay
                    {
                        Count = kvp.Value.Count,
                        Level = kvp.Value.Level
                    };
                }
            });

        // ActivityDayDto -> ActivityDay (gRPC)
        CreateMap<ActivityDayDto, ActivityDay>();

        // DailySummaryDto -> GetDailySummaryResponse (gRPC)
        CreateMap<DailySummaryDto, GetDailySummaryResponse>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.NewCards, opt => opt.MapFrom(src => src.NewCards))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews));

        // GoalProgressDto -> GoalProgress (gRPC)
        CreateMap<GoalProgressDto, GoalProgress>();

        // ============================================================================
        // Community Service Mappings
        // ============================================================================

        // Contributions
        CreateMap<CreateContributionRequest, CreateContributionDto>()
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => Guid.Parse(src.DeckId)))
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CardId) ? Guid.Parse(src.CardId) : (Guid?)null))
            .ForMember(dest => dest.Content, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.Content != null ? MapCardContentToPayload(src.Content) : null))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Comment) ? src.Comment : null));

        CreateMap<VocabularyService.Dtos.Community.ContributionDto, GrpcContributionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.TargetDeckId, opt => opt.MapFrom(src => src.TargetDeckId.ToString()))
            .ForMember(dest => dest.TargetCardId, opt => opt.MapFrom(src => 
                src.TargetCardId.HasValue ? new StringValue { Value = src.TargetCardId.Value.ToString() } : null))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Content, opt => opt.MapFrom((src, dest, destMember, context) => 
                MapPayloadToCardContent(src.Content)))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Comment) ? new StringValue { Value = src.Comment } : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.UpdatedAt, DateTimeKind.Utc))));

        CreateMap<VocabularyService.Dtos.Community.AuthorInfoDto, AuthorInfo>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.DisplayName) ? src.DisplayName : string.Empty));

        CreateMap<VocabularyService.Dtos.Community.ContributionDiffDto, ContributionDiff>()
            .ForMember(dest => dest.OriginalCard, opt => opt.MapFrom((src, dest, destMember, context) => 
                src.OriginalCard != null ? MapPayloadToCardContent(src.OriginalCard) : null))
            .ForMember(dest => dest.ProposedCard, opt => opt.MapFrom((src, dest, destMember, context) => 
                MapPayloadToCardContent(src.ProposedCard)))
            .ForMember(dest => dest.ChangedFields, opt => opt.MapFrom(src => src.ChangedFields));

        CreateMap<ResolveContributionRequest, ResolveContributionDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.ResolutionComment, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.ResolutionComment) ? src.ResolutionComment : null));

        // Publishing
        CreateMap<PublishDeckRequest, PublishDeckDto>()
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => Guid.Parse(src.DeckId)))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src => src.LicenseType));

        CreateMap<ForkDeckRequest, ForkDeckDto>()
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => Guid.Parse(src.DeckId)))
            .ForMember(dest => dest.TargetProjectId, opt => opt.MapFrom(src => Guid.Parse(src.TargetProjectId)))
            .ForMember(dest => dest.NewTitle, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.NewTitle) ? src.NewTitle : null));

        CreateMap<VocabularyService.Dtos.Community.PublishedDeckDto, GrpcPublishedDeckDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Description) ? new StringValue { Value = src.Description } : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CoverImageUrl) ? new StringValue { Value = src.CoverImageUrl } : null))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime())))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.UpdatedAt.ToUniversalTime())));

        CreateMap<VocabularyService.Dtos.Community.AuthorProfileDto, GetAuthorProfileResponse>()
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.AuthorId.ToString()))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.DisplayName) ? src.DisplayName : string.Empty));

        // Marketplace
        CreateMap<CreateProductRequest, CreateProductDto>()
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => Guid.Parse(src.DeckId)))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => (decimal)src.Price))
            .ForMember(dest => dest.DescriptionHtml, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.DescriptionHtml) ? src.DescriptionHtml : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CoverImageUrl) ? src.CoverImageUrl : null));

        CreateMap<UpdateProductRequest, UpdateProductDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Title) ? src.Title : null))
            .ForMember(dest => dest.DescriptionHtml, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.DescriptionHtml) ? src.DescriptionHtml : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CoverImageUrl) ? src.CoverImageUrl : null))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => 
                src.Price.HasValue ? (decimal?)src.Price.Value : null))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Currency) ? src.Currency : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Status) ? src.Status : null));

        CreateMap<VocabularyService.Dtos.Community.ProductDto, GrpcProductDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => (double)src.Price))
            .ForMember(dest => dest.DescriptionHtml, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.DescriptionHtml) ? new StringValue { Value = src.DescriptionHtml } : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CoverImageUrl) ? new StringValue { Value = src.CoverImageUrl } : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime())))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.UpdatedAt.ToUniversalTime())));

        CreateMap<CreateReviewRequest, CreateReviewDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => Guid.Parse(src.ProductId)))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Comment) ? src.Comment : null));

        CreateMap<VocabularyService.Dtos.Community.ProductStatsDto, GetProductStatsResponse>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId.ToString()))
            .ForMember(dest => dest.RetentionRate, opt => opt.MapFrom(src => 
                src.RetentionRate.HasValue ? new DoubleValue { Value = src.RetentionRate.Value } : null));

        CreateMap<VocabularyService.Dtos.Community.EntitlementDto, CheckEntitlementResponse>()
            .ForMember(dest => dest.GrantedAt, opt => opt.MapFrom(src => 
                src.GrantedAt.HasValue ? Timestamp.FromDateTime(src.GrantedAt.Value.ToUniversalTime()) : null));

        // ============================================================================
        // Sync Service Mappings
        // ============================================================================

        // SyncDataResponseDto -> SyncDataResponse (gRPC)
        CreateMap<SyncDataResponseDto, SyncDataResponse>()
            .ForMember(dest => dest.SyncToken, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.SyncToken, DateTimeKind.Utc))))
            .ForMember(dest => dest.Changes, opt => opt.MapFrom(src => src.Changes))
            .ForMember(dest => dest.DeletedObjects, opt => opt.MapFrom(src => src.DeletedObjects));

        // SyncChangesDto -> SyncChanges (gRPC)
        CreateMap<SyncChangesDto, SyncChanges>()
            .ForMember(dest => dest.Decks, opt => opt.MapFrom(src => src.Decks))
            .ForMember(dest => dest.Cards, opt => opt.MapFrom(src => src.Cards))
            .ForMember(dest => dest.Progress, opt => opt.MapFrom(src => src.Progress));

        // SyncDeckDto -> SyncDeck (gRPC)
        CreateMap<SyncDeckDto, SyncDeck>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.ParentDeckId, opt => opt.MapFrom(src => 
                src.ParentDeckId.HasValue ? new StringValue { Value = src.ParentDeckId.Value.ToString() } : null))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId.ToString()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Description) ? new StringValue { Value = src.Description } : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CoverImageUrl) ? new StringValue { Value = src.CoverImageUrl } : null))
            .ForMember(dest => dest.ContributionPolicy, opt => opt.MapFrom(src => 
                ParseContributionPolicy(src.ContributionPolicy)))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src => 
                ParseLicenseType(src.LicenseType)))
            .ForMember(dest => dest.ForkedFromId, opt => opt.MapFrom(src => 
                src.ForkedFromId.HasValue ? new StringValue { Value = src.ForkedFromId.Value.ToString() } : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.UpdatedAt, DateTimeKind.Utc))));

        // SyncCardDto -> SyncCard (gRPC)
        CreateMap<SyncCardDto, SyncCard>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()))
            .ForMember(dest => dest.CreatorId, opt => opt.MapFrom(src => src.CreatorId.ToString()))
            .ForMember(dest => dest.NoteId, opt => opt.MapFrom(src => src.NoteId.ToString()))
            .ForMember(dest => dest.SearchDocument, opt => opt.MapFrom(src => src.SearchDocument))
            .ForMember(dest => dest.ProjectTermId, opt => opt.MapFrom(src =>
                src.ProjectTermId.HasValue ? new StringValue { Value = src.ProjectTermId.Value.ToString() } : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src =>
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src =>
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.UpdatedAt, DateTimeKind.Utc))))
            .AfterMap((src, dest) =>
            {
                foreach (var kv in src.FieldValues)
                {
                    var p = new NoteFieldValuePayload();
                    if (!string.IsNullOrEmpty(kv.Value.String))
                        p.StringValue = kv.Value.String;
                    if (kv.Value.Strings is { Count: > 0 })
                        p.StringValues.AddRange(kv.Value.Strings);
                    dest.FieldValues[kv.Key] = p;
                }
            });

        // SyncProgressDto -> UserCardProgressDto (gRPC)
        CreateMap<SyncProgressDto, UserCardProgressDto>()
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => src.CardId.ToString()))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.Due, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.Due, DateTimeKind.Utc))))
            .ForMember(dest => dest.LastReview, opt => opt.MapFrom(src => 
                Timestamp.FromDateTime(DateTime.SpecifyKind(src.LastReview, DateTimeKind.Utc))));

        // DeletedObjectInfoDto -> DeletedObjectInfo (gRPC)
        CreateMap<DeletedObjectInfoDto, DeletedObjectInfo>()
            .ForMember(dest => dest.EntityId, opt => opt.MapFrom(src => src.EntityId.ToString()));

        // BatchSubmitReviewsRequest (gRPC) -> BatchSubmitReviewsRequestDto
        CreateMap<BatchSubmitReviewsRequest, BatchSubmitReviewsRequestDto>()
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews));

        // BatchReviewItem (gRPC) -> BatchReviewItemDto
        CreateMap<BatchReviewItem, BatchReviewItemDto>()
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => Guid.Parse(src.CardId)))
            .ForMember(dest => dest.ReviewedAt, opt => opt.MapFrom(src => 
                src.ReviewedAt.ToDateTime()))
            .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.SessionId) ? Guid.Parse(src.SessionId) : (Guid?)null))
            .ForMember(dest => dest.UserAnswer, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.UserAnswer) ? src.UserAnswer : null));

        // BatchSubmitReviewsResponseDto -> BatchSubmitReviewsResponse (gRPC)
        CreateMap<BatchSubmitReviewsResponseDto, BatchSubmitReviewsResponse>()
            .ForMember(dest => dest.FailedCardIds, opt => opt.MapFrom(src => 
                src.FailedCardIds.Select(id => id.ToString())));

        // ============================================================================
        // AI Service Mappings
        // ============================================================================

        // GenerateContextRequest (gRPC) -> GenerateContextRequestDto
        CreateMap<GenerateContextRequest, GenerateContextRequestDto>();

        // ContextSuggestionDto -> ContextSuggestion (gRPC)
        CreateMap<ContextSuggestionDto, ContextSuggestion>()
            .ForMember(dest => dest.TargetIndex, opt => opt.MapFrom(src => src.TargetIndex));

        // GenerateContextResponseDto -> GenerateContextResponse (gRPC)
        CreateMap<GenerateContextResponseDto, GenerateContextResponse>();

        // ExplainGrammarRequest (gRPC) -> ExplainGrammarRequestDto
        CreateMap<ExplainGrammarRequest, ExplainGrammarRequestDto>()
            .ForMember(dest => dest.ContextPrompt, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ContextPrompt) ? src.ContextPrompt : null));

        // ExplainGrammarResponseDto -> ExplainGrammarResponse (gRPC)
        CreateMap<ExplainGrammarResponseDto, ExplainGrammarResponse>()
            .ForMember(dest => dest.RelatedTopic, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.RelatedTopic) ? new StringValue { Value = src.RelatedTopic } : null));

        // ============================================================================
        // Text Service Mappings
        // ============================================================================

        // AnalyzeTextRequest (gRPC) -> AnalyzeTextRequestDto
        CreateMap<AnalyzeTextRequest, AnalyzeTextRequestDto>()
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => Guid.Parse(src.ProjectId)));

        // TextTokenDto <-> TextToken (gRPC)
        CreateMap<TextTokenDto, TextToken>()
            .ForMember(dest => dest.TermText, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.TermText) ? new StringValue { Value = src.TermText } : null))
            .ForMember(dest => dest.ProjectTermId, opt => opt.MapFrom(src =>
                src.ProjectTermId.HasValue ? src.ProjectTermId.Value.ToString() : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (GrpcTokenStatus)(int)src.Status))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (GrpcTokenType)(int)src.Type));

        CreateMap<TextPhraseDto, TextPhrase>()
            .ForMember(dest => dest.ProjectTermId, opt => opt.MapFrom(src =>
                src.ProjectTermId.HasValue ? src.ProjectTermId.Value.ToString() : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (GrpcTokenStatus)(int)src.Status));

        // TextAnalysisStatsDto -> TextAnalysisStats (gRPC)
        CreateMap<TextAnalysisStatsDto, TextAnalysisStats>();

        // AnalyzeTextResponseDto -> AnalyzeTextResponse (gRPC)
        CreateMap<AnalyzeTextResponseDto, AnalyzeTextResponse>();
    }

    private static JsonTypes.ContributionPayload MapCardContentToPayload(Pvs.Content.Grpc.CardContent content)
    {
        return new JsonTypes.ContributionPayload
        {
            FieldValues = NoteFieldMapHelper.FromProtoMap(content.FieldValues),
        };
    }

    private static Pvs.Content.Grpc.CardContent MapPayloadToCardContent(JsonTypes.ContributionPayload payload)
    {
        var content = new Pvs.Content.Grpc.CardContent();
        foreach (var kv in payload.FieldValues)
        {
            var p = new NoteFieldValuePayload();
            if (!string.IsNullOrEmpty(kv.Value.String))
                p.StringValue = kv.Value.String;
            if (kv.Value.Strings is { Count: > 0 })
                p.StringValues.AddRange(kv.Value.Strings);
            content.FieldValues[kv.Key] = p;
        }

        return content;
    }

    private static SrsStatus MapSrsStatusStringToEnum(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "NEW" => SrsStatus.New,
            "LEARNING" => SrsStatus.Learning,
            "REVIEW" => SrsStatus.Review,
            "RELEARNING" => SrsStatus.Relearning,
            "MATURE" => SrsStatus.Mature,
            _ => SrsStatus.New
        };
    }

    private static SrsStatus MapProgressStateToGrpc(UserCardProgress progress)
    {
        return progress.State switch
        {
            0 => SrsStatus.New,
            1 => SrsStatus.Learning,
            2 when progress.ScheduledDays >= 21 => SrsStatus.Mature,
            2 => SrsStatus.Review,
            3 => SrsStatus.Relearning,
            _ => SrsStatus.New
        };
    }

    /// <summary>Парсинг UUID из gRPC string; пусто/мусор → Empty (BulkCreate дополняет в хендлере).</summary>
    private static Guid ParseGuidFromGrpcString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Guid.Empty;
        return Guid.TryParse(value.Trim(), out var g) ? g : Guid.Empty;
    }

    private static ContributionPolicy ParseContributionPolicy(string? value)
    {
        if (string.IsNullOrEmpty(value)) return ContributionPolicy.Closed;
        return System.Enum.TryParse<ContributionPolicy>(value, true, out var policy) ? policy : ContributionPolicy.Closed;
    }

    private static LicenseType ParseLicenseType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return LicenseType.Private;
        return System.Enum.TryParse<LicenseType>(value, true, out var license) ? license : LicenseType.Private;
    }

    /// <summary>
    /// Получает дефолтные настройки FSRS
    /// </summary>
    private static JsonTypes.FsrsSettings GetDefaultFsrsSettings()
    {
        return new JsonTypes.FsrsSettings
        {
            RequestRetention = 0.9,
            MaximumInterval = 36500,
            W = GetDefaultWeights(),
            // Как дефолты py-fsrs / типичный пресет Anki: 1 мин, 10 мин; relearning 10 мин
            LearningStepsSeconds = [60, 600],
            RelearningStepsSeconds = [600],
            EnableFuzzing = true
        };
    }

    /// <summary>
    /// Получает дефолтную статистику проекта
    /// </summary>
    private static JsonTypes.ProjectStats GetDefaultProjectStats()
    {
        return new JsonTypes.ProjectStats
        {
            TotalLemmas = 0,
            MatureLemmas = 0
        };
    }

    /// <summary>
    /// Получает дефолтные веса FSRS (стандартные веса FSRS v5)
    /// </summary>
    private static double[] GetDefaultWeights()
    {
        // Стандартные веса FSRS v5 (18 значений)
        return
        [
            0.4, 0.6, 2.4, 5.8, 4.93, 0.94, 0.86, 0.01, 1.49, 0.14, 0.94,
            2.18, 0.05, 0.34, 1.26, 0.29, 2.61, 0.0
        ];
    }

    /// <summary>Aligns duplicate-check previews with CardGrpcService SRS string labels.</summary>
    private static SrsStatus MapDuplicatePreviewSrsToGrpc(string status) =>
        (status ?? "").Trim().ToUpperInvariant() switch
        {
            "NEW" => SrsStatus.New,
            "LEARNING" => SrsStatus.Learning,
            "REVIEW" => SrsStatus.Review,
            "RELEARNING" => SrsStatus.Relearning,
            "MATURE" => SrsStatus.Mature,
            _ => SrsStatus.New,
        };

    private static Guid? ParseOptionalDeckGuid(string? deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId)) return null;
        return Guid.TryParse(deckId.Trim(), out var g) ? g : null;
    }

    private static NotePayload BuildGrpcNotePayload(
        Guid noteId,
        Guid noteTypeId,
        string? projectTermId,
        Dictionary<string, JsonTypes.NoteFieldValue> fieldValues)
    {
        var note = new NotePayload
        {
            Id = noteId.ToString(),
            NoteTypeId = noteTypeId.ToString(),
        };
        if (!string.IsNullOrEmpty(projectTermId))
            note.ProjectTermId = projectTermId;

        foreach (var kv in fieldValues)
        {
            var p = new NoteFieldValuePayload();
            if (!string.IsNullOrEmpty(kv.Value.String))
                p.StringValue = kv.Value.String;
            if (kv.Value.Strings is { Count: > 0 })
                p.StringValues.AddRange(kv.Value.Strings);
            note.FieldValues[kv.Key] = p;
        }

        return note;
    }
}
