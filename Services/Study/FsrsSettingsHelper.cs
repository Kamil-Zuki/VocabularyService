using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services.Study;

internal static class FsrsSettingsHelper
{
    public static FsrsSettings CloneWithoutFuzz(FsrsSettings? settings)
    {
        if (settings == null)
        {
            return new FsrsSettings
            {
                RequestRetention = 0.9,
                MaximumInterval = 36500,
                LearningStepsSeconds = [60, 600],
                RelearningStepsSeconds = [600],
                EnableFuzzing = false,
            };
        }

        return new FsrsSettings
        {
            RequestRetention = settings.RequestRetention,
            MaximumInterval = settings.MaximumInterval,
            W = settings.W,
            LearningStepsSeconds = settings.LearningStepsSeconds,
            RelearningStepsSeconds = settings.RelearningStepsSeconds,
            EnableFuzzing = false,
        };
    }

    public static bool IsIntradayLearningState(short state) => state is 1 or 3;
}
