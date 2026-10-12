using PhisicsGame.Core.Text;

namespace PhisicsGame.Core.Localization;

public sealed class LocalizationService
{
    public string CurrentLanguage { get; } = "ru";

    private readonly Dictionary<string, string> _strings = new()
    {
        ["menu.start"] = "Нажмите Enter, чтобы начать",
        ["menu.topicPortals"] = "Решение задач",
        ["menu.settings"] = "Настройки",
        ["menu.exit"] = "Выход",
        ["menu.footer.select"] = "Enter — выбрать",
        ["loading.line"] = "Загрузка: {0}",
        ["loading.status"] = "Загрузка",
        ["ui.labButton"] = "Лаборатория",
        ["world.help"] = "WASD — движение, E — взаимодействие, Esc — назад",
        ["world.taskHint"] = "Подойдите к терминалу: E — задача, Tab — подсказка профессора",
        ["world.hud.labFirst"] = "E — показать подсказку (лаборатория)",
        ["world.hud.labSecond"] = "E — открыть лабораторный модуль",
        ["world.connecting.lab"] = "Подключение к лаборатории…",
        ["world.lab.portal"] = "Лаборатория",
        ["world.interact"] = "Нажмите E",
        ["hint.panel.world"] = "Псс-т! Это терминал «{0}». Нажми E — загрузится задача. Считай внимательно: ответ проверяется автоматически.",
        ["hint.panel.world.solved"] = "Ты уже проходил «{0}». Нажми E, если хочешь закрепить материал ещё раз.",
        ["hint.panel.portal"] = "Тема «{0}»: здесь будут задачи по разделу. Готов? Нажми E ещё раз — перейти к задачам.",
        ["hint.panel.lab.cloud1"] = "Откроется отдельное окно. Игра не закроется — можно переключаться между окнами.",
        ["hint.panel.lab.tab"] = "Здесь доступны лабораторный модуль, симулятор и конструктор установок — в отдельном окне. Игра не закроется. Сначала E — подсказка, затем E — запуск.",
        ["portal.hub.help"] = "WASD — движение, E — задачи по теме, Esc — меню",
        ["portal.hub.eFirst"] = "E — подсказка профессора",
        ["portal.hub.eSecond"] = "E — перейти в раздел",
        ["hint.panel.topic"] = "«{0}» — задача из этой темы. Нажми E — откроется проверка. Сначала спокойно прочитай условие.",
        ["hint.panel.topicCatalog"] = "Раздел «{0}»: здесь один терминал — все задачи темы открываются в приложении Windows (WPF). Подойди и нажми E.",
        ["topic.hud.topicLine"] = "Тема: {0}",
        ["topic.hud.footer"] = "E — каталог задач раздела (WPF), Tab — подсказка, Esc — к порталам",
        ["task.controls"] = "Цифры | Backspace | Проверить / Enter",
        ["task.sidebar.theory"] = "ТЕОРИЯ",
        ["task.sidebar.tasks"] = "ЗАДАЧИ",
        ["task.sidebar.board"] = "ДОСКА",
        ["task.placeholder.theory"] = "Раздел Теория скоро будет доступен.",
        ["task.theory.output"] = "ВЫВОД",
        ["task.theory.integralDesc"] = "Определённый интеграл по [0; 3,14].",
        ["task.theory.integralLo"] = "0",
        ["task.theory.integralHi"] = "3,14",
        ["task.theory.integralArg"] = "sin(x)dx",
        ["task.board.title"] = "Доска",
        ["task.board.hint"] = "ЛКМ - рисовать, C - очистить, Esc - назад",
        ["task.check"] = "Проверить",
        ["task.progress"] = "Задача {0} из {1}",
        ["task.lab.title"] = "Лаборатория",
        ["task.lab.hint"] = "Рекомендуемая установка из окна лаборатории WPF.",
        ["task.lab.openWpf"] = "Открыть установку (WPF)",
        ["task.lab.noWpf"] = "Для этой задачи пока нет связанной установки в каталоге лаборатории.",
        ["task.lab.embeddedWpf"] = "Игра уже в окне лаборатории — выберите установку в меню WPF слева.",
        ["task.lab.wpfFailed"] = "Не удалось открыть: {0}",
        ["task.wpfSetup.inclined_plane"] = "Груз на наклонной плоскости",
        ["task.wpfSetup.free_fall"] = "Свободное падение",
        ["task.wpfSetup.projectile"] = "Движение тела, брошенного под углом",
        ["task.wpfSetup.spring"] = "Пружина с грузом",
        ["task.wpfSetup.buoyancy"] = "Плавание тела (Архимед)",
        ["task.wpfSetup.communicating_vessels"] = "Сообщающиеся сосуды",
        ["task.wpfSetup.ideal_gas"] = "Идеальный газ",
        ["task.wpfSetup.ohm_law"] = "Закон Ома",
        ["task.wpfSetup.resistors_connection"] = "Резисторы: последовательно/параллельно",
        ["task.wpfSetup.charge_in_field"] = "Заряд в однородном поле",
        ["task.wpfSetup.thin_lens"] = "Тонкая линза",
        ["task.wpfSetup.refraction_at_boundary"] = "Преломление на границе",
        ["task.wpfSetup.harmonic_oscillations"] = "Гармонические колебания",
        ["task.success"] = "Верно! Объект активирован.",
        ["task.fail"] = "Неверно, попробуйте снова.",
        ["dialog.done1"] = "Отлично! Вы применили второй закон Ньютона.",
        ["dialog.done2"] = "Продолжайте исследование и ищите новые терминалы.",
        ["task.ohm.title"] = "Закон Ома",
        ["task.ohm.desc"] = "Выберите верную формулу для силы тока.",
        ["task.ohm.formula"] = "I = U / R",
        ["task.choice.0"] = "I = U / R",
        ["task.choice.1"] = "I = U * R",
        ["task.choice.2"] = "I = R / U",
        ["task.sim.title"] = "Простая симуляция",
        ["task.sim.desc"] = "Настройте ускорение тележки до 8.0 м/с^2.",
        ["task.sim.formula"] = "a_target = 8.0 м/с^2",
        ["task.electro.title"] = "Электростатика",
        ["task.electro.desc"] = "Как направлена сила между разноименными зарядами?",
        ["task.electro.formula"] = "F ~ q1 * q2 / r^2",
        ["task.electro.choice0"] = "Притяжение",
        ["task.electro.choice1"] = "Отталкивание",
        ["task.electro.choice2"] = "Силы нет",
        ["task.optics.title"] = "Оптика",
        ["task.optics.desc"] = "Показатель преломления n = c/v. Если v = c/3, найдите n.",
        ["task.optics.formula"] = "n = c / v",
        ["task.thermal.title"] = "Термодинамика",
        ["task.thermal.desc"] = "Для идеального газа V/T = const. При T выросла в 2 раза, как меняется V?",
        ["task.thermal.formula"] = "V2/V1 = T2/T1",
        ["task.atomic.title"] = "Атомная физика",
        ["task.atomic.desc"] = "Какой тип излучения имеет наибольшую проникающую способность?",
        ["task.atomic.formula"] = "alpha < beta < gamma",
        ["task.atomic.choice0"] = "Альфа",
        ["task.atomic.choice1"] = "Гамма",
        ["task.atomic.choice2"] = "Бета"
    };

    public string T(string key, params object?[] args)
    {
        if (_strings.TryGetValue(key, out var value))
        {
            var text = args is { Length: > 0 } ? string.Format(value, args) : value;
            return SpriteFontText.Sanitize(text);
        }

        var fallback = key;
        var fallbackText = args is { Length: > 0 } ? string.Format(fallback, args) : fallback;
        return SpriteFontText.Sanitize(fallbackText);
    }
}
