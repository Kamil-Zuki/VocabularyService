using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public static class LessonSeeder
{
    public static async Task SeedAsync(VocabularyServiceContext db)
    {
        var existingLessonIds = await db.Lessons.Select(l => l.Id).ToListAsync();

        var lessons = new List<Lesson>
        {
            // ── Grammar & Structure ──────────────────────────────────────────────
            new()
            {
                Id = Guid.Parse("11111111-0001-0001-0001-000000000001"),
                Title = "Present Simple vs Present Continuous",
                Description = "Learn when to use 'I work' vs 'I am working' — one of the most common confusions in English.",
                Category = "Grammar & Structure",
                Difficulty = "Beginner",
                ColorCssClass = "from-blue-500/20 to-blue-600/10",
                CefrLevel = "A2",
                OrderIndex = 2,
                TargetSkills = "W,S",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Present Simple vs Present Continuous

**Present Simple** — постоянные факты, привычки, расписание.
**Present Continuous** — то, что происходит прямо сейчас или временно.

### Ключевые сигнальные слова

| Present Simple | Present Continuous |
|---|---|
| always, usually, often | now, at the moment |
| every day/week | currently, today |
| never, rarely | look! listen! |

### Примеры
- *She **works** at a bank.* (постоянная работа)  
- *She **is working** from home today.* (временно)

### Stative verbs — только Simple!
Глаголы состояния **не используются** в Continuous:  
know, believe, want, love, hate, understand, seem, belong, own
""",
                SystemPrompt = "You are a friendly English tutor. The student is studying Present Simple vs Present Continuous. Start by giving them 2-3 fill-in-the-blank exercises using their own vocabulary words (if available, otherwise use common verbs). After each answer, give clear feedback and explain any mistakes. Keep the tone encouraging. Ask them to write 2 original sentences about their daily life at the end."
            },
            new()
            {
                Id = Guid.Parse("11111111-0001-0001-0001-000000000002"),
                Title = "Past Simple vs Present Perfect",
                Description = "Master the difference between 'I went' and 'I have gone' — a key grammar point for intermediate learners.",
                Category = "Grammar & Structure",
                Difficulty = "Intermediate",
                ColorCssClass = "from-blue-500/20 to-blue-600/10",
                CefrLevel = "B1",
                OrderIndex = 3,
                TargetSkills = "W,S",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## Past Simple vs Present Perfect

### Past Simple — завершённые действия с указанием времени
Используй когда: **когда** это произошло — важно.

*I **visited** Paris in 2019.*  
*She **finished** the report yesterday.*

### Present Perfect — связь с настоящим
Используй когда: **факт** важен, время — нет.

*I **have visited** Paris.* (опыт)  
*She **has finished** the report.* (результат сейчас)

### Ключевые маркеры

| Past Simple | Present Perfect |
|---|---|
| yesterday, ago, in 2020 | ever, never, just |
| last week/month/year | already, yet, recently |
| when I was young | since, for (duration) |

### Формула Present Perfect
**have/has + V3 (past participle)**
""",
                SystemPrompt = "You are a friendly English tutor. The student is practicing Past Simple vs Present Perfect. Start with a short diagnostic — ask them to tell you about something they did last week AND something they have done in their life. Analyze their answer for tense errors. Then give 3 targeted exercises. Finish by asking them to write 3 sentences about their recent experiences."
            },
            new()
            {
                Id = Guid.Parse("11111111-0001-0001-0001-000000000003"),
                Title = "Conditionals: Zero, First & Second",
                Description = "If it rains, I will stay home. If I were rich, I would travel. Learn all three conditional types.",
                Category = "Grammar & Structure",
                Difficulty = "Intermediate",
                ColorCssClass = "from-blue-500/20 to-blue-600/10",
                CefrLevel = "B1",
                OrderIndex = 6,
                TargetSkills = "W,S",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## Conditionals

### Zero Conditional — факты и законы природы
**If + Present Simple, Present Simple**  
*If you heat water to 100°C, it boils.*

### First Conditional — реальное будущее
**If + Present Simple, will + infinitive**  
*If it rains, I will stay home.*  
*If I study hard, I'll pass the exam.*

### Second Conditional — нереальное/маловероятное
**If + Past Simple, would + infinitive**  
*If I were rich, I would travel the world.*  
*If I had more time, I would learn piano.*

> 💡 **Важно:** В Second Conditional говорим **"If I were"**, не "was" (даже для I/he/she).

### Сравнение
| Type | Вероятность | Пример |
|---|---|---|
| Zero | Факт | If you mix colours, you get new ones |
| First | Реально | If I finish early, I'll call you |
| Second | Нереально | If I were a bird, I would fly |
""",
                SystemPrompt = "You are a friendly English tutor teaching conditionals. Start with a warm-up: ask the student 'What would you do if you won the lottery?' and 'What will you do if the weather is nice this weekend?' Analyze their conditional usage carefully. Then give 5 exercises mixing all three types. Encourage the student to create their own sentences about their real life goals and dreams."
            },
            new()
            {
                Id = Guid.Parse("11111111-0001-0001-0001-000000000004"),
                Title = "Passive Voice Mastery",
                Description = "The book was written, the report has been submitted. Learn when and how to use the passive voice naturally.",
                Category = "Grammar & Structure",
                Difficulty = "Intermediate",
                ColorCssClass = "from-blue-500/20 to-blue-600/10",
                CefrLevel = "B1",
                OrderIndex = 9,
                TargetSkills = "R,W",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Passive Voice

### Когда использовать пассивный залог?
- Когда исполнитель **неизвестен**: *The window was broken.*
- Когда исполнитель **неважен**: *English is spoken worldwide.*
- В **формальном/научном** тексте: *The results were analysed.*

### Формула: **be (в нужном времени) + V3**

| Время | Активный | Пассивный |
|---|---|---|
| Present Simple | Someone cleans the office | The office **is cleaned** |
| Past Simple | They built the bridge | The bridge **was built** |
| Present Perfect | They have sent the email | The email **has been sent** |
| Future | They will announce results | Results **will be announced** |

### by + агент
*The novel **was written by** Tolstoy.*  
(агент упоминается, только если важен)

### Типичные ошибки
❌ *The letter was wrote...*  
✅ *The letter was written...*
""",
                SystemPrompt = "You are an English tutor teaching passive voice. Start by asking the student to describe their workplace or school — where things are made, used, kept. Help them naturally convert active sentences to passive. Give 5 transformation exercises (active → passive). Then discuss when passive sounds more natural in formal writing vs conversation."
            },
            new()
            {
                Id = Guid.Parse("11111111-0001-0001-0001-000000000005"),
                Title = "Modal Verbs: can, could, must, should, might",
                Description = "Express ability, obligation, advice and probability. Modal verbs are essential for natural English.",
                Category = "Grammar & Structure",
                Difficulty = "Beginner",
                ColorCssClass = "from-blue-500/20 to-blue-600/10",
                CefrLevel = "B1",
                OrderIndex = 8,
                TargetSkills = "W,S",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Modal Verbs

### Способность (Ability)
- **can** (настоящее): *I can swim.*
- **could** (прошлое): *I could swim when I was 5.*

### Разрешение (Permission)
- **can/could**: *Can I leave early? Could I use your phone?*
- **may** (формально): *May I help you?*

### Обязанность (Obligation)
- **must** (внутренняя): *I must call my mother — I promised.*
- **have to** (внешняя): *I have to wear a uniform at work.*
- **should / ought to** (совет): *You should see a doctor.*

### Вероятность (Probability)
- **must** (уверен): *He must be tired — he worked 12 hours.*
- **might/may** (возможно): *It might rain later.*
- **can't** (невозможно): *That can't be right.*

### Запрет vs Отсутствие обязанности
- **mustn't** = запрещено: *You mustn't smoke here.*
- **don't have to** = не нужно: *You don't have to come if you're busy.*
""",
                SystemPrompt = "You are a friendly English tutor teaching modal verbs. Start with a real-life scenario: the student has a job interview tomorrow — what should/must/could they do to prepare? Guide them to use different modals naturally. Then give 5 fill-in-the-blank exercises. Finish with a short role-play: you are a doctor, they must/should/can't do certain things for their health."
            },
            new()
            {
                Id = Guid.Parse("11111111-0001-0001-0001-000000000006"),
                Title = "Third Conditional & Mixed Conditionals",
                Description = "If I had studied harder, I would have passed. Advanced conditional forms for discussing regrets and hypotheticals.",
                Category = "Grammar & Structure",
                Difficulty = "Advanced",
                ColorCssClass = "from-blue-500/20 to-blue-600/10",
                CefrLevel = "B2",
                OrderIndex = 1,
                TargetSkills = "W,S",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## Third & Mixed Conditionals

### Third Conditional — прошлые нереальные ситуации
**If + Past Perfect, would have + V3**

Используется для **сожалений** и **нереализованных возможностей**.

*If I had studied harder, I would have passed the exam.*  
*If she had left earlier, she wouldn't have missed the train.*

### Mixed Conditionals

**Тип 1: Прошлая причина → настоящее следствие**  
If + Past Perfect, would + infinitive  
*If I had chosen a different career, I would be happier now.*

**Тип 2: Настоящая причина → прошлое следствие**  
If + Past Simple, would have + V3  
*If I weren't so shy, I would have talked to her at the party.*

### Инверсия в условных предложениях (formal)
*Had I known → If I had known*  
*Were I to → If I were to*
""",
                SystemPrompt = "You are an advanced English tutor. This is a challenging lesson on Third and Mixed Conditionals. Start by asking the student about a past decision they regret or a 'what if' moment in their life. Help them express it using Third Conditional. Then give 4 transformation exercises. Discuss the emotional nuance: Third Conditional often implies regret or criticism. Keep it intellectually engaging."
            },

            // ── Vocabulary Building ──────────────────────────────────────────────
            new()
            {
                Id = Guid.Parse("11111111-0002-0002-0002-000000000001"),
                Title = "Phrasal Verbs: Movement & Action",
                Description = "Pick up, put down, set off, give up — master the most common phrasal verbs for movement and action.",
                Category = "Vocabulary Building",
                Difficulty = "Intermediate",
                ColorCssClass = "from-purple-500/20 to-purple-600/10",
                CefrLevel = "B2",
                OrderIndex = 9,
                TargetSkills = "R,S",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Phrasal Verbs: Movement & Action

### Самые частые (Top 15)

| Phrasal Verb | Значение | Пример |
|---|---|---|
| **pick up** | поднять, забрать | *Pick up the phone. I'll pick you up at 6.* |
| **put down** | положить | *Put down your phone and listen.* |
| **set off / set out** | отправиться | *We set off early to avoid traffic.* |
| **give up** | бросить, сдаться | *Don't give up — you're almost there.* |
| **carry on** | продолжать | *Carry on — I'll join you later.* |
| **break down** | сломаться | *My car broke down on the highway.* |
| **turn up / show up** | появиться | *He turned up two hours late.* |
| **run out of** | закончиться | *We've run out of coffee.* |
| **look after** | присматривать | *Can you look after my cat?* |
| **come across** | наткнуться | *I came across this word by accident.* |

### Separable vs Inseparable
**Separable:** *Turn the TV off / Turn off the TV / Turn it off ✅*  
**Inseparable:** *Look after the children ✅ / Look the children after ❌*
""",
                SystemPrompt = "You are a vocabulary coach focusing on phrasal verbs. Start by asking the student about their week — and listen specifically for opportunities to teach phrasal verbs. Every time they use a simple verb that could be a phrasal verb, teach them the phrasal alternative. Give 5 gap-fill exercises using the 15 verbs from the lesson. Finish by asking them to tell a short story about a journey using at least 5 phrasal verbs."
            },
            new()
            {
                Id = Guid.Parse("11111111-0002-0002-0002-000000000002"),
                Title = "Collocations: Business English",
                Description = "Make a decision, hold a meeting, reach an agreement. Learn verb-noun collocations essential for professional communication.",
                Category = "Vocabulary Building",
                Difficulty = "Intermediate",
                ColorCssClass = "from-purple-500/20 to-purple-600/10",
                CefrLevel = "B2",
                OrderIndex = 11,
                TargetSkills = "R,W",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Business Collocations

### Ключевые глагольные связки

**make + noun**
- make a *decision / plan / profit / mistake / progress / suggestion*

**do + noun**
- do *business / research / work / damage / harm*

**hold + noun**
- hold a *meeting / conference / presentation / discussion*

**reach + noun**
- reach an *agreement / conclusion / target / deadline*

**take + noun**
- take *action / responsibility / initiative / notes / a break*

**set + noun**
- set *a goal / a deadline / a budget / priorities*

### Типичные ошибки
❌ *do a decision* → ✅ *make a decision*  
❌ *make a meeting* → ✅ *hold a meeting*  
❌ *take a presentation* → ✅ *give a presentation*
""",
                SystemPrompt = "You are a business English coach. Start by asking the student about their work or studies. Ask them to describe a recent project or meeting. Listen carefully for missing collocations and correct them naturally. Give 6 collocation exercises (choose the correct verb). Then do a short role-play: the student needs to present a project idea to you (their manager). Correct collocations in their responses and suggest more professional alternatives."
            },
            new()
            {
                Id = Guid.Parse("11111111-0002-0002-0002-000000000003"),
                Title = "Idioms: Emotions & Feelings",
                Description = "Feeling under the weather? On top of the world? Learn 15 essential idioms for expressing emotions in natural English.",
                Category = "Vocabulary Building",
                Difficulty = "Intermediate",
                ColorCssClass = "from-purple-500/20 to-purple-600/10",
                CefrLevel = "B2",
                OrderIndex = 12,
                TargetSkills = "L,S",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Idioms: Emotions & Feelings

| Idiom | Значение | Пример |
|---|---|---|
| **on top of the world** | счастлив, в восторге | *I got the job — I'm on top of the world!* |
| **under the weather** | плохо себя чувствует | *I'm feeling a bit under the weather today.* |
| **over the moon** | в восторге | *She was over the moon about the news.* |
| **down in the dumps** | в унынии | *He's been down in the dumps since losing his job.* |
| **on edge** | на нервах | *I've been on edge all day waiting for results.* |
| **in high spirits** | в отличном настроении | *The team was in high spirits after winning.* |
| **at a loss** | растерян | *I'm at a loss for words — that's incredible.* |
| **bite someone's head off** | накричать | *Don't bite my head off — I'm just asking!* |
| **keep one's chin up** | не унывать | *Keep your chin up — things will get better.* |
| **blow off steam** | выпустить пар | *I go for a run to blow off steam after work.* |

### Формальность
Большинство идиом — **разговорный стиль**. Избегай их в деловых письмах!
""",
                SystemPrompt = "You are a conversational English coach teaching emotion idioms. Start by asking how the student is feeling today and why. When they answer, suggest an idiom that fits their emotion. Then ask them about different emotional situations: a time they were excited, nervous, upset. Help them naturally use 3-4 idioms from the lesson in context. Give a fun matching exercise at the end."
            },
            new()
            {
                Id = Guid.Parse("11111111-0002-0002-0002-000000000004"),
                Title = "Advanced Vocabulary: Formal vs Informal",
                Description = "Begin vs start, purchase vs buy, assistance vs help. Learn when to use formal register in professional contexts.",
                Category = "Vocabulary Building",
                Difficulty = "Advanced",
                ColorCssClass = "from-purple-500/20 to-purple-600/10",
                CefrLevel = "B2",
                OrderIndex = 13,
                TargetSkills = "R,W",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## Formal vs Informal Register

### Ключевые замены

| Informal | Formal | Контекст |
|---|---|---|
| start | commence | business, legal |
| help | assist / facilitate | professional |
| buy | purchase | commercial |
| show | demonstrate | presentation |
| use | utilise / employ | academic |
| find out | ascertain / determine | research |
| try | attempt / endeavour | formal writing |
| need | require | contracts |
| think about | consider | decision-making |
| get better | improve / enhance | reports |

### Linking words: Informal → Formal
- but → **however / nevertheless**
- so → **therefore / consequently / thus**
- also → **furthermore / moreover / in addition**
- because → **due to / owing to / as a result of**

### Правило регистра
📧 Email коллеге = informal  
📄 Business report = formal  
📞 Phone call = informal/semi-formal  
📑 Cover letter = formal
""",
                SystemPrompt = "You are an advanced English tutor focusing on register. Give the student a short informal email and ask them to rewrite it in formal style. Correct and explain every informal word choice. Then reverse the exercise: give a formal text and ask them to make it casual. Discuss when native speakers choose each register. This is about developing a sophisticated 'ear' for English style."
            },

            // ── Real-Life Situations ────────────────────────────────────────────
            new()
            {
                Id = Guid.Parse("11111111-0003-0003-0003-000000000001"),
                Title = "Small Talk & Breaking the Ice",
                Description = "Learn how to start conversations, keep them going, and exit gracefully. Essential for social and professional networking.",
                Category = "Real-Life Situations",
                Difficulty = "Beginner",
                ColorCssClass = "from-green-500/20 to-green-600/10",
                CefrLevel = "A2",
                OrderIndex = 16,
                TargetSkills = "L,S",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Small Talk & Breaking the Ice

### Универсальные темы (safe topics)
- Weather (but don't stop there!)
- Weekend / plans
- Work / studies (light version)
- Sports / TV shows / travel

### Стартовые фразы

**При знакомстве:**
- *"I don't think we've met — I'm [name]."*
- *"How do you know [host's name]?"*
- *"What do you do for work?"*

**Развитие темы:**
- *"That's interesting — how did you get into that?"*
- *"Have you always been in [field]?"*
- *"What's the best part of your job?"*

**Переход к другой теме:**
- *"On a completely different note..."*
- *"Speaking of which..."*
- *"That reminds me..."*

**Завершение разговора:**
- *"It was great chatting with you!"*
- *"I should go and say hello to [name], but..."*
- *"I'll let you mingle — lovely to meet you."*

### Поддержание беседы — backchanneling
*"Really?", "Oh wow!", "No way!", "I know what you mean", "Absolutely!"*
""",
                SystemPrompt = "You are a friendly English conversation partner. Pretend you've just met the student at a networking event. Start with small talk — ask about their work, how they're enjoying the event, what they do in their free time. Be natural and responsive. If they give short answers, help them expand. Practice graceful conversation exits. Keep it fun and low-pressure — this should feel like a real conversation, not a test."
            },
            new()
            {
                Id = Guid.Parse("11111111-0003-0003-0003-000000000002"),
                Title = "Job Interviews in English",
                Description = "Tell me about yourself, what are your weaknesses? Practice the most common interview questions with professional answers.",
                Category = "Real-Life Situations",
                Difficulty = "Intermediate",
                ColorCssClass = "from-green-500/20 to-green-600/10",
                CefrLevel = "B2",
                OrderIndex = 20,
                TargetSkills = "S,W",
                EstimatedMinutes = 30,
                ContentMarkdown = """
## Job Interviews in English

### Структура ответа: STAR Method
**S**ituation → **T**ask → **A**ction → **R**esult

*"Tell me about a time you solved a difficult problem."*
- **S:** *"At my previous job, our main database crashed..."*
- **T:** *"I was responsible for restoring access within 4 hours..."*
- **A:** *"I coordinated with the IT team and identified the backup..."*
- **R:** *"We restored 95% of data within 3 hours, with no client impact."*

### Ключевые вопросы и формулы ответов

**"Tell me about yourself"**  
→ Present-Past-Future formula: текущая роль → опыт → цели

**"What are your weaknesses?"**  
→ Реальная слабость + как вы над ней работаете  
*"I tend to focus too much on details, but I've been using time-boxing to manage this."*

**"Why do you want this role?"**  
→ Конкретно о компании + как совпадает с вашими целями

**Вопросы для работодателя:**
- *"What does success look like in this role?"*
- *"What are the biggest challenges facing the team?"*
- *"What opportunities exist for professional development?"*
""",
                SystemPrompt = "You are a professional interviewer conducting a mock job interview in English. Ask the student what position they're applying for, then conduct a realistic interview with 5-6 key questions (Tell me about yourself, strengths/weaknesses, a challenge you overcame, why this company, where do you see yourself in 5 years). After each answer, give specific feedback: what was good, what could be improved, and suggest better phrasing. Keep it supportive but realistic."
            },
            new()
            {
                Id = Guid.Parse("11111111-0003-0003-0003-000000000003"),
                Title = "Emails & Professional Writing",
                Description = "Write clear, professional emails in English. Learn structure, tone, and the phrases native speakers actually use.",
                Category = "Real-Life Situations",
                Difficulty = "Intermediate",
                ColorCssClass = "from-green-500/20 to-green-600/10",
                CefrLevel = "B2",
                OrderIndex = 19,
                TargetSkills = "W",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## Professional Email Writing

### Структура письма

**Subject line:** Конкретно и ясно  
*"Meeting request — Q3 budget review, July 15"*

**Greeting:**
- Formal: *Dear Mr Smith / Dear Ms Johnson*
- Semi-formal: *Hi Tom / Hello Sarah*
- Unknown: *Dear Hiring Manager / To whom it may concern*

**Opening line:**
- *"I hope this email finds you well."*
- *"I'm writing regarding..."*
- *"Following up on our conversation..."*

**Body:** Один параграф = одна мысль  
Используй bullet points для перечислений.

**Call to action:**
- *"Please let me know if you have any questions."*
- *"I would appreciate your feedback by Friday."*
- *"Could you confirm receipt of this email?"*

**Closing:**
- Formal: *Yours sincerely / Yours faithfully*
- Semi-formal: *Best regards / Kind regards*
- Informal: *Best / Thanks*

### Золотые правила
✅ Короткие предложения  
✅ Активный залог где возможно  
✅ Перечитай перед отправкой  
❌ Избегай ALL CAPS  
❌ Не используй сокращения (can't → cannot) в формальных письмах
""",
                SystemPrompt = "You are a professional writing coach. Start by asking the student what kind of emails they write most often at work or university. Then give them a specific scenario to write an email: e.g., requesting a deadline extension, following up after a job interview, or declining a meeting politely. After they write it, provide detailed feedback on structure, tone, word choice, and grammar. Help them rewrite any weak sections."
            },
            new()
            {
                Id = Guid.Parse("11111111-0003-0003-0003-000000000004"),
                Title = "Disagreeing Politely & Expressing Opinions",
                Description = "I see your point, but... Learn how to disagree without conflict and express strong opinions diplomatically in English.",
                Category = "Real-Life Situations",
                Difficulty = "Intermediate",
                ColorCssClass = "from-green-500/20 to-green-600/10",
                CefrLevel = "B1",
                OrderIndex = 20,
                TargetSkills = "S,W",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Disagreeing Politely & Expressing Opinions

### Выражение мнения

**Уверенно:**
- *"In my view / In my opinion..."*
- *"I strongly believe that..."*
- *"It seems to me that..."*

**Осторожно:**
- *"I tend to think that..."*
- *"I might be wrong, but..."*
- *"From my perspective..."*

### Несогласие — от мягкого к твёрдому

**Мягкое:**
- *"I see your point, but..."*
- *"That's a fair point, though I'd argue..."*
- *"I'm not entirely sure I agree..."*

**Умеренное:**
- *"I'm afraid I don't quite agree..."*
- *"I understand where you're coming from, but..."*

**Твёрдое:**
- *"I completely disagree, actually..."*
- *"I have to respectfully challenge that..."*

### Соглашение с оговорками
- *"You're right to an extent, but..."*
- *"That's partly true, however..."*
- *"I agree with your point about X, but not about Y."*

### Запрос чужого мнения
- *"What do you make of...?"*
- *"How do you feel about...?"*
- *"Would you agree that...?"*
""",
                SystemPrompt = "You are a debate partner helping the student practice expressing and defending opinions in English. Choose a mildly controversial topic (remote work vs office, social media benefits vs harms, university vs self-education). State a strong opinion and invite them to respond. When they agree or disagree, challenge them to elaborate. Teach them the polite disagreement phrases from the lesson in context. Make it feel like a genuine intellectual discussion."
            },
            new()
            {
                Id = Guid.Parse("11111111-0003-0003-0003-000000000005"),
                Title = "Negotiations & Persuasion",
                Description = "Learn the language of negotiation: making offers, compromising, and persuading in a professional English context.",
                Category = "Real-Life Situations",
                Difficulty = "Advanced",
                ColorCssClass = "from-green-500/20 to-green-600/10",
                CefrLevel = "B2",
                OrderIndex = 21,
                TargetSkills = "S,W",
                EstimatedMinutes = 30,
                ContentMarkdown = """
## Negotiations & Persuasion

### Структура переговоров

1. **Opening position:** *"We're looking for a price around..."*
2. **Exploring:** *"What's your priority — timeline or budget?"*
3. **Proposing:** *"What if we were to...?"*
4. **Counter-offering:** *"That's not quite what we had in mind..."*
5. **Compromising:** *"We could meet you halfway on..."*
6. **Closing:** *"So, we're agreed on...?"*

### Ключевые фразы

**Сделать предложение:**
- *"We'd be prepared to offer..."*
- *"How would you feel about...?"*

**Запросить уступку:**
- *"Is there any room for negotiation on...?"*
- *"Could you be flexible on the timeline?"*

**Купить время:**
- *"I'll need to check with my team."*
- *"Can I get back to you on that?"*

**Убедить:**
- *"The key benefit here is..."*
- *"What this means for you is..."*
- *"The evidence suggests..."*

**Закрыть сделку:**
- *"I think we have a deal."*
- *"Let's move forward on that basis."*
""",
                SystemPrompt = "You are a business negotiation partner. Set up a realistic scenario: the student is negotiating either a salary increase with their manager, a contract price with a supplier, or a project deadline with a client. Play the other party and start in a firm position. Guide the student through making offers, handling objections, and reaching a compromise. Focus on the language of negotiation, not just the outcome. After 8-10 exchanges, give detailed feedback."
            },

            // ── Pronunciation & Listening ───────────────────────────────────────
            new()
            {
                Id = Guid.Parse("11111111-0004-0004-0004-000000000001"),
                Title = "Word Stress & Natural Rhythm",
                Description = "English rhythm is stress-timed. Learn which syllables to stress and why it makes you sound natural.",
                Category = "Pronunciation & Listening",
                Difficulty = "Intermediate",
                ColorCssClass = "from-orange-500/20 to-orange-600/10",
                CefrLevel = "B2",
                OrderIndex = 16,
                TargetSkills = "L,S",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Word Stress & Natural Rhythm

### Английский — stress-timed язык
Ударные слоги произносятся через **равные промежутки времени**.  
Безударные слоги «сжимаются».

*"I want to GO to the STORE"*  
vs.  
*"The MANAGER presented the QUARTERLY REPORT"*

### Ударение в многосложных словах

**Существительные часто:** ударение на 1-й слог  
*PRE-sent, RE-cord, OB-ject, PER-mit*

**Глаголы часто:** ударение на 2-й слог  
*pre-SENT, re-CORD, ob-JECT, per-MIT*

### Content vs Function Words
**Content words** (ударные): nouns, verbs, adjectives, adverbs  
*"She BOUGHT a NEW LAPTOP yesterday"*

**Function words** (безударные): articles, prepositions, conjunctions  
a/an/the, in/at/on/for, and/but/or → произносятся кратко

### Слабые формы (weak forms)
- *can* = /kən/ в потоке речи  
- *of* = /əv/  
- *to* = /tə/  
- *was* = /wəz/
""",
                SystemPrompt = "You are a pronunciation coach. This session is about word stress and rhythm. Ask the student to read a few sentences aloud (they can type them with emphasis marks: use CAPS for stressed syllables). Then analyze their stress patterns based on what they type. Give 5 pairs of noun/verb homographs (present/present, record/record) and ask them to put them in sentences showing the stress difference. Discuss the rhythmic pattern of English sentences."
            },
            new()
            {
                Id = Guid.Parse("11111111-0004-0004-0004-000000000002"),
                Title = "Listening Skills: Catching Fast Speech",
                Description = "Gonna, wanna, dunno, innit — English speakers contract and link words. Learn to understand fast, natural speech.",
                Category = "Pronunciation & Listening",
                Difficulty = "Intermediate",
                ColorCssClass = "from-orange-500/20 to-orange-600/10",
                CefrLevel = "B2",
                OrderIndex = 18,
                TargetSkills = "L",
                EstimatedMinutes = 20,
                ContentMarkdown = """
## Understanding Fast English Speech

### Contractions & Reductions в разговорной речи

| Written | Spoken | Значение |
|---|---|---|
| going to | **gonna** | собираюсь |
| want to | **wanna** | хочу |
| have to | **hafta** | должен |
| don't know | **dunno** | не знаю |
| kind of | **kinda** | как-то, типа |
| out of | **outta** | из |
| a lot of | **lotta** | много |
| let me | **lemme** | позволь мне |
| give me | **gimme** | дай мне |

### Linking sounds (связывание)
Слова связываются в потоке речи:

- *"pick it up"* → *"pickit-up"*
- *"take it easy"* → *"take-it-easy"*
- *"an apple"* → *"an-napple"*

### Elision — выпадение звуков
- *last night* → *lass-night*
- *next day* → *nex-day*
- *friendship* → *frenship*

### Стратегии понимания
1. Сосредоточься на **content words** — они ударные
2. Используй **контекст** — не каждое слово важно
3. Привыкай к **частотным фразам** целиком
""",
                SystemPrompt = "You are a listening skills coach. Explain to the student that you'll type sentences in 'fast speech' form and ask them to guess the 'written' form and meaning. Use reductions like 'gonna', 'wanna', 'dunno', 'kinda', 'hafta'. Start simple and increase difficulty. Also discuss strategies for when they watch English movies or YouTube — where to focus attention, how to use subtitles strategically, and how to train their ear over time."
            },

            // ── Writing Skills ──────────────────────────────────────────────────
            new()
            {
                Id = Guid.Parse("11111111-0005-0005-0005-000000000001"),
                Title = "Essay Structure & Academic Writing",
                Description = "Introduction, body, conclusion. Learn to structure arguments clearly and academically in English.",
                Category = "Writing Skills",
                Difficulty = "Advanced",
                ColorCssClass = "from-rose-500/20 to-rose-600/10",
                CefrLevel = "B2",
                OrderIndex = 19,
                TargetSkills = "W",
                EstimatedMinutes = 30,
                ContentMarkdown = """
## Essay Structure & Academic Writing

### Базовая структура

**Introduction (10-15% объёма)**
1. Hook: интересный факт/вопрос/цитата
2. Background: контекст
3. Thesis statement: чёткий аргумент

**Body Paragraphs (70-80% объёма)**  
Каждый параграф = **одна главная идея**  
Структура: Topic sentence → Evidence → Analysis → Transition

**Conclusion (10-15% объёма)**
1. Restate thesis (другими словами)
2. Summary of main points
3. Final thought / implication

### Академические связки

**Добавление:** Furthermore, Moreover, In addition, Additionally  
**Контраст:** However, Nevertheless, On the other hand, Conversely  
**Причина:** Because, Since, Due to, Owing to  
**Результат:** Therefore, Consequently, Thus, As a result  
**Пример:** For instance, For example, Such as, Specifically

### Что избегать
❌ "I think / I believe" → ✅ *"The evidence suggests..."*  
❌ Слишком короткие параграфы (1-2 предложения)  
❌ Разговорные сокращения (don't → do not)  
❌ Начинать предложения с And/But → And/But  
✅ Also/However
""",
                SystemPrompt = "You are an academic writing tutor. Ask the student to pick a topic they know well or care about. Help them brainstorm and outline a 5-paragraph essay together: we'll write the thesis statement, then topic sentences for each body paragraph. After the outline is solid, ask them to write the introduction paragraph. Give detailed line-by-line feedback on academic tone, structure, and grammar. Be a rigorous but encouraging editor."
            },
            new()
            {
                Id = Guid.Parse("11111111-0005-0005-0005-000000000002"),
                Title = "Storytelling & Narrative Writing",
                Description = "Hook your reader, build tension, create vivid scenes. Learn narrative techniques that make English writing come alive.",
                Category = "Writing Skills",
                Difficulty = "Intermediate",
                ColorCssClass = "from-rose-500/20 to-rose-600/10",
                CefrLevel = "B1",
                OrderIndex = 17,
                TargetSkills = "W,R",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## Storytelling & Narrative Writing

### Структура истории (Story Arc)

1. **Hook** — первая строка, которая захватывает
2. **Setup** — кто, где, когда
3. **Conflict / Rising action** — проблема нарастает
4. **Climax** — момент кульминации
5. **Resolution** — развязка + вывод

### Show, Don't Tell
❌ *"She was scared."*  
✅ *"Her hands trembled as she reached for the door handle."*

❌ *"The city was busy."*  
✅ *"Car horns blared, vendors shouted prices, and somewhere above, pigeons scattered from a ledge."*

### Vivid language tools

**Sensory details:** звук, запах, текстура  
**Strong verbs:** не *went* — а *strode, crept, dashed, stumbled*  
**Simile:** *"like a freight train"*  
**Metaphor:** *"Time was a river pulling him forward"*

### Временные маркеры для нарратива
*Meanwhile, Shortly afterwards, All of a sudden,  
Before long, At that very moment, Eventually*
""",
                SystemPrompt = "You are a creative writing coach. Ask the student to tell you about a memorable experience — could be funny, scary, exciting, or surprising. Based on what they share, work together to transform it into a proper narrative: identify the story arc, suggest a hook, teach them 'show don't tell' by rewriting any 'telling' sentences they use. By the end, they should have a polished 150-200 word story. Celebrate every good descriptive sentence they write!"
            },

            // ── Exam Preparation ────────────────────────────────────────────────
            new()
            {
                Id = Guid.Parse("11111111-0006-0006-0006-000000000001"),
                Title = "IELTS Speaking: Part 2 (Long Turn)",
                Description = "You have 1 minute to prepare and 2 minutes to speak. Master the IELTS Speaking Part 2 cue card technique.",
                Category = "Exam Preparation",
                Difficulty = "Advanced",
                ColorCssClass = "from-yellow-500/20 to-yellow-600/10",
                CefrLevel = "B2",
                OrderIndex = 22,
                TargetSkills = "S",
                EstimatedMinutes = 30,
                ContentMarkdown = """
## IELTS Speaking Part 2 — Long Turn

### Формат
- Получаете **cue card** с темой и 3-4 bullet points
- **1 минута** на подготовку (делайте заметки!)
- **1-2 минуты** речи — говорить должны **ВЫ**
- Экзаменатор может задать 1-2 уточняющих вопроса

### Структура ответа (2 минуты = ~250-300 слов)

**Intro (15 сек):** *"I'd like to talk about..."*  
**Main point 1 (30 сек):** Who / What  
**Main point 2 (30 сек):** When / Where  
**Main point 3 (30 сек):** How / Why this is memorable  
**Conclusion (15 сек):** *"Overall, this was... because..."*

### Пример cue card

*"Describe a time you learned something new.*  
*You should say:*  
*- what you learned*  
*- how you learned it*  
*- why you decided to learn it*  
*- and explain how you felt about it."*

### Оценивается
- **Fluency & Coherence** — плавность, связность
- **Lexical Resource** — богатство лексики
- **Grammar Range & Accuracy** — разнообразие грамматики
- **Pronunciation** — произношение

### Полезные фразы для перехода
*"Moving on to...", "What's particularly interesting is...",  
"I should also mention that...", "To sum up..."*
""",
                SystemPrompt = "You are an IELTS examiner conducting a mock Speaking Part 2 test. Give the student a cue card topic (choose from: a memorable journey, a person who influenced you, a skill you'd like to learn, a time you helped someone). Tell them they have 1 minute to prepare (they can use that time to type notes). Then ask them to speak for 2 minutes. After they respond, score each criterion (Fluency, Lexical Resource, Grammar, Pronunciation) out of 9 and give specific improvement advice. Then offer to try another topic."
            },
            new()
            {
                Id = Guid.Parse("11111111-0006-0006-0006-000000000002"),
                Title = "TOEFL Reading: Inference & Vocabulary Questions",
                Description = "Master the hardest TOEFL reading question types: inference, rhetorical purpose, and vocabulary in context.",
                Category = "Exam Preparation",
                Difficulty = "Advanced",
                ColorCssClass = "from-yellow-500/20 to-yellow-600/10",
                CefrLevel = "B2",
                OrderIndex = 21,
                TargetSkills = "R",
                EstimatedMinutes = 25,
                ContentMarkdown = """
## TOEFL Reading — Inference & Vocabulary Questions

### Типы сложных вопросов

**Inference Questions**  
*"It can be inferred from paragraph 2 that..."*  
→ Ответ **не сказан прямо** — нужно логически вывести  
→ Правильный ответ всегда **следует из текста**, не из знаний

**Rhetorical Purpose Questions**  
*"Why does the author mention...?"*  
→ Ищи **структурную роль**: пример? контраргумент? доказательство?  
→ Ответ часто: *"to illustrate", "to contrast", "to support the claim"*

**Vocabulary in Context**  
*"The word X in paragraph 3 is closest in meaning to..."*  
→ Замени слово в предложении — смысл должен сохраниться  
→ НЕ используй общее значение — только **это** значение в **этом** контексте

### Стратегия для TOEFL Reading
1. **Skim** passage за 2-3 минуты (structure only)
2. **Read question** → locate relevant paragraph
3. **Re-read** carefully, then answer
4. **Eliminate wrong** answers — 3 distractor patterns:
   - Too extreme (always, never, all)
   - True but irrelevant (from text, but doesn't answer Q)
   - Outside the text (may be true, not stated)

### Время
65-54 минуты → 3-4 текста → 10 вопросов каждый  
≈ **1.5 мин/вопрос**
""",
                SystemPrompt = "You are a TOEFL preparation tutor. Create a short academic paragraph (5-7 sentences) on a topic like climate change, urban development, or ancient civilizations. Then ask 4 questions: 1 factual, 1 vocabulary-in-context, 1 inference, 1 rhetorical purpose. After the student answers, explain why each answer is right or wrong using the text as evidence. Then give a second passage on a different topic. Focus on teaching the *strategy* for answering each question type, not just whether the answers are correct."
            },
        };

        lessons.AddRange(A1A2LessonsSeeder.GetLessons());

        var newLessons = lessons.Where(l => !existingLessonIds.Contains(l.Id)).ToList();
        if (!newLessons.Any()) return;

        // Set timestamps
        foreach (var lesson in newLessons)
        {
            lesson.CreatedAt = DateTime.UtcNow;
            lesson.UpdatedAt = DateTime.UtcNow;
        }

        await db.Lessons.AddRangeAsync(newLessons);
        await db.SaveChangesAsync();
    }
}
