# WPFPhisics1.1 — полное описание для переноса на Stride

Документ описывает **что именно делает** исходный WPF-проект: навигацию, общие паттерны и **каждую установку** отдельно (физика, параметры, UI, анимация, графики, команды). Агент должен воспроизвести поведение и смысл в Stride (C#, сцены, UI, таймеры/Update).

---

## Часть A. Общая архитектура приложения

### A.1 Назначение

Интерактивный **физический конструктор**: выбор раздела → список установок → экран установки с ползунками, визуализацией, графиками и текстовым объяснением. Опционально — **чат с Groq API** для расширенного объяснения по текущей теме.

### A.2 Технологии в WPF

- **.NET**, **WPF** (XAML), **MVVM** (`INotifyPropertyChanged`, `ICommand`).
- **Таймеры**: `DispatcherTimer` для анимации (шаг 20–40 мс).
- **Графики**: **LiveChartsCore + SkiaSharp** (линии, scatter).
- **Схемы установок**: кастомные `UserControl` с переопределением `OnRender` / `DrawingContext` (векторная отрисовка).
- **Физика**: отдельные **статические классы** в папке `Physics/` — без зависимости от WPF (удобно портировать в Stride как обычные `.cs`).

### A.3 Навигация

1. **`MainWindow`**: единственный `ContentControl` (`MainContent`), в него подставляются страницы.
2. **`MainMenuPage`**: список разделов из **`PhysicsCatalog.Sections`** (`Navigation/PhysicsSection.cs`).
3. **`SetupListPage`**: список установок выбранного раздела; кнопка «Назад» → главное меню.
4. При выборе установки создаётся соответствующий **`UserControl`** (см. таблицу id ниже); кнопка «← К списку установок» → возврат к `SetupListPage` с тем же разделом.

**Таблица соответствия `setup.Id` → класс View (WPF):**

| Id | View (UserControl) | ViewModel |
|----|---------------------|-----------|
| `inclined_plane` | `InclinedPlaneView` | `MainViewModel` |
| `free_fall` | `FreeFallView` | `FreeFallViewModel` |
| `projectile` | `ProjectileMotionView` | `ProjectileMotionViewModel` |
| `spring` | `SpringOscillatorView` | `SpringOscillatorViewModel` |
| `buoyancy` | `BuoyancyView` | `BuoyancyViewModel` |
| `communicating_vessels` | `CommunicatingVesselsView` | `CommunicatingVesselsViewModel` |
| `ideal_gas` | `IdealGasView` | `IdealGasViewModel` |
| `ohm_law` | `OhmLawView` | `OhmLawViewModel` |
| `resistors_connection` | `ResistorsConnectionView` | `ResistorsConnectionViewModel` |
| `charge_in_field` | `ChargeInFieldView` | `ChargeInFieldViewModel` |
| `thin_lens` | `ThinLensView` | `ThinLensViewModel` |
| `refraction_at_boundary` | `RefractionAtBoundaryView` | `RefractionAtBoundaryViewModel` |
| `harmonic_oscillations` | `HarmonicOscillationsView` | `HarmonicOscillationsViewModel` |

**Каталог разделов и имён установок** — в `Navigation/PhysicsSection.cs` (`PhysicsCatalog`).

### A.4 Общие элементы UI на экранах установок

Почти везде повторяются:

- Кнопка **«← К списку установок»** (RoutedEvent `BackRequested`).
- Заголовок + краткая теоретическая строка.
- **Ползунки** (`Slider`) для параметров с `Clamp` в сеттерах ViewModel.
- Кнопки: **Сброс** (где есть анимация), **Объяснить** (локальный текст), **Нейросеть-помощник** / Groq (HTTP).
- Область **текста объяснения** (`ExplanationText`).
- Тёмная палитра: фон ~`#1E1E1E`, панели ~`#252526`, акцент ~`#0E639C`.

### A.5 Сервисы

- **`GroqService`**: POST к API Groq, ключ из `GROQ_API_KEY`, `groq_api_key.txt` или встроенной конфигурации. Формирует промпты по-русски с численными значениями.
- **`PhysicsChatContext.CurrentSetupName`**: при открытии установки записывается **человекочитаемое имя** установки (для контекста чата).
- **`ChatWindow`**: отдельное окно чата (кнопка в `MainWindow`).

### A.6 Что переносить в Stride первым

1. Папку **`Physics/`** целиком (логика без UI).
2. Структуру **`PhysicsCatalog`** (разделы + id).
3. Для каждой установки — **эквивалент ViewModel** (состояние + команды) и **подсистему отрисовки** (Stride UI или 2D-слой + примитивы).

В Stride нет WPF-биндингов: использовать **свойства с уведомлениями** (события / INotifyPropertyChanged в общей библиотеке) и привязку к UI через код или Stride UI.

---

## Часть B. Описание каждой установки

Ниже для **каждого `setup.Id`**: исходные файлы, физика, параметры, что считает ViewModel, что рисуется, анимация, графики.

---

### B.1 `inclined_plane` — Груз на наклонной плоскости

**Файлы:** `Pages/InclinedPlaneView.xaml(.cs)`, `MainViewModel.cs`, `Physics/InclinedPlane.cs`, `Block.cs`, `PhysicsEngine.cs`, `ForceCalculator.cs`, `Physics/PhysicsExplanationBuilder.cs`, `InclinedPlaneView` связан с перетаскиванием блока и графиками.

**Физика:**

- Плоскость: угол **α** в градусах, `10…60` (`InclinedPlane.AngleDegrees`).
- Блок: масса **m** кг, позиция вдоль плоскости **t** ∈ `[0.14, 0.86]` (нормализованная координата «вдоль плоскости»).
- Трение: **μ** ∈ `[0, 1]`.
- `ForceCalculator`: вес `mg`, нормаль `N = mg·cos α`, вдоль плоскости вниз `m·g·sin α`, трение `μN`. Покой, если `μN ≥ m·g·sin α`; иначе ускорение `a = (m·g·sin α − μN)/m = g·(sin α − μ·cos α)`.
- `PhysicsEngine.Step(dt)`: обновляет скорость и **уменьшает** `PositionAlongPlane` (движение вниз к `MinT = 0.14`). Остановка при достижении низа.

**ViewModel (`MainViewModel`):**

- Параметры: `MassKg`, `AngleDegrees`, `FrictionMu`.
- Выходы: `FWeight`, `NormalN`, `FAlong`, `FTr`, `Acceleration`, `StateText` («покой» / «скольжение»), `RestConditionText` (сравнение μ и tg α), `TimeFor1MSeconds`, `LastRunSummaryText`.
- **Симуляция**: таймер `SimDt ≈ 0.04 с`; при запуске очищается график, пишутся точки `(время, путь, v, a)` в `ChartData` → синхронизация в `PathPoints`, `VelocityPoints`, `AccelerationPoints` для LiveCharts.
- **Перетаскивание блока** мышью по плоскости (в View): меняется `Block.PositionAlongPlane`, вызывается `ClampBlockPositionForDrag`, `RefreshForces`.

**Визуализация:**

- **Мини-схема** (LiveCharts): дуга угла α, отрезок плоскости, точка груза, стрелки сил F, N, вдоль плоскости, трение — см. `UpdateInclinedPlaneChart()` в `MainViewModel` (масштаб сил `ForceChartScale`).
- Отдельные контролы: схема сил, возможно `MiniSchemeControl` / `ChartControl` для больших графиков s(t), v(t), a(t).

**Команды:** старт/стоп симуляции, сброс, объяснить, Groq (специальный метод `GroqService.GetInclinedPlaneExplanationAsync`).

---

### B.2 `free_fall` — Свободное падение

**Файлы:** `FreeFallViewModel.cs`, `Physics/FreeFall.cs`, `FreeFallCanvas.cs`, `FreeFallChartControl.cs`.

**Физика (`FreeFall`):** `g = 9.81`. Начальная высота **h₀**. Без v₀: `h(t) = h₀ − g·t²/2`, скорость вниз `v(t) = g·t`, `T = √(2h₀/g)`, `v_кон = √(2gh₀)`.

**Параметры:** `Height0` (ограничение в VM: `0.1…1000`, в слайдере XAML часто `0.5…150`).

**ViewModel:**

- `TimeOfFall`, `FinalSpeed`, `CurrentTime`, `CurrentHeight` = `FreeFall.HeightAt(CurrentTime, Height0)`.
- **Графики:** точки `h(t)` и `v(t)` до T (`GetHeightPoints` / `GetVelocityPoints`, ~60 точек).
- **Анимация:** таймер ~30 мс, шаг `AnimDt = 0.03 с`; при достижении T или h≤0 — стоп.

**Визуал:** холст падения тела + два графика (высота и скорость от времени).

**Команды:** Build, Play/Stop, Reset, Explain, Groq (промпт с h₀, T, v).

---

### B.3 `projectile` — Тело, брошенное под углом

**Файлы:** `ProjectileMotionViewModel.cs`, `Physics/ProjectileMotion.cs`, `ProjectileMotionView`, `TrajectoryCanvas`, `ProjectileMotionChartControl`, `ProjectileForcesSchemeControl`.

**Физика:** без сопротивления воздуха, `g = 9.81`.  
`x = v₀·cos(α)·t`, `y = y₀ + v₀·sin(α)·t − g·t²/2`.  
Время полёта, дальность, максимальная высота — в `ProjectileMotion`.

**Параметры:** `V0`, `AngleDegrees` (1…89), `Y0` ≥ 0.

**ViewModel:** `Range`, `MaxHeight`, `TimeOfFlight`, `TrajectoryPoints`, анимация по `PositionAt`, текущие `CurrentX`, `CurrentY`.

**Визуал:** траектория, схема сил/скоростей, графики (если есть в XAML).

---

### B.4 `spring` — Пружина с грузом (гармонический осциллятор по k, m)

**Файлы:** `SpringOscillatorViewModel.cs`, `Physics/SpringOscillator.cs`, `SpringOscillatorView`, `SpringCanvas`, `SpringChartControl`, `SpringForcesSchemeControl`.

**Физика:** без трения; `ω = √(k/m)`, `T = 2π/ω`, `x(t) = x₀·cos(ωt) + (v₀/ω)·sin(ωt)`, `v(t)`, `a = −(k/m)·x`.

**Параметры:** `K` (жёсткость), `M` (масса), `X0` (начальное смещение), `V0` (начальная скорость).  
**Важно:** в UI есть слайдер **`Damping`** (0…1) и свойство **`Zeta`** — они **участвуют в объяснении/отображении**, но **анимация и графики** в коде используют **незатухающую** модель `SpringOscillator` (затухание в дифференциальном виде в `Physics/SpringOscillator.cs` **не реализовано**).

**ViewModel:** три набора точек: x(t), v(t), a(t) за **2 периода**; анимация обновляет только `CurrentX` через `PositionAt`.

---

### B.5 `buoyancy` — Плавание тела (Архимед)

**Файлы:** `BuoyancyViewModel.cs`, `Physics/Buoyancy.cs`, `BuoyancyView`, `BuoyancyCanvas`, `BuoyancyChartControl`, `BuoyancyDensityChartControl`, `BuoyancyForcesSchemeControl`.

**Физика:** `F_A = ρ_ж·g·V_погр`, `P = ρ_т·g·V`. Равновесие плавания: доля погружённого объёма `V_погр/V = min(1, ρ_т/ρ_ж)`; тонет если ρ_т > ρ_ж; ρ_т = ρ_ж — нейтральное.

**Параметры:** `RhoBody`, `RhoFluid`, `Volume` (м³).

**ViewModel:** `ArchimedesForce`, `Weight`, `SubmergedFraction`, `ConditionText`, `StatusInequality`.  
**Анимация:** `CurrentSubmergedFraction` **плавно стремится** к `SubmergedFraction` (шаг `AnimStep = 0.03`). Пользователь может **перетащить куб** в жидкости → `SetCurrentSubmergedFromCanvas` → таймер снова тянет к равновесию.

**Визуал:** сосуд, куб, уровень жидкости, схема сил, графики (в т.ч. плотность).

---

### B.6 `communicating_vessels` — Сообщающиеся сосуды

**Файлы:** `CommunicatingVesselsViewModel.cs`, `Physics/CommunicatingVessels.cs`, `CommunicatingVesselsView`, `CommunicatingVesselsCanvas`, `CommunicatingVesselsChartControl`.

**Два режима** (`IsTwoLiquidsMode`):

1. **Одна жидкость:** площади сечений `Area1`, `Area2`, объём `Volume`, плотность `Density`. Уровень `h = V/(S₁+S₂)`, давление на дно `p = ρgh`.
2. **Две жидкости:** `RhoLight`, `RhoHeavy`, заданная высота тяжёлой `HeightHeavy` → высота лёгкой `h_light = h_heavy · (ρ_heavy/ρ_light)`; давление на границе `ρ_heavy·g·h_heavy`.

**График LiveCharts:** кривая «h_масла(ρ)» (зависимость для демонстрации) + маркер текущего состояния — см. `Build()` в ViewModel.

**Визуал:** два колена, уровни, подписи давлений.

---

### B.7 `ideal_gas` — Идеальный газ (pV = nRT)

**Файлы:** `IdealGasViewModel.cs`, `Physics/IdealGas.cs`, `IdealGasView`, `IdealGasCanvas`, `IdealGasChartControl`.

**Параметры:** `N` (моль), `Temperature` (K), `Volume` (м³) — базовый объём; для анимации используются `VMin = 0.4·Volume`, `VMax = 1.6·Volume`.

**Физика:** `Pressure = f(n, T, V)` по уравнению состояния; строится изотерма — точки `(V, P)` между VMin и VMax.

**Анимация:** цикл **4 с**, `CurrentV` меняется по **синусу** между VMin и VMax, `CurrentP` пересчитывается; график изотермы статичен при Build.

---

### B.8 `ohm_law` — Закон Ома (замкнутая цепь)

**Файлы:** `OhmLawViewModel.cs`, `Physics/OhmLaw.cs`, `OhmLawView`, `OhmLawCircuitCanvas`, `OhmLawChartControl`.

**Цепь:** ЭДС **ε**, внутреннее **r**, внешнее **R**, ток **I**. Связь: `I = ε/(r+R)`, падения напряжений, мощность, КПД `η = U_R/ε·100%`.

**Логика `Build()`:** если заданы **любые три** из четырёх (ε, r, R, I), недостающая величина **вычисляется** (обратные формулы в `OhmLaw`). Иначе подсказка «задайте три величины».

**Визуал:** схема цепи (источник, r, R), график/индикаторы U(I) при необходимости.

---

### B.9 `resistors_connection` — Резисторы последовательно / параллельно

**Файлы:** `ResistorsConnectionViewModel.cs`, `Physics/ResistorsConnection.cs`, `ResistorsConnectionView`.

**Параметры:** `VoltageV` (0…20 В), `R1`, `R2`, `R3` (0.1…100 Ом), переключатель **`IsSeries`** / параллель.

**Расчёт:** эквивалентное R, ток, падения U_i или токи в ветвях, мощность `P = U·I` (суммарная).  
**Столбчатые диаграммы:** свойства `BarWidthU`, `BarWidthI`, `BarWidthR`, `BarWidthP` — масштаб относительно максимумов (20 В, 2 А, 50 Ом, 50 Вт в комментарии).

---

### B.10 `charge_in_field` — Заряд в однородном электрическом поле

**Файлы:** `ChargeInFieldViewModel.cs`, `Physics/ChargeInField.cs`, `ChargeInFieldView`, `ChargeTrajectoryCanvas`, схемы.

**Модель:** поле **вниз** (как g в механике). `a_y = −q·E/m` (знак от q). Горизонталь: равномерное движение; вертикаль: равноускоренное. Аналог «броска» с заменой g на электрическое ускорение.

**Параметры:** `Q` (заряд, Кл), `E` (В/м), `M` (кг), `V0`, `AngleDegrees`.

**ViewModel:** ускорение, дальность, max высота, время полёта, траектория, анимация по `PositionAt`.

---

### B.11 `thin_lens` — Тонкая линза

**Файлы:** `ThinLensViewModel.cs`, `Physics/ThinLens.cs`, `ThinLensView`, `ThinLensCanvas`.

**Параметры (см):** `D` — расстояние предмет–линза, `F` — фокус, `ObjectHeight` — высота предмета.

**Расчёт:** `1/f = 1/d + 1/d'` → `d'`, увеличение `Γ`, высота изображения, признак действительного/мнимого. Особый случай **d = f** → изображение в бесконечности (`d'` NaN/∞, отдельный текст).

**Визуал:** оптическая ось, предмет, линза, три характерных луча, изображение.

---

### B.12 `refraction_at_boundary` — Преломление на границе

**Файлы:** `RefractionAtBoundaryViewModel.cs`, `Physics/RefractionAtBoundary.cs`, `RefractionAtBoundaryView`, `RefractionAtBoundaryCanvas`.

**Параметры:** показатели преломления `N1`, `N2`, угол падения **θ₁** (0…90°).

**Расчёт:** закон Снеллиуса; **полное внутреннее отражение** при выходе из более плотной среды; предельный угол. Отображение: `RefractionAngleDisplay`, `IsTotalInternalReflection`, `CriticalAngleDegrees`.

**Визуал:** два полупространства, нормаль, падающий и преломлённый луч (или отражение).

---

### B.13 `harmonic_oscillations` — Гармонические колебания (общий вид)

**Файлы:** `HarmonicOscillationsViewModel.cs`, `Physics/HarmonicOscillations.cs`, `HarmonicOscillationsView`, `HarmonicOscillationsCanvas`.

**Отличие от `spring`:** задаётся не k и m, а **амплитуда A**, **период T** (или из него ω = 2π/T), **начальная фаза φ₀** в градусах.

**Формулы:** `x(t) = A·cos(ωt + φ₀)`, `v(t)`, `a(t) = −ω²x`.

**ViewModel:** `Omega`, `Frequency`, график точек `GetChartPoints`, анимация обновляет `CurrentX`, `CurrentV`, `CurrentA`.

---

## Часть C. Рекомендации агенту Stride

1. **Сохранить `setup.Id`** из таблицы в §A.3 для сцен/скриптов и сохранений.
2. Вынести **`Physics/`** в общую сборку Game.Core; Stride-сцены только вызывают статические методы.
3. Заменить `DispatcherTimer` на **`Update()`** или `ScriptComponent` с накоплением времени.
4. Заменить LiveCharts на **кастомную отрисовку** (Stride UI, импостор, DebugDraw, или текстуры) — сохранить **оси и физические величины**.
5. Для каждой установки: один **Scene** или один **root entity** с дочерними UI + логикой; переключение по каталогу.
6. **Groq**: перенести HTTP; не хранить ключ в репозитории.

---

## Часть D. Быстрый указатель файлов по папкам

- `Navigation/PhysicsSection.cs` — каталог.
- `MainWindow.xaml.cs` — маршрутизация по id.
- `Physics/*.cs` — вся математика.
- `*ViewModel.cs` — состояние установок.
- `Pages/*View.xaml` — компоновка и слайдеры.
- `*Canvas.cs`, `*ChartControl.cs`, `*SchemeControl.cs` — отрисовка WPF.
- `Services/GroqService.cs`, `PhysicsChatContext.cs` — чат.

---

*При переносе на Stride сверяйтесь с исходными `.cs` в репозитории WPFPhisics1.1 для точных формул и диапазонов параметров.*
