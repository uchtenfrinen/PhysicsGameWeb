import type { ReactNode } from "react";

export function LabShell({
  title,
  side,
  stage,
  charts,
  explanation,
  formulas,
  layout = "default",
}: {
  title: string;
  side: ReactNode;
  stage: ReactNode;
  charts: ReactNode;
  explanation: string;
  formulas?: ReactNode;
  layout?: "default" | "wide";
}) {
  const mid = (
    <>
      <div className="lab-stage">{stage}</div>
      <div className="lab-charts">{charts}</div>
    </>
  );

  return (
    <div className={layout === "wide" ? "lab lab-wide" : "lab"}>
      <aside className="lab-side">
        <p className="lab-id">{title}</p>
        {side}
      </aside>
      {layout === "wide" ? <div className="lab-mid">{mid}</div> : mid}
      <div className="lab-talk">
        {formulas ? <div className="lab-formulas">{formulas}</div> : null}
        <p className="lab-talk-text">{explanation}</p>
        <div className="lab-chat">
          <p className="lab-chat-title">Чат с ИИ</p>
          <div className="lab-chat-log" />
          <form onSubmit={(e) => e.preventDefault()}>
            <input disabled placeholder="вопрос" />
            <button type="submit" disabled>
              Отправить
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
