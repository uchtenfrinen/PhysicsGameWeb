import { LAB_SECTIONS } from "../labs/catalog.ts";

export type TaskKind = "numeric" | "choice";

export type TaskFile = {
  kind: "image" | "pdf";
  url: string;
  name: string;
};

export type TaskStub = {
  id: string;
  topicId: string;
  title: string;
  kind: TaskKind;
  condition?: string;
  files?: TaskFile[];
  labId?: string;
  options?: string[];
  expectedNumeric?: string;
  correctChoiceIndex?: number;
};

export const TASK_TOPICS = LAB_SECTIONS;

const STORAGE_KEY = "physics-bru-tasks";
const SEED_TASKS: TaskStub[] = [
  {
    id: "m-1",
    topicId: "mechanics",
    title: "Найти скорость у подножия склона",
    kind: "numeric",
    condition: "Тело соскальзывает без трения с высоты 5 м. Найти скорость у подножия. g = 10 м/с².",
    expectedNumeric: "10",
  },
  {
    id: "m-2",
    topicId: "mechanics",
    title: "Время падения с высоты 20 м",
    kind: "numeric",
    condition: "Тело падает с высоты 20 м без начальной скорости. Найти время падения. g = 10 м/с².",
    expectedNumeric: "2",
  },
  {
    id: "m-3",
    topicId: "mechanics",
    title: "Какая сила больше",
    kind: "choice",
    condition: "Брусок равномерно скользит вниз по наклонной плоскости. Сравните силу тяжести и силу трения.",
    options: ["тяжести", "трения", "равны"],
    correctChoiceIndex: 0,
  },
  {
    id: "h-1",
    topicId: "hydro",
    title: "Сила Архимеда на тело",
    kind: "numeric",
    condition: "Тело объёмом 0.002 м³ полностью погружено в воду (ρ = 1000 кг/м³). Найти силу Архимеда в ньютонах. g = 10 м/с².",
    expectedNumeric: "20",
  },
  {
    id: "h-2",
    topicId: "hydro",
    title: "Погрузится или всплывёт",
    kind: "choice",
    condition: "Плотность тела больше плотности жидкости. Что произойдёт с телом? Варианты: утонет, всплывёт, будет плавать где угодно.",
    options: ["утонет", "всплывёт", "будет плавать где угодно"],
    correctChoiceIndex: 0,
  },
  {
    id: "g-1",
    topicId: "heat",
    title: "Объём газа по уравнению состояния",
    kind: "numeric",
    condition: "p = 100 кПа, m = 0.028 кг газа (молярная масса M = 0.028 кг/моль), T = 300 К. Найти объём по уравнению Менделеева — Клапейрона. R = 8.31 Дж/(моль·К). Ответ в м³.",
    expectedNumeric: "0.025",
  },
  {
    id: "e-1",
    topicId: "electricity",
    title: "Сила тока в замкнутой цепи",
    kind: "numeric",
    condition: "ЭДС источника 12 В, внутреннее сопротивление 0.5 Ом, внешнее сопротивление 5.5 Ом. Найти силу тока в амперах.",
    expectedNumeric: "2",
  },
  {
    id: "o-1",
    topicId: "optics",
    title: "Изображение в собирающей линзе",
    kind: "numeric",
    condition: "Предмет стоит на расстоянии d = 60 см от собирающей линзы с фокусным расстоянием F = 20 см. Найти расстояние до изображения d' в сантиметрах.",
    expectedNumeric: "30",
  },
  {
    id: "w-1",
    topicId: "waves",
    title: "Период гармонических колебаний",
    kind: "numeric",
    condition: "Груз на пружине жёсткостью k = 100 Н/м массой 0.1 кг совершает гармонические колебания. Найти период в секундах (π ≈ 3.14).",
    expectedNumeric: "0.2",
  },
];

function loadTasks(): TaskStub[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [...SEED_TASKS];
    const parsed = JSON.parse(raw) as TaskStub[];
    if (!Array.isArray(parsed)) return [...SEED_TASKS];
    return parsed.filter((task) => task && typeof task.id === "string" && typeof task.topicId === "string");
  } catch {
    return [...SEED_TASKS];
  }
}

function saveTasks() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(TASKS));
  } catch {
    // Хранилище недоступно (private mode, переполнение) — работаем в памяти.
  }
}

export function resetTasksToSeed() {
  TASKS = [...SEED_TASKS];
  saveTasks();
}

export let TASKS: TaskStub[] = loadTasks();

export function tasksForTopic(topicId: string) {
  return TASKS.filter((task) => task.topicId === topicId);
}

export function findTask(taskId: string | undefined) {
  if (!taskId || taskId === "new") return undefined;
  return TASKS.find((task) => task.id === taskId);
}

export function addTask(task: TaskStub) {
  TASKS.push(task);
  saveTasks();
}

export function updateTask(id: string, patch: Partial<TaskStub>) {
  const current = TASKS.find((task) => task.id === id);
  if (!current) return;
  Object.assign(current, patch);
  saveTasks();
}

export function removeTask(id: string) {
  const index = TASKS.findIndex((task) => task.id === id);
  if (index >= 0) TASKS.splice(index, 1);
  saveTasks();
}

const SOLVED_KEY = "physics-bru-solved";

export function loadSolvedIds(): string[] {
  try {
    const raw = localStorage.getItem(SOLVED_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

export function saveSolvedIds(ids: string[]) {
  try {
    localStorage.setItem(SOLVED_KEY, JSON.stringify(ids));
  } catch {
    // Хранилище недоступно — статус живёт только в памяти страницы.
  }
}

export const KIND_LABEL: Record<TaskKind, string> = {
  numeric: "числовой ответ",
  choice: "выбор ответа",
};

export const EDITABLE_KINDS: TaskKind[] = ["numeric", "choice"];

export function parseAnswerNumber(value: string) {
  const n = Number(value.replace(",", ".").trim());
  return Number.isFinite(n) ? n : null;
}

export function isNumericCorrect(given: string, expected: string | undefined) {
  if (!expected?.trim()) return false;
  const a = parseAnswerNumber(given);
  const b = parseAnswerNumber(expected);
  if (a == null || b == null) return false;
  return Math.abs(a - b) <= Math.max(0.01, Math.abs(b) * 0.01);
}

export function isChoiceCorrect(index: number, correctIndex: number | undefined) {
  return correctIndex != null && index === correctIndex;
}
