using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public static class A1A2LessonsSeeder
{
    public static List<Lesson> GetLessons()
    {
        return new List<Lesson>
        {
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000001"),
                Title = "Глагол to be: I am, he is, they are",
                Description = "Master the most fundamental verb in English.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 1,
                TargetSkills = "R,W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Глагол to be: I am, he is, they are

### Rule
This is a basic rule for Глагол to be: I am, he is, they are.

### Examples
- Example 1 for Глагол to be: I am, he is, they are
- Example 2 for Глагол to be: I am, he is, they are
""",
                SystemPrompt = "You are an English tutor helping the student practice Глагол to be: I am, he is, they are. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000002"),
                Title = "Указательные местоимения: this/that/these/those",
                Description = "Learn to point things out in English.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 2,
                TargetSkills = "R,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Указательные местоимения: this/that/these/those

### Rule
This is a basic rule for Указательные местоимения: this/that/these/those.

### Examples
- Example 1 for Указательные местоимения: this/that/these/those
- Example 2 for Указательные местоимения: this/that/these/those
""",
                SystemPrompt = "You are an English tutor helping the student practice Указательные местоимения: this/that/these/those. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000003"),
                Title = "Существительные: ед. и мн. число",
                Description = "Learn how to make nouns plural.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 3,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Существительные: ед. и мн. число

### Rule
This is a basic rule for Существительные: ед. и мн. число.

### Examples
- Example 1 for Существительные: ед. и мн. число
- Example 2 for Существительные: ед. и мн. число
""",
                SystemPrompt = "You are an English tutor helping the student practice Существительные: ед. и мн. число. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000004"),
                Title = "Притяжательные местоимения: my/your/his",
                Description = "Talk about possession and ownership.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 4,
                TargetSkills = "R,W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Притяжательные местоимения: my/your/his

### Rule
This is a basic rule for Притяжательные местоимения: my/your/his.

### Examples
- Example 1 for Притяжательные местоимения: my/your/his
- Example 2 for Притяжательные местоимения: my/your/his
""",
                SystemPrompt = "You are an English tutor helping the student practice Притяжательные местоимения: my/your/his. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000005"),
                Title = "Present Simple: утверждения",
                Description = "Talk about facts and daily routines.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 5,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Present Simple: утверждения

### Rule
This is a basic rule for Present Simple: утверждения.

### Examples
- Example 1 for Present Simple: утверждения
- Example 2 for Present Simple: утверждения
""",
                SystemPrompt = "You are an English tutor helping the student practice Present Simple: утверждения. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000006"),
                Title = "Present Simple: вопросы и отрицания",
                Description = "Learn to ask questions and say no.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 6,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Present Simple: вопросы и отрицания

### Rule
This is a basic rule for Present Simple: вопросы и отрицания.

### Examples
- Example 1 for Present Simple: вопросы и отрицания
- Example 2 for Present Simple: вопросы и отрицания
""",
                SystemPrompt = "You are an English tutor helping the student practice Present Simple: вопросы и отрицания. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000007"),
                Title = "Артикли: a/an/the",
                Description = "Master the basics of English articles.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 7,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Артикли: a/an/the

### Rule
This is a basic rule for Артикли: a/an/the.

### Examples
- Example 1 for Артикли: a/an/the
- Example 2 for Артикли: a/an/the
""",
                SystemPrompt = "You are an English tutor helping the student practice Артикли: a/an/the. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000008"),
                Title = "There is / There are",
                Description = "Describe what exists around you.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 8,
                TargetSkills = "R,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## There is / There are

### Rule
This is a basic rule for There is / There are.

### Examples
- Example 1 for There is / There are
- Example 2 for There is / There are
""",
                SystemPrompt = "You are an English tutor helping the student practice There is / There are. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000009"),
                Title = "Глагол have/have got",
                Description = "Talk about what you possess.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 9,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Глагол have/have got

### Rule
This is a basic rule for Глагол have/have got.

