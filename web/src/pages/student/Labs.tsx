import { Link } from "react-router-dom";
import { LAB_SECTIONS, LAB_SETUPS, READY_LABS } from "../../labs/catalog.ts";

export function Labs() {
  return (
    <div className="lab-index">
      {LAB_SECTIONS.map((section) => {
        const labs = LAB_SETUPS.filter((lab) => lab.section === section.id);
        if (labs.length === 0) return null;
        return (
          <section key={section.id} className="lab-card">
            <h2>{section.name}</h2>
            <ul>
              {labs.map((lab) => (
                <li key={lab.id}>
                  {READY_LABS.has(lab.id) ? (
                    <Link to={lab.id}>{lab.name}</Link>
                  ) : (
                    <span>{lab.name}</span>
                  )}
                </li>
              ))}
            </ul>
          </section>
        );
      })}
    </div>
  );
}
