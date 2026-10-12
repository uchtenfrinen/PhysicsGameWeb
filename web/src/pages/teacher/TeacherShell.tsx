import { Link, NavLink, Outlet, useParams } from "react-router-dom";
import { findTask, TASK_TOPICS } from "../../tasks/catalog.ts";

export function TeacherShell() {
  const { topicId, taskId } = useParams();
  const task = findTask(taskId);
  const topic = TASK_TOPICS.find((item) => item.id === topicId);
  const barTitle = taskId === "new" ? "новая задача" : (task?.title ?? topic?.name);

  return (
    <div className="shell">
      <header className="bar">
        <Link to="/" className="mark">
          Физика БРУ
        </Link>
        <nav>
          <NavLink to="/teacher/tasks">Задачи</NavLink>
        </nav>
        {barTitle ? <p className="bar-lab">{barTitle}</p> : null}
      </header>
      <div className="stage">
        <Outlet />
      </div>
    </div>
  );
}
