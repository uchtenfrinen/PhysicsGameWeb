# Physics Tasks (два WPF + Core)

Два десктопных клиента и общая библиотека **`PhysicsTasks.Core`**: модели задач, валидация (`PhysicsTask.Validate`), прогресс, JSON-репозиторий, плюс мессенджер, чтобы хост мог открыть задачу по событию.

| Проект | Что за exe/dll | Зачем |
|--------|----------------|--------|
| **PhysicsTasks.Core** | `PhysicsTasks.Core.dll` | Общие типы, `SaveService`, `JsonTaskRepository`, пути `PhysicsTasksPaths`, `TaskCatalogPathResolver` для выбора `tasks.json`. |
| **PhysicsTasks.Terminal** | `PhysicsTasksTerminal.exe` | Ученик: список, ввод ответа, проверка, разметка через [MdXaml](https://github.com/whistyun/MdXaml). |
| **PhysicsTasks.Teacher** | `PhysicsTasksTeacher.exe` | Преподаватель: окно-заготовка и путь к **общему** `tasks.json`; сюда потом можно воткнуть нормальный редактор или БД. |

Папка в репо по истории называется `Tasks\`, в solution проект — **PhysicsTasks.Terminal**.

## Где лежит `tasks.json`

1. **Общий каталог (удобно, если оба WPF на одной машине)**  
   `%ProgramData%\PhisicsGame\catalog\tasks.json`  
   Панель препода пишет сюда; терминал **сначала** смотрит сюда, если файл есть.

2. **Рядом с exe**  
   Если общего нет — берётся `Data\tasks.json` из выходной папки терминала (копируется при сборке). Удобно для portable / dev.

3. **Свой путь**  
   Переменная **`PHYSICS_TASKS_JSON`** = полный путь к файлу. Если задана и файл существует — выигрывает у всего остального.

Логика в `TaskCatalogPathResolver.Resolve(AppContext.BaseDirectory)` в **PhysicsTasks.Core**.

## Прогресс ученика

Как в игре: **`%LocalAppData%\PhisicsGame\save.json`** (`PhysicsTasksPaths.GetDefaultSavePath()`).

## Как открыть задачу в терминале

- Запуск с аргументами `--task=…`, `/task:…`, `-task …` (детали в `StartupArgsParser`).
- Через `WeakReferenceMessenger` / `MessengerTaskSessionHost.OpenTask`.
- Встраиванием `TaskWorkspaceView` с `TaskWorkspaceViewModel`.

## База вместо файла

В Core уже есть идея `ITaskRepository`: можно подцепить PostgreSQL + Dapper и в терминал, и в панель препода. Тогда `ProgramData` можно не использовать или оставить как экспорт/кэш.

## Ограничение

В exe терминала нет редактора каталога — только решение. Правки задач — в **PhysicsTasks.Teacher** или через сервер, когда появится.
