using Google.Protobuf.WellKnownTypes;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using Vocab;

namespace VocabularyService.Services;

/// <summary>
/// FSRS scheduler backed only by the inclusive py-fsrs service.
/// </summary>
public class InclusiveFsrsScheduler : IFsrsScheduler
{
    private readonly Vocab.VocabService.VocabServiceClient _client;
    private readonly ILogger<InclusiveFsrsScheduler> _logger;

    public InclusiveFsrsScheduler(
        Vocab.VocabService.VocabServiceClient client,
        ILogger<InclusiveFsrsScheduler> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FsrsNextState> GetNextStateAsync(
        UserCardProgress progress,
        int rating,
        DateTime reviewAt,
        int durationMs,
        FsrsSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var request = new ReviewRequest
        {
            Card = InclusiveCardMapper.ToInclusiveCard(progress),
            Quality = rating,
            ReviewDuration = durationMs > 0 ? durationMs : null,
            ReviewAt = Timestamp.FromDateTime(DateTime.SpecifyKind(reviewAt, DateTimeKind.Utc))
        };

        if (settings != null)
        {
            if (settings.RequestRetention > 0)
                request.RequestRetention = settings.RequestRetention;
            if (settings.MaximumInterval > 0)
                request.MaximumInterval = settings.MaximumInterval;
            if (settings.W is { Length: > 0 })
                request.W.AddRange(settings.W);
            if (settings.LearningStepsSeconds is { Length: > 0 })
                request.LearningStepsSeconds.AddRange(settings.LearningStepsSeconds);
            if (settings.RelearningStepsSeconds is { Length: > 0 })
                request.RelearningStepsSeconds.AddRange(settings.RelearningStepsSeconds);
            request.EnableFuzzing = settings.EnableFuzzing ?? true;
        }

        var response = await _client.ReviewCardAsync(request, cancellationToken: cancellationToken);
        if (response?.Card == null)
        {
            throw new InvalidOperationException("Inclusive ReviewCard returned an empty card");
        }

        var nextState = InclusiveCardMapper.FromInclusiveCard(response.Card, reviewAt);
        if (nextState.State == 0)
        {
            _logger.LogWarning("Inclusive returned state 0; coercing to Learning (1) for FSRS parity");
            return nextState with { State = 1 };
        }

        return nextState;
    }
}
