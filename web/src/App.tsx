import { Navigate, Route, Routes } from "react-router-dom";
import { Gate } from "./pages/Gate.tsx";
import { LabRoute } from "./pages/student/LabRoute.tsx";
import { Labs } from "./pages/student/Labs.tsx";
import { StudentShell } from "./pages/student/StudentShell.tsx";
import { Tasks } from "./pages/student/Tasks.tsx";
import { AdminTasks } from "./pages/teacher/AdminTasks.tsx";
import { TeacherShell } from "./pages/teacher/TeacherShell.tsx";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Gate />} />
      <Route path="/student" element={<StudentShell />}>
        <Route index element={<Navigate to="labs" replace />} />
        <Route path="play" element={<Navigate to="/student/labs" replace />} />
        <Route path="labs" element={<Labs />} />
        <Route path="labs/:setupId" element={<LabRoute />} />
        <Route path="tasks" element={<Tasks />} />
        <Route path="tasks/:topicId" element={<Tasks />} />
        <Route path="tasks/:topicId/:taskId" element={<Tasks />} />
      </Route>
      <Route path="/teacher" element={<TeacherShell />}>
        <Route index element={<Navigate to="tasks" replace />} />
        <Route path="tasks" element={<AdminTasks />} />
        <Route path="tasks/:topicId" element={<AdminTasks />} />
        <Route path="tasks/:topicId/:taskId" element={<AdminTasks />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
