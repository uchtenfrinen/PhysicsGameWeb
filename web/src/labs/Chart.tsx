import { useEffect, useRef } from "react";

type Point = { t: number; y: number };

type Props = {
  points: Point[];
  markerT: number;
  markerY: number;
  title: string;
  xLabel: string;
  yLabel: string;
};

export function LabChart({ points, markerT, markerY, title, xLabel, yLabel }: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ points, markerT, markerY, title, xLabel, yLabel });
  const drawRef = useRef<() => void>(() => undefined);

  useEffect(() => {
    state.current = { points, markerT, markerY, title, xLabel, yLabel };
    drawRef.current();
  }, [points, markerT, markerY, title, xLabel, yLabel]);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;
    const parent = canvas.parentElement;
    if (!parent) return;

    const draw = () => {
      const s = state.current;
      const dpr = window.devicePixelRatio || 1;
      const w = parent.clientWidth;
      const h = parent.clientHeight;
      canvas.width = Math.max(1, Math.floor(w * dpr));
      canvas.height = Math.max(1, Math.floor(h * dpr));
      canvas.style.width = `${w}px`;
      canvas.style.height = `${h}px`;
      const ctx = canvas.getContext("2d");
      if (!ctx || w < 40 || h < 40) return;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, w, h);

      const mute = "#6e6a62";
      const line = "#d9d4c9";
      const ink = "#171614";
      const accent = "#2a5349";
      const tickText = (v: number) => {
        const a = Math.abs(v);
        if (a >= 100) return v.toFixed(0);
        if (a >= 10) return v.toFixed(0);
        return v.toFixed(1);
      };
      const maxT = Math.max(...s.points.map((p) => p.t), Number.isFinite(s.markerT) ? s.markerT : 0, 0.1);
      const ys = s.points.map((p) => p.y);
      const markY = Number.isFinite(s.markerY) ? [s.markerY] : [];
      const yMinRaw = Math.min(0, ...ys, ...markY);
      const yMaxRaw = Math.max(0, ...ys, ...markY);
      const yMin = yMinRaw === yMaxRaw ? yMinRaw - 0.01 : yMinRaw;
      const yMax = yMinRaw === yMaxRaw ? yMaxRaw + 0.01 : yMaxRaw;

      ctx.font = "10px Outfit, ui-sans-serif, sans-serif";
      let yTickW = 0;
      for (let i = 0; i <= 4; i++) {
        yTickW = Math.max(yTickW, ctx.measureText(tickText(yMin + ((yMax - yMin) * i) / 4)).width);
      }
      const xLabelW = s.xLabel ? ctx.measureText(s.xLabel).width : 0;
      const left = Math.max(36, yTickW + 8);
      const right = Math.max(12, xLabelW + 8);
      const top = 22;
      const bottom = 24;
      const plotW = w - left - right;
      const plotH = h - top - bottom;
      if (plotW <= 0 || plotH <= 0) return;
      const xOf = (t: number) => left + (t / maxT) * plotW;
      const yOf = (y: number) => top + plotH - ((y - yMin) / (yMax - yMin)) * plotH;

      ctx.strokeStyle = line;
      ctx.strokeRect(left, top, plotW, plotH);

      ctx.fillStyle = mute;
      ctx.font = "12px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText(s.title, left, 14);

      ctx.font = "10px Outfit, ui-sans-serif, sans-serif";
      for (let i = 0; i <= 4; i++) {
        const t = (maxT * i) / 4;
        const x = xOf(t);
        ctx.strokeStyle = line;
        ctx.beginPath();
        ctx.moveTo(x, top);
        ctx.lineTo(x, top + plotH);
        ctx.stroke();
        if (i === 4 && s.xLabel) continue;
        ctx.fillStyle = mute;
        const label = tickText(t);
        ctx.fillText(label, x - ctx.measureText(label).width / 2, h - 8);
      }
      for (let i = 0; i <= 4; i++) {
        const val = yMin + ((yMax - yMin) * i) / 4;
        const y = yOf(val);
        ctx.strokeStyle = line;
        ctx.beginPath();
        ctx.moveTo(left, y);
        ctx.lineTo(left + plotW, y);
        ctx.stroke();
        if (i === 4 && s.yLabel) continue;
        ctx.fillStyle = mute;
        const label = tickText(val);
        ctx.fillText(label, left - ctx.measureText(label).width - 6, y + 3);
      }

      ctx.fillStyle = mute;
      if (s.xLabel) ctx.fillText(s.xLabel, left + plotW + 4, h - 8);
      if (s.yLabel) ctx.fillText(s.yLabel, 6, top - 6);

      if (s.points.length >= 2) {
        ctx.strokeStyle = accent;
        ctx.lineWidth = 1.6;
        ctx.beginPath();
        s.points.forEach((p, i) => {
          const x = xOf(p.t);
          const y = yOf(p.y);
          if (i === 0) ctx.moveTo(x, y);
          else ctx.lineTo(x, y);
        });
        ctx.stroke();
        ctx.lineWidth = 1;

        const tMark = Math.min(Math.max(0, s.markerT), maxT);
        if (Number.isFinite(s.markerY) && Number.isFinite(s.markerT)) {
          const yMark = Math.min(Math.max(yMin, s.markerY), yMax);
          ctx.fillStyle = ink;
          ctx.beginPath();
          ctx.arc(xOf(tMark), yOf(yMark), 3, 0, Math.PI * 2);
          ctx.fill();
        }
      }
    };

    drawRef.current = draw;
    draw();
    const observer = new ResizeObserver(draw);
    observer.observe(parent);
    return () => observer.disconnect();
  }, []);

  return (
    <div className="chart-slot">
      <canvas ref={ref} />
    </div>
  );
}
