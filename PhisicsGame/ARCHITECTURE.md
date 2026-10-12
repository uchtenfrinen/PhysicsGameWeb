# Physics Adventure Architecture

## Why this approach

Project uses a **component-oriented OOP architecture**:
- Easier to scale for a large game than a monolithic `Game1`.
- Lower entry barrier than full ECS at prototype stage.
- Entity + component model can be migrated to true ECS later.

## Folder structure

- `Core/`
  - `Scenes/` scene interfaces and manager.
  - `Input/` keyboard state abstraction.
  - `Localization/` RU/EN dictionary-based localization service.
  - `Save/` save data + JSON persistence.
  - `Dialog/` simple dialogue queue.
  - `Tasks/` task definitions repository (JSON-driven).
- `Gameplay/`
  - `Scenes/` menu, world and task scenes.
  - `Entities/` entity object and components.
  - `World/` world object repository (JSON-driven).
  - `Camera2D.cs` world-to-screen transform.
  - `TileMap.cs` tile-based map data and rendering.
- `Tasks/`
  - `PhysicsTask.cs` extensible task contract + factory.
  - `NumericPhysicsTask.cs`, `ChoicePhysicsTask.cs`, `SimulationPhysicsTask.cs`.
- `Data/`
  - `tasks.json` tasks content (condition, validation, task type).
  - `world_objects.json` map-to-task binding.
- `Content/Fonts/DefaultFont.spritefont`
  - runtime UI text rendering in scene overlays.

## Scene flow

`MainMenuScene -> WorldScene -> TaskScene -> WorldScene`

- Menu: start and language toggle.
- World: movement, interaction with task terminal.
- Task: answer checking and callback to world progress.

## Extending task system

To add a new task:
1. Add item to `Data/tasks.json` with `Id`, `Type`, and validation fields.
2. Add localized title/description and choices in `LocalizationService`.
3. Add or map world terminal in `Data/world_objects.json` by `TaskId`.
4. If task type is new, create a `PhysicsTask` subclass and UI branch in `TaskScene`.

## Prototype controls

- `Enter` start game.
- `WASD` movement.
- `E` interact.
- `L` toggle RU/EN.
- `Esc` back.
- Task input:
  - `numeric`: digits + `Backspace` + `Enter`
  - `choice`: `Up/Down` + `Enter`
  - `simulation`: `Left/Right` + `Enter`
