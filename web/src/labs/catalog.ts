export type LabSetup = {
  id: string;
  name: string;
  section: string;
};

export const LAB_SECTIONS: { id: string; name: string }[] = [
  { id: "mechanics", name: "Механика" },
  { id: "hydro", name: "Гидростатика" },
  { id: "heat", name: "Термодинамика" },
  { id: "electricity", name: "Электричество" },
  { id: "optics", name: "Оптика" },
  { id: "waves", name: "Колебания и волны" },
];

export const LAB_SETUPS: LabSetup[] = [
  { id: "inclined_plane", name: "Груз на наклонной плоскости", section: "mechanics" },
  { id: "free_fall", name: "Свободное падение", section: "mechanics" },
  { id: "projectile", name: "Движение тела, брошенного под углом", section: "mechanics" },
  { id: "spring", name: "Пружина с грузом", section: "mechanics" },
  { id: "buoyancy", name: "Плавание тела (Архимед)", section: "hydro" },
  { id: "communicating_vessels", name: "Сообщающиеся сосуды", section: "hydro" },
  { id: "ideal_gas", name: "Уравнение состояния идеального газа", section: "heat" },
  { id: "ohm_law", name: "Закон Ома для замкнутой цепи", section: "electricity" },
  { id: "resistors_connection", name: "Резисторы: последовательно/параллельно", section: "electricity" },
  { id: "charge_in_field", name: "Заряд в однородном поле", section: "electricity" },
  { id: "thin_lens", name: "Тонкая линза", section: "optics" },
  { id: "refraction_at_boundary", name: "Преломление на границе", section: "optics" },
  { id: "harmonic_oscillations", name: "Гармонические колебания", section: "waves" },
];

export const READY_LABS = new Set([
  "free_fall",
  "inclined_plane",
  "projectile",
  "spring",
  "buoyancy",
  "communicating_vessels",
  "ideal_gas",
  "ohm_law",
  "resistors_connection",
  "charge_in_field",
  "thin_lens",
  "refraction_at_boundary",
  "harmonic_oscillations",
]);
