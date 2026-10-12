import { Link } from "react-router-dom";

export function Gate() {
  return (
    <div className="gate">
      <span className="mark">Физика БРУ</span>
      <div className="gate-choices">
        <Link to="/student">Ученик</Link>
        <Link to="/teacher">Преподаватель</Link>
      </div>
    </div>
  );
}
