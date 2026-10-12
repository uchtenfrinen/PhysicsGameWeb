import { useEffect, useRef } from "react";
import { arrow, colEl, colN, colTr, fitCanvas, ink, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  n1: number;
  n2: number;
  theta1: number;
  theta2: number;
  tir: boolean;
  progress: number;
  playing: boolean;
  dragEnabled: boolean;
  onPlaceTheta1: (deg: number) => void;
  onPlaceTheta2: (deg: number) => void;
};

type Pt = { x: number; y: number };

function layoutRefraction(w: number, h: number) {
  const margin = 24;
  const cx = w / 2;
  const cy = h / 2;
  const plotW = w - 2 * margin;
  const plotH = h - 2 * margin;
  const scale = Math.min(plotW, plotH) * 0.48;
  const xOf = (xw: number) => cx + xw * scale;
  const yOf = (yw: number) => cy - yw * scale;
  const xFromPx = (px: number) => (px - cx) / scale;
  const yFromPx = (py: number) => (cy - py) / scale;
  return { margin, cx, cy, scale, xOf, yOf, xFromPx, yFromPx };
}

function lerp(a: Pt, b: Pt, t: number): Pt {
  const k = Math.min(1, Math.max(0, t));
  return { x: a.x + (b.x - a.x) * k, y: a.y + (b.y - a.y) * k };
}

function along(pts: Pt[], t: number): Pt {
  if (pts.length < 2) return pts[0] ?? { x: 0, y: 0 };
  const n = pts.length - 1;
  const u = Math.min(1, Math.max(0, t)) * n;
  const i = Math.min(n - 1, Math.floor(u));
  return lerp(pts[i], pts[i + 1], u - i);
}

function angleFromPoint(xw: number, yw: number) {
  return (Math.atan2(Math.abs(xw), Math.abs(yw)) * 180) / Math.PI;
}