### Examples
- Example 1 for Глагол have/have got
- Example 2 for Глагол have/have got
""",
                SystemPrompt = "You are an English tutor helping the student practice Глагол have/have got. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000010"),
                Title = "Предлоги места: in/on/at/next to",
                Description = "Describe where things are located.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 10,
                TargetSkills = "R,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Предлоги места: in/on/at/next to

### Rule
This is a basic rule for Предлоги места: in/on/at/next to.

### Examples
- Example 1 for Предлоги места: in/on/at/next to
- Example 2 for Предлоги места: in/on/at/next to
""",
                SystemPrompt = "You are an English tutor helping the student practice Предлоги места: in/on/at/next to. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000011"),
                Title = "Прилагательные и порядок слов",
                Description = "Learn how to describe nouns with adjectives.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 11,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Прилагательные и порядок слов

### Rule
This is a basic rule for Прилагательные и порядок слов.

### Examples
- Example 1 for Прилагательные и порядок слов
- Example 2 for Прилагательные и порядок слов
""",
                SystemPrompt = "You are an English tutor helping the student practice Прилагательные и порядок слов. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000012"),
                Title = "Глагол can/can't: способность",
                Description = "Talk about what you can and cannot do.",
                Category = "Grammar & Structure",
                Difficulty = "Starter",
                ColorCssClass = "from-emerald-500/20 to-emerald-600/10",
                CefrLevel = "A1",
                OrderIndex = 12,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Глагол can/can't: способность

### Rule
This is a basic rule for Глагол can/can't: способность.

### Examples
- Example 1 for Глагол can/can't: способность
- Example 2 for Глагол can/can't: способность
""",
                SystemPrompt = "You are an English tutor helping the student practice Глагол can/can't: способность. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000013"),
                Title = "Present Continuous",
                Description = "Talk about actions happening right now.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 1,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Present Continuous

### Rule
This is a basic rule for Present Continuous.

### Examples
- Example 1 for Present Continuous
- Example 2 for Present Continuous
""",
                SystemPrompt = "You are an English tutor helping the student practice Present Continuous. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000014"),
                Title = "Past Simple: правильные глаголы",
                Description = "Learn to talk about the past using regular verbs.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 3,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Past Simple: правильные глаголы

### Rule
This is a basic rule for Past Simple: правильные глаголы.

### Examples
- Example 1 for Past Simple: правильные глаголы
- Example 2 for Past Simple: правильные глаголы
""",
                SystemPrompt = "You are an English tutor helping the student practice Past Simple: правильные глаголы. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000015"),
                Title = "Past Simple: неправильные глаголы",
                Description = "Master the common irregular verbs in the past.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 4,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Past Simple: неправильные глаголы

### Rule
This is a basic rule for Past Simple: неправильные глаголы.

### Examples
- Example 1 for Past Simple: неправильные глаголы
- Example 2 for Past Simple: неправильные глаголы
""",
                SystemPrompt = "You are an English tutor helping the student practice Past Simple: неправильные глаголы. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000016"),
                Title = "Past Simple: отрицание и вопрос",
                Description = "Ask questions and make negative sentences in the past.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 5,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Past Simple: отрицание и вопрос

### Rule
This is a basic rule for Past Simple: отрицание и вопрос.

### Examples
- Example 1 for Past Simple: отрицание и вопрос
- Example 2 for Past Simple: отрицание и вопрос
""",
                SystemPrompt = "You are an English tutor helping the student practice Past Simple: отрицание и вопрос. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000017"),
                Title = "Past Continuous",
                Description = "Describe background actions in the past.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 6,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Past Continuous

### Rule
This is a basic rule for Past Continuous.

