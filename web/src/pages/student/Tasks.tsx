import { useEffect, useState } from "react";
import { Navigate, NavLink, useParams } from "react-router-dom";
import {
  findTask,
  isChoiceCorrect,
  isNumericCorrect,
  KIND_LABEL,
  loadSolvedIds,
  saveSolvedIds,
  TASK_TOPICS,
  type TaskFile,
  type TaskStub,
  tasksForTopic,
} from "../../tasks/catalog.ts";

export function Tasks() {
  const { topicId, taskId } = useParams();
  const topic = TASK_TOPICS.find((item) => item.id === topicId);
  const [solved, setSolved] = useState<string[]>(() => loadSolvedIds());
  const list = topic ? tasksForTopic(topic.id) : [];
  const task = list.find((item) => item.id === taskId) ?? findTask(taskId);

  useEffect(() => {
    saveSolvedIds(solved);
  }, [solved]);

  if (!topic) {
    return <Navigate to={`/student/tasks/${TASK_TOPICS[0].id}`} replace />;
  }
  if (taskId && (!task || task.topicId !== topic.id)) {
    const fallback = list[0];
    return (
      <Navigate
        to={fallback ? `/student/tasks/${topic.id}/${fallback.id}` : `/student/tasks/${topic.id}`}
        replace
      />
    );
  }

  const selected = task && task.topicId === topic.id ? task : undefined;

  return (
    <div className="task-work">
      <nav className="task-topics">
        {TASK_TOPICS.map((item) => (
          <NavLink key={item.id} to={`/student/tasks/${item.id}`}>
            {item.name}
          </NavLink>
        ))}
      </nav>
      <ul className="task-list">
        {list.map((item) => (
          <li key={item.id}>
            <NavLink to={`/student/tasks/${topic.id}/${item.id}`} end>
              <span>{item.title}</span>
              <span className={solved.includes(item.id) ? "task-list-solved" : undefined}>
                {solved.includes(item.id) ? "решена" : KIND_LABEL[item.kind]}
              </span>
            </NavLink>
          </li>
        ))}
      </ul>
      <section className="task-solve">
        {selected ? (
          <>
            <div className="task-condition">
              {selected.condition}
              <TaskFiles files={selected.files} />
            </div>
            <TaskAnswer
              key={selected.id}
              task={selected}
              onVerdict={(ok) => {
                if (!ok) return;
                setSolved((current) =>
                  current.includes(selected.id) ? current : [...current, selected.id],
                );
              }}
            />
          </>
        ) : null}
      </section>
    </div>
  );
}

function TaskFiles({ files }: { files?: TaskFile[] }) {
  if (!files?.length) return null;
  return (
    <div className="task-files">
      {files.map((file) =>
        file.kind === "image" ? (
          <a key={file.url} href={file.url} target="_blank" rel="noreferrer">
            <img src={file.url} alt={file.name} />
          </a>
        ) : (
          <a key={file.url} className="task-file-pdf" href={file.url} target="_blank" rel="noreferrer">
            {file.name}
          </a>
        ),
      )}
    </div>
  );
}

function TaskAnswer({ task, onVerdict }: { task: TaskStub; onVerdict: (ok: boolean) => void }) {
  const [value, setValue] = useState("");
  const [picked, setPicked] = useState<number | null>(null);
  const [ok, setOk] = useState<boolean | null>(null);

  const verdict =
    ok == null ? null : (
      <p className={ok ? "task-verdict is-ok" : "task-verdict is-no"}>{ok ? "решена" : "не решена"}</p>
    );

  if (task.kind === "choice") {
    return (
      <div className="task-answer">
        {(task.options ?? []).map((option, index) => (
          <button
            type="button"
            key={`${option}-${index}`}
            className={picked === index ? "active" : undefined}
            onClick={() => {
              const ok = isChoiceCorrect(index, task.correctChoiceIndex);
              setPicked(index);
              setOk(ok);
              onVerdict(ok);
            }}
          >
            {option}
          </button>
        ))}
        {verdict}
      </div>
    );
  }

  return (
    <form
      className="task-answer"
      onSubmit={(e) => {
        e.preventDefault();
        const ok = isNumericCorrect(value, task.expectedNumeric);
        setOk(ok);
        onVerdict(ok);
      }}
    >
      <input
        inputMode="decimal"
        placeholder="ответ"
        value={value}
        onChange={(e) => {
          setValue(e.target.value);
          setOk(null);
        }}
      />
      <button type="submit" className="act-primary">
        Проверить
      </button>
      {verdict}
    </form>
  );
}
