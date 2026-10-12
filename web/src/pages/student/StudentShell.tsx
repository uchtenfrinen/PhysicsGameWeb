import { Link, NavLink, Outlet, useParams } from "react-router-dom";
import { LAB_SETUPS } from "../../labs/catalog.ts";
import { findTask, TASK_TOPICS } from "../../tasks/catalog.ts";

export function StudentShell() {
  const { setupId, topicId, taskId } = useParams();
  const setup = LAB_SETUPS.find((lab) => lab.id === setupId);
  const task = findTask(taskId);
  const topic = TASK_TOPICS.find((item) => item.id === topicId);
  const barTitle = setup?.name ?? task?.title ?? topic?.name;

  return (
    <div className="shell">
      <header className="bar">
        <Link to="/" className="mark">
          Физика БРУ
        </Link>
        <nav>
          <NavLink to="/student/labs">Лаборатории</NavLink>
          <NavLink to="/student/tasks">Задачи</NavLink>
        </nav>
        {barTitle ? <p className="bar-lab">{barTitle}</p> : null}
      </header>
      <div className="stage">
        <Outlet />
      </div>
    </div>
  );
}
