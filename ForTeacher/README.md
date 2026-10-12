# ForTeacher — модуль преподавателя (WPF)

Отдельное приложение и встраиваемый `UserControl`: заводишь задачи (заголовок, текст, числовой ответ, картинка по желанию), привязываешь к WPF-лабу через `LabWpfSetupId`, список лаб берётся из `TaskWpfSetupCatalog.GetTeacherLabSetupsForTopic`. Игровой мир, MonoGame и сами экраны лабораторий сюда не входят — только редактор задач для преподавателя.

## Где лежит `tasks.json`

По умолчанию:

`<каталог_приложения>/Data/tasks.json`

После сборки это часто что-то вроде:

`ForTeacher/bin/Debug/net9.0-windows/Data/tasks.json`

В коде: `AppPaths.DefaultTasksJsonPath` (`Services/AppPaths.cs`). Для встраивания — `TeacherTasksUserControl.TasksJsonPath` или `TeacherModuleFactory.CreateDefault(tasksJsonPath)`.

Пример файла:

```json
{
  "version": 1,
  "tasks": [
    {
      "id": "mechanics_teacher_20260416120000000",
      "topic": "mechanics",
      "title": "Пример",
      "text": "Опишите результат измерения.",
      "expectedNumericAnswer": 12.5,
      "imageRelativePath": "TeacherImages/mechanics_teacher_20260416120000000_img.png",
      "labWpfSetupId": 101,
      "isTeacherTask": true
    }
  ]
}
```

`TaskRepository` при сохранении **переписывает весь** `tasks.json`, но старается не потерять записи с `isTeacherTask: false` (например, задачи ученика из основного приложения).

## Поля `TaskDefinition`

| Поле | Зачем |
|------|--------|
| `id` | Уникальный id; для препода обычно `{topic}_teacher_{UTC yyyyMMddHHmmssfff}`. |
| `topic` | Раздел в UI (`mechanics`, `optics` …). |
| `title` | Заголовок, до 200 символов. |
| `text` | Условие. |
| `expectedNumericAnswer` | Ожидаемое число. |
| `imageRelativePath` | Необязательно: от `Data/` до файла в `TeacherImages/`. |
| `labWpfSetupId` | Какой WPF-лаб привязан. |
| `isTeacherTask` | `true` для задач из этого модуля; удалять из UI можно только их. |
| `difficulty` | 1–5 или `0`/нет — «не задано». |
| `aiReasoning` | Короткий текст от модели (подсказка к «звёздам»). |
| `KeyLawsJson` | JSON-массив строк с законами. |
| `estimatedTimeMin` | Оценка времени на задачу, минуты. |

Картинки при сохранении копируются в `Data/TeacherImages/`.

## Репозиторий

- `TaskRepository.Add` — только задачи с `IsTeacherTask == true`.
- `TaskRepository.TryRemoveTeacherTaskById` — удаление по id, если задача преподавательская.

После записи файла — событие `TaskRepository.TasksFileChanged`; у контрола то же через `TasksFileChanged`.

```csharp
var editor = new TeacherTasksUserControl();
editor.TasksJsonPath = @"D:\SharedCourse\Data\tasks.json";
editor.TasksFileChanged += (_, _) => { /* перечитать задачи в хосте */ };
```

## Логин в standalone

Стартовое окно спрашивает пароль. По умолчанию `1234` (`TeacherModuleFactory.CreateDefault`, `loginPassword`). Ничего между сеансами не запоминается.

## PostgreSQL (опционально)

Переменная `FOR_TEACHER_PG_CONNECTION` — строка Npgsql. Тогда вместо заглушки используется `PostgreSqlDatabaseService`; после add/remove для преподавательских задач вызывается `UpsertTeacherTasksAsync`.

### Ключ для «Анализировать» (OpenAI-совместимый API)

Порядок поиска ключа такой же по смыслу, как Groq в `WPFPhisics1.1`, плюс явный ключ для ForTeacher:

1. `FOR_TEACHER_OPENAI_API_KEY`
2. `TeacherAiApiKeyConfig.Key` (если зашит в сборке — в git не класть)
3. `GROQ_API_KEY`
4. Первая непустая строка из `groq_api_key.txt` (каталог exe, родитель, cwd, три уровня вверх от exe, потом предки каталога exe и cwd — в каждом ищутся `groq_api_key.txt` и типичные пути WPF, чтобы ключ из соседнего проекта подхватился)

Ключ проверяется **при каждом нажатии «Анализировать»**, перезапуск не обязателен. Если не находится — в UI показывается понятная ошибка (HTTP-код, битый JSON и т.д.).

Если ForTeacher запускается **из MonoGame (PhisicsGame)** и ключ лежит только рядом с игрой, процесс игры перед стартом модуля может выставить дочернему процессу **`GROQ_API_KEY`** из `groq_api_key.txt` относительно `PhisicsGame.exe` (тот же обход путей, что в `TeacherAiCredentials`).

- `FOR_TEACHER_OPENAI_BASE_URL` — если задан, используется как есть (без хвостового `/`). Иначе: ключ `gsk_…` → `https://api.groq.com/openai/v1`, иначе OpenAI.
- `FOR_TEACHER_OPENAI_MODEL` — если задан, им модель для всех запросов. Иначе Groq без картинки — `llama-3.1-8b-instant`; Groq с картинкой — `FOR_TEACHER_OPENAI_VISION_MODEL` или по умолчанию `meta-llama/llama-4-scout-17b-16e-instruct`; не-Groq — `gpt-4o-mini`.

Минимальная таблица:

```sql
CREATE TABLE IF NOT EXISTS teacher_tasks (
    id text PRIMARY KEY,
    topic text NOT NULL,
    title text NOT NULL,
    text text NOT NULL,
    expected_numeric_answer double precision,
    image_relative_path text,
    lab_wpf_setup_id text NOT NULL,
    is_teacher_task boolean NOT NULL,
    difficulty integer,
    ai_reasoning text,
    key_laws text,
    estimated_time_min integer
);
```

Если таблица старая, без полей ИИ:

```sql
ALTER TABLE teacher_tasks ADD COLUMN IF NOT EXISTS difficulty integer;
ALTER TABLE teacher_tasks ADD COLUMN IF NOT EXISTS ai_reasoning text;
ALTER TABLE teacher_tasks ADD COLUMN IF NOT EXISTS key_laws text;
ALTER TABLE teacher_tasks ADD COLUMN IF NOT EXISTS estimated_time_min integer;
```

UPSERT гоняется по текущим преподавательским задачам; **удаление только из JSON автоматически не чистит строки в БД** — если нужно, делай свою синхронизацию.

## Каталог лаб

`TaskWpfSetupCatalog` — заглушка. В своём продукте подставь свою реализацию `ITaskWpfSetupCatalog`, контракт `GetTeacherLabSetupsForTopic(string topic)` сохрани.

## Сборка

```powershell
dotnet build .\ForTeacher.sln -c Release
dotnet run --project .\ForTeacher\ForTeacher.csproj
```

Встраивание: ссылка на проект или DLL, в разметке `<views:TeacherTasksUserControl/>`, xmlns `clr-namespace:ForTeacher.Views;assembly=ForTeacher` для DLL.