### Examples
- Example 1 for Past Continuous
- Example 2 for Past Continuous
""",
                SystemPrompt = "You are an English tutor helping the student practice Past Continuous. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000018"),
                Title = "Будущее: going to",
                Description = "Talk about your plans and intentions.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 7,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Будущее: going to

### Rule
This is a basic rule for Будущее: going to.

### Examples
- Example 1 for Будущее: going to
- Example 2 for Будущее: going to
""",
                SystemPrompt = "You are an English tutor helping the student practice Будущее: going to. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000019"),
                Title = "Будущее: will (прогнозы, решения)",
                Description = "Make predictions and spontaneous decisions.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 8,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Будущее: will (прогнозы, решения)

### Rule
This is a basic rule for Будущее: will (прогнозы, решения).

### Examples
- Example 1 for Будущее: will (прогнозы, решения)
- Example 2 for Будущее: will (прогнозы, решения)
""",
                SystemPrompt = "You are an English tutor helping the student practice Будущее: will (прогнозы, решения). Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000020"),
                Title = "Сравнительные степени",
                Description = "Compare two things.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 9,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Сравнительные степени

### Rule
This is a basic rule for Сравнительные степени.

### Examples
- Example 1 for Сравнительные степени
- Example 2 for Сравнительные степени
""",
                SystemPrompt = "You are an English tutor helping the student practice Сравнительные степени. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000021"),
                Title = "Превосходная степень",
                Description = "Describe the highest degree of a quality.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 10,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Превосходная степень

### Rule
This is a basic rule for Превосходная степень.

### Examples
- Example 1 for Превосходная степень
- Example 2 for Превосходная степень
""",
                SystemPrompt = "You are an English tutor helping the student practice Превосходная степень. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000022"),
                Title = "Should/shouldn't: совет",
                Description = "Give and ask for advice.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 11,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Should/shouldn't: совет

### Rule
This is a basic rule for Should/shouldn't: совет.

### Examples
- Example 1 for Should/shouldn't: совет
- Example 2 for Should/shouldn't: совет
""",
                SystemPrompt = "You are an English tutor helping the student practice Should/shouldn't: совет. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000023"),
                Title = "Исчисляемые/неисчисляемые и much/many",
                Description = "Talk about quantities.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 12,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Исчисляемые/неисчисляемые и much/many

### Rule
This is a basic rule for Исчисляемые/неисчисляемые и much/many.

### Examples
- Example 1 for Исчисляемые/неисчисляемые и much/many
- Example 2 for Исчисляемые/неисчисляемые и much/many
""",
                SystemPrompt = "You are an English tutor helping the student practice Исчисляемые/неисчисляемые и much/many. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000024"),
                Title = "some/any/no + compounds",
                Description = "Use some, any, no and their compounds.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 13,
                TargetSkills = "R,W",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## some/any/no + compounds

### Rule
This is a basic rule for some/any/no + compounds.

### Examples
- Example 1 for some/any/no + compounds
- Example 2 for some/any/no + compounds
""",
                SystemPrompt = "You are an English tutor helping the student practice some/any/no + compounds. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000025"),
                Title = "Наречия частоты",
                Description = "Describe how often you do things.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 14,
                TargetSkills = "W,S",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Наречия частоты

### Rule
This is a basic rule for Наречия частоты.

### Examples
- Example 1 for Наречия частоты
- Example 2 for Наречия частоты
""",
                SystemPrompt = "You are an English tutor helping the student practice Наречия частоты. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
            new()
            {
                Id = Guid.Parse("22222222-0001-0000-0000-000000000026"),
                Title = "Вопросительные слова",
                Description = "Ask open-ended questions.",
                Category = "Grammar & Structure",
                Difficulty = "Elementary",
                ColorCssClass = "from-teal-500/20 to-teal-600/10",
                CefrLevel = "A2",
                OrderIndex = 15,
                TargetSkills = "S,R",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## Вопросительные слова

### Rule
This is a basic rule for Вопросительные слова.

### Examples
- Example 1 for Вопросительные слова
- Example 2 for Вопросительные слова
""",
                SystemPrompt = "You are an English tutor helping the student practice Вопросительные слова. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            },
        };
    }
}