export function RefractionCanvas({
  n1,
  n2,
  theta1,
  theta2,
  tir,
  progress,
  playing,
  dragEnabled,
  onPlaceTheta1,
  onPlaceTheta2,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({
    n1,
    n2,
    theta1,
    theta2,
    tir,
    progress,
    playing,
    dragEnabled,
    onPlaceTheta1,
    onPlaceTheta2,
  });
  const drawRef = useRef<() => void>(() => undefined);
  const drag = useRef<"theta1" | "theta2" | null>(null);

  useEffect(() => {
    state.current = {
      n1,
      n2,
      theta1,
      theta2,
      tir,
      progress,
      playing,
      dragEnabled,
      onPlaceTheta1,
      onPlaceTheta2,
    };
    drawRef.current();
  }, [n1, n2, theta1, theta2, tir, progress, playing, dragEnabled, onPlaceTheta1, onPlaceTheta2]);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;
    const parent = canvas.parentElement;
    if (!parent) return;

    const pointOnCanvas = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    };

    const draw = () => {
      const sized = fitCanvas(canvas, parent);
      if (!sized) return;
      const { ctx, w, h } = sized;
      const s = state.current;
      const g = layoutRefraction(w, h);
      const { margin, cx, cy, xOf, yOf } = g;
      const rad1 = (s.theta1 * Math.PI) / 180;
      const rad2 = Number.isFinite(s.theta2) ? (s.theta2 * Math.PI) / 180 : 0;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";

      ctx.fillStyle = "rgba(42, 83, 73, 0.08)";
      ctx.fillRect(margin, cy, w - 2 * margin, h - margin - cy);

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(margin, cy);
      ctx.lineTo(w - margin, cy);
      ctx.stroke();
      ctx.setLineDash([5, 4]);
      ctx.strokeStyle = mute;
      ctx.beginPath();
      ctx.moveTo(cx, cy - Math.min((h - 2 * margin) / 2 - 8, 52));
      ctx.lineTo(cx, cy + Math.min((h - 2 * margin) / 2 - 8, 52));
      ctx.stroke();
      ctx.setLineDash([]);

      const x1 = -Math.sin(rad1);
      const y1 = Math.cos(rad1);
      const p0 = { x: xOf(x1), y: yOf(y1) };
      const p1 = { x: cx, y: cy };
      ctx.strokeStyle = colEl;
      ctx.fillStyle = colEl;
      arrow(ctx, p0.x, p0.y, p1.x, p1.y);

      let p2: Pt;
      if (s.tir) {
        p2 = { x: xOf(Math.sin(rad1)), y: yOf(Math.cos(rad1)) };
        ctx.strokeStyle = colTr;
        ctx.fillStyle = colTr;
        arrow(ctx, p1.x, p1.y, p2.x, p2.y);
        ctx.fillText("полное отражение", p2.x + 10, p2.y - 4);
      } else {
        p2 = { x: xOf(Math.sin(rad2)), y: yOf(-Math.cos(rad2)) };
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, p1.x, p1.y, p2.x, p2.y);
      }

      ctx.fillStyle = mute;
      ctx.fillText(`n₁ ${s.n1.toFixed(2)}`, margin + 8, cy - 14);
      ctx.fillText(`n₂ ${s.n2.toFixed(2)}`, margin + 8, cy + 22);

      const t1 = { x: xOf(-Math.sin(rad1 / 2) * 0.38), y: yOf(Math.cos(rad1 / 2) * 0.38) };
      ctx.fillStyle = colEl;
      ctx.fillText("θ₁", t1.x - 14, t1.y);

      ctx.strokeStyle = colEl;
      ctx.beginPath();
      ctx.arc(cx, cy, 28, -Math.PI / 2, -Math.PI / 2 - rad1, true);
      ctx.stroke();

      if (!s.tir && Number.isFinite(s.theta2)) {
        const t2 = { x: xOf(Math.sin(rad2 / 2) * 0.38), y: yOf(-Math.cos(rad2 / 2) * 0.38) };
        ctx.fillStyle = colN;
        ctx.fillText("θ₂", t2.x + 6, t2.y + 4);
        ctx.strokeStyle = colN;
        ctx.beginPath();
        ctx.arc(cx, cy, 28, Math.PI / 2, Math.PI / 2 + rad2, false);
        ctx.stroke();
      }

      const t = s.playing || s.progress > 1e-6 ? s.progress : 0;
      if (t > 0) {
        const p = along([p0, p1, p2], t);
        ctx.fillStyle = colN;
        ctx.beginPath();
        ctx.arc(p.x, p.y, 5, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "#efece6";
        ctx.beginPath();
        ctx.arc(p.x - 1.4, p.y - 1.4, 1.8, 0, Math.PI * 2);
        ctx.fill();
      }
    };

    const applyPoint = (p: Pt, kind: "theta1" | "theta2") => {
      const g = layoutRefraction(parent.clientWidth, parent.clientHeight);
      const xw = g.xFromPx(p.x);
      const yw = g.yFromPx(p.y);
      const deg = clamp(angleFromPoint(xw, yw), 0, 90);
      if (kind === "theta1") state.current.onPlaceTheta1(deg);
      else state.current.onPlaceTheta2(deg);
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutRefraction(parent.clientWidth, parent.clientHeight);
      const yw = g.yFromPx(p.y);
      if (Math.abs(yw) < 0.02) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      drag.current = yw > 0 ? "theta1" : "theta2";
      applyPoint(p, drag.current);
    };
    const onMove = (e: PointerEvent) => {
      if (!drag.current || !state.current.dragEnabled) return;
      applyPoint(pointOnCanvas(e), drag.current);
    };
    const onUp = (e: PointerEvent) => {
      if (!drag.current) return;
      drag.current = null;
      try {
        canvas.releasePointerCapture(e.pointerId);
      } catch {
        /* already released */
      }
    };

    drawRef.current = draw;
    draw();
    canvas.addEventListener("pointerdown", onDown);
    canvas.addEventListener("pointermove", onMove);
    canvas.addEventListener("pointerup", onUp);
    canvas.addEventListener("pointercancel", onUp);
    const observer = new ResizeObserver(draw);
    observer.observe(parent);
    return () => {
      observer.disconnect();
      canvas.removeEventListener("pointerdown", onDown);
      canvas.removeEventListener("pointermove", onMove);
      canvas.removeEventListener("pointerup", onUp);
      canvas.removeEventListener("pointercancel", onUp);
    };
  }, []);

  return (
    <canvas
      ref={ref}
      className="lab-canvas"
      style={{
        cursor: dragEnabled ? "grab" : "default",
        touchAction: "none",
      }}
    />
  );
}
