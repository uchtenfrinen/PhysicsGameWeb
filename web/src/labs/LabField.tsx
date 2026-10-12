export function LabHint({ text }: { text: string }) {
  return (
    <span
      className="hint"
      tabIndex={0}
      onMouseDown={(e) => e.preventDefault()}
    >
      ?
      <span className="hint-tip" role="tooltip">
        {text}
      </span>
    </span>
  );
}

export function LabField({
  label,
  value,
  digits,
  onCommit,
  hint,
}: {
  label: string;
  value: number;
  digits: number;
  onCommit: (n: number) => void;
  hint?: string;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      <input
        key={`${value.toFixed(digits)}`}
        defaultValue={value.toFixed(digits)}
        onBlur={(e) => {
          const n = Number(e.target.value.replace(",", "."));
          if (Number.isFinite(n)) onCommit(n);
          else e.target.value = value.toFixed(digits);
        }}
        onKeyDown={(e) => {
          if (e.key === "Enter") (e.target as HTMLInputElement).blur();
        }}
      />
      {hint ? <LabHint text={hint} /> : null}
    </label>
  );
}
