using Google.Protobuf.WellKnownTypes;
using VocabularyService.Data.Entities;
using Vocab;

namespace VocabularyService.Services;

/// <summary>
/// Маппинг UserCardProgress ↔ Vocab.Card (inclusive proto).
/// </summary>
public static class InclusiveCardMapper
{
    /// <summary>
    /// Преобразует UserCardProgress в proto Card для inclusive/py-fsrs.
    /// <para>
    /// Contract: internal <c>State=0</c> (NEW) is sent on the wire as 0; inclusive maps it to
    /// <c>State.Learning</c> for the first <c>review_card</c> call. Step must be forwarded for learning/relearning ladders.
    /// </para>
    /// </summary>
    public static Vocab.Card ToInclusiveCard(UserCardProgress progress)
    {
        var card = new Vocab.Card
        {
            // NEW cards use state 0; inclusive/main.py maps 0 -> py-fsrs Learning on first review.
            State = progress.State,
            Due = Timestamp.FromDateTime(DateTime.SpecifyKind(progress.Due, DateTimeKind.Utc)),
            LastReview = Timestamp.FromDateTime(DateTime.SpecifyKind(progress.LastReview, DateTimeKind.Utc))
        };
        card.Step = progress.Step;
        card.Stability = progress.Stability;
        card.Difficulty = progress.Difficulty;
        return card;
    }

    /// <summary>
    /// Извлекает из proto Card значения в FsrsNextState.
    /// </summary>
    public static FsrsNextState FromInclusiveCard(Vocab.Card card, DateTime reviewAt)
    {
        var state = (short)card.State;
        var step = card.Step ?? 0;
        var stability = card.Stability ?? 0f;
        var difficulty = card.Difficulty ?? 0f;
        var due = card.Due?.ToDateTime() ?? reviewAt;
        if (due.Kind != DateTimeKind.Utc)
            due = DateTime.SpecifyKind(due, DateTimeKind.Utc);
        return new FsrsNextState(stability, difficulty, due, state, step);
    }
}
