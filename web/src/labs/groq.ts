export async function requestExplain(prompt: string): Promise<string> {
  const response = await fetch("/api/explain", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ prompt }),
  });
  const data = (await response.json()) as { text?: string };
  return data.text ?? "Пустой ответ.";
}

export async function requestChat(context: string, question: string): Promise<string> {
  const response = await fetch("/api/chat", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ context, question }),
  });
  const data = (await response.json()) as { text?: string };
  return data.text ?? "Пустой ответ.";
}
