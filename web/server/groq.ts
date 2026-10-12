import { existsSync, readFileSync } from "node:fs";
import type { IncomingMessage, ServerResponse } from "node:http";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import type { Plugin } from "vite";

const NO_LATEX =
  "\n\nВажно: формулы пиши без LaTeX и без разметки. Используй только обычный текст: буквы греческого алфавита (α, β, ω, ν, ρ, θ), подстрочные индексы Unicode (₀, ₁, ₓ, например v₀, y₀, x₁), точку умножения ·, дроби в одну строку вида a/b или (числитель)/(знаменатель). Не используй \\[, \\], \\frac, \\cos, \\sin, \\tan, \\cdot и подобные команды — они отображаются как непонятные символы.";

const USE_GIVEN =
  "\n\nОбязательно: в тексте объяснения используй только те численные значения (L, H, T, силы, и т.д.), которые указаны выше в задании. Не пересчитывай их по формулам и не приводи другие числа — все цифры должны совпадать с результатами расчёта в приложении.";

function findKey(): string {
  const env = (process.env.GROQ_API_KEY ?? "").trim();
  if (env) return env;
  const here = dirname(fileURLToPath(import.meta.url));
  const files = [
    resolve(here, "..", "groq_api_key.txt"),
    resolve(here, "..", "..", "groq_api_key.txt"),
    resolve(here, "..", "..", "WPFPhisics1.1", "WPFPhisics1.1", "groq_api_key.txt"),
  ];
  for (const file of files) {
    if (!existsSync(file)) continue;
    const key = readFileSync(file, "utf8").trim();
    if (key) return key;
  }
  return "";
}

function sanitizeLatex(text: string): string {
  return text
    .replace(/\\\[/g, "")
    .replace(/\\\]/g, "")
    .replace(/\\\(/g, "")
    .replace(/\\\)/g, "")
    .replace(/\\cdot/g, "·")
    .replace(/\\times/g, "×");
}

async function complete(prompt: string): Promise<string> {
  const key = findKey();
  if (!key) {
    return "Ключ API не найден. Задайте GROQ_API_KEY или файл groq_api_key.txt.";
  }
  const response = await fetch("https://api.groq.com/openai/v1/chat/completions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${key}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      model: "llama-3.1-8b-instant",
      messages: [{ role: "user", content: prompt }],
      max_tokens: 1024,
    }),
  });
  if (!response.ok) {
    return `Ошибка API: ${response.status}. ${await response.text()}`;
  }
  const data = (await response.json()) as {
    choices?: { message?: { content?: string } }[];
  };
  const content = data.choices?.[0]?.message?.content;
  return sanitizeLatex(content ?? "Пустой ответ.");
}

function readBody(req: IncomingMessage): Promise<string> {
  return new Promise((resolveBody, reject) => {
    const chunks: Buffer[] = [];
    req.on("data", (chunk: Buffer) => chunks.push(chunk));
    req.on("end", () => resolveBody(Buffer.concat(chunks).toString("utf8")));
    req.on("error", reject);
  });
}

async function handle(
  req: IncomingMessage,
  res: ServerResponse,
  kind: "explain" | "chat",
): Promise<void> {
  try {
    const body = JSON.parse(await readBody(req)) as {
      prompt?: string;
      context?: string;
      question?: string;
    };
    let prompt = "";
    if (kind === "explain") {
      prompt = `${body.prompt ?? ""}${NO_LATEX}${USE_GIVEN}`;
    } else {
      prompt =
        "Ты — помощник по физике в учебном приложении. Отвечай только на вопросы в рамках физики: механика, гидростатика, термодинамика, электричество, оптика, колебания. " +
        "Если вопрос не по физике — вежливо откажись и предложи переформулировать в контексте физики.\n\n" +
        (body.context?.trim() ? `Контекст текущей установки: ${body.context.trim()}\n\n` : "") +
        `Вопрос пользователя: ${(body.question ?? "").trim()}` +
        NO_LATEX;
    }
    const text = await complete(prompt);
    res.setHeader("Content-Type", "application/json; charset=utf-8");
    res.end(JSON.stringify({ text }));
  } catch (error) {
    res.statusCode = 500;
    res.setHeader("Content-Type", "application/json; charset=utf-8");
    res.end(JSON.stringify({ text: `Ошибка: ${String(error)}` }));
  }
}

export function groqPlugin(): Plugin {
  const middleware = (req: IncomingMessage, res: ServerResponse, next: () => void) => {
    const url = req.url?.split("?")[0];
    if (url !== "/api/explain" && url !== "/api/chat") {
      next();
      return;
    }
    if (req.method !== "POST") {
      res.statusCode = 405;
      res.end();
      return;
    }
    void handle(req, res, url === "/api/explain" ? "explain" : "chat");
  };

  return {
    name: "groq-proxy",
    configureServer(server) {
      server.middlewares.use(middleware);
    },
  };
}
