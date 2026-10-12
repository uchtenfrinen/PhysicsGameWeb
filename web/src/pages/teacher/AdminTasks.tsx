import { useEffect, useRef, useState } from "react";
import { Navigate, NavLink, useNavigate, useParams } from "react-router-dom";
import {
  addTask,
  EDITABLE_KINDS,
  KIND_LABEL,
  removeTask,
  TASK_TOPICS,
  type TaskFile,
  type TaskKind,
  type TaskStub,
  tasksForTopic,
  updateTask,
} from "../../tasks/catalog.ts";

export function AdminTasks() {
  const { topicId, taskId } = useParams();
  const navigate = useNavigate();
  const topic = TASK_TOPICS.find((item) => item.id === topicId);
  const [, setRev] = useState(0);
  const list = topic ? tasksForTopic(topic.id) : [];
  const isNew = taskId === "new";
  const selected = isNew ? undefined : list.find((item) => item.id === taskId);

  if (!topic) {
    return <Navigate to={`/teacher/tasks/${TASK_TOPICS[0].id}`} replace />;
  }
  if (taskId && !isNew && !selected) {
    return <Navigate to={`/teacher/tasks/${topic.id}`} replace />;
  }

  return (
    <div className="task-work">
      <nav className="task-topics">
        {TASK_TOPICS.map((item) => (
          <NavLink key={item.id} to={`/teacher/tasks/${item.id}`}>
            {item.name}
          </NavLink>
        ))}
      </nav>
      <div className="task-list-col">
        <button type="button" className="task-add" onClick={() => navigate(`/teacher/tasks/${topic.id}/new`)}>
          Добавить
        </button>
        <ul className="task-list">
          {list.map((item) => (
            <li key={item.id}>
              <NavLink to={`/teacher/tasks/${topic.id}/${item.id}`} end>
                <span>{item.title}</span>
                <span>{KIND_LABEL[item.kind]}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </div>
      <section className={isNew || selected ? "task-solve" : "task-solve task-solve-empty"}>
        {isNew || selected ? (
          <TaskEditor
            key={taskId ?? "empty"}
            topicId={topic.id}
            task={selected}
            onSaved={(id) => {
              setRev((n) => n + 1);
              navigate(`/teacher/tasks/${topic.id}/${id}`);
            }}
            onDeleted={() => {
              setRev((n) => n + 1);
              navigate(`/teacher/tasks/${topic.id}`);
            }}
          />
        ) : (
          <p className="task-panel-title">Панель преподавателя</p>
        )}
      </section>
    </div>
  );
}

function TaskEditor({
  topicId,
  task,
  onSaved,
  onDeleted,
}: {
  topicId: string;
  task?: TaskStub;
  onSaved: (id: string) => void;
  onDeleted: () => void;
}) {
  const [title, setTitle] = useState(task?.title ?? "");
  const [condition, setCondition] = useState(task?.condition ?? "");
  const [kind, setKind] = useState<TaskKind>(task?.kind === "choice" ? "choice" : "numeric");
  const [expectedNumeric, setExpectedNumeric] = useState(task?.expectedNumeric ?? "");
  const [options, setOptions] = useState(task?.options?.length ? task.options : ["", "", ""]);
  const [correctChoiceIndex, setCorrectChoiceIndex] = useState(task?.correctChoiceIndex ?? 0);
  const [files, setFiles] = useState<TaskFile[]>(task?.files ?? []);
  const photoRef = useRef<HTMLInputElement>(null);
  const pdfRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setTitle(task?.title ?? "");
    setCondition(task?.condition ?? "");
    setKind(task?.kind === "choice" ? "choice" : "numeric");
    setExpectedNumeric(task?.expectedNumeric ?? "");
    setOptions(task?.options?.length ? task.options : ["", "", ""]);
    setCorrectChoiceIndex(task?.correctChoiceIndex ?? 0);
    setFiles(task?.files ?? []);
  }, [task]);

  const addFiles = (list: FileList | null, kind: TaskFile["kind"]) => {
    if (!list) return;
    const pending = Array.from(list);
    const store = (url: string, name: string) => {
      setFiles((current) => [...current, { kind, url, name }]);
    };
    pending.forEach((file) => {
      const reader = new FileReader();
      reader.onload = () => store(reader.result as string, file.name);
      reader.readAsDataURL(file);
    });
  };

  const save = () => {
    const nextTitle = title.trim();
    if (!nextTitle) return;
    const kept = options
      .map((text, index) => ({ text: text.trim(), index }))
      .filter((item) => item.text);
    const patch: Partial<TaskStub> = {
      title: nextTitle,
      condition: condition.trim(),
      kind,
      files,
      expectedNumeric: kind === "numeric" ? expectedNumeric.trim() : undefined,
      options: kind === "choice" ? kept.map((item) => item.text) : undefined,
      correctChoiceIndex:
        kind === "choice" ? Math.max(0, kept.findIndex((item) => item.index === correctChoiceIndex)) : undefined,
    };
    if (task) {
      updateTask(task.id, patch);
      onSaved(task.id);
      return;
    }
    const id = `t-${Date.now().toString(36)}`;
    addTask({ id, topicId, ...patch, kind, title: nextTitle });
    onSaved(id);
  };

  return (
    <form
      className="admin-form"
      onSubmit={(e) => {
        e.preventDefault();
        save();
      }}
    >
      <label>
        Заголовок
        <input value={title} onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label className="admin-form-grow">
        Условие
        <textarea value={condition} onChange={(e) => setCondition(e.target.value)} />
      </label>
      <div className="admin-files">
        <input
          ref={photoRef}
          type="file"
          accept="image/*"
          multiple
          hidden
          onChange={(e) => {
            addFiles(e.target.files, "image");
            e.target.value = "";
          }}
        />
        <input
          ref={pdfRef}
          type="file"
          accept="application/pdf"
          multiple
          hidden
          onChange={(e) => {
            addFiles(e.target.files, "pdf");
            e.target.value = "";
          }}
        />
        <button type="button" onClick={() => photoRef.current?.click()}>
          фото
        </button>
        <button type="button" onClick={() => pdfRef.current?.click()}>
          PDF
        </button>
        {files.map((file) => (
          <span key={file.url} className="admin-file-name">
            <a href={file.url} target="_blank" rel="noreferrer">
              {file.name}
            </a>
            <button
              type="button"
              onClick={() => {
                setFiles((current) => current.filter((item) => item.url !== file.url));
              }}
            >
              убрать
            </button>
          </span>
        ))}
      </div>
      <div className="admin-kind">
        {EDITABLE_KINDS.map((item) => (
          <button type="button" key={item} className={kind === item ? "active" : undefined} onClick={() => setKind(item)}>
            {KIND_LABEL[item]}
          </button>
        ))}
      </div>
      {kind === "numeric" ? (
        <label>
          Верный ответ
          <input value={expectedNumeric} onChange={(e) => setExpectedNumeric(e.target.value)} inputMode="decimal" />
        </label>
      ) : (
        <div className="admin-options">
          {options.map((option, index) => (
            <label key={index}>
              Вариант {index + 1}
              <input
                value={option}
                onChange={(e) => {
                  const next = options.slice();
                  next[index] = e.target.value;
                  setOptions(next);
                }}
              />
              <button
                type="button"
                className={correctChoiceIndex === index ? "active" : undefined}
                onClick={() => setCorrectChoiceIndex(index)}
              >
                верный
              </button>
            </label>
          ))}
        </div>
      )}
      <div className="task-answer">
        <button type="submit" className="act-primary">
          Сохранить
        </button>
        {task ? (
          <button
            type="button"
            onClick={() => {
              removeTask(task.id);
              onDeleted();
            }}
          >
            Удалить
          </button>
        ) : null}
      </div>
    </form>
  );
}
