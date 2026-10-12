import { useEffect, useRef } from "react";
import { accent, arrow, colN, colTr, fitCanvas, forceArrowLen, ink, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  isSeries: boolean;
  r1: number;
  r2: number;
  r3: number;
  i1: number;
  i2: number;
  i3: number;
  u1: number;
  u2: number;
  u3: number;
  currentA: number;
  phase: number;
  dragEnabled: boolean;
  onPlaceR: (index: number, r: number) => void;
};

type Box = { cx: number; cy: number; rw: number; rh: number };

function layoutSeries(w: number, h: number) {
  const y = h * 0.52;
  const x0 = 48;
  const x1 = w - 40;
  const boxes: Box[] = [0, 1, 2].map((i) => {
    const cx = x0 + 72 + ((x1 - x0 - 96) * (i + 0.5)) / 3;
    return { cx, cy: y, rw: 64, rh: 28 };
  });
  return { y, x0, x1, boxes };
}

function layoutParallel(w: number, h: number) {
  const top = 40;
  const bot = h - 44;
  const left = 56;
  const right = w - 36;
  const span = Math.min(140, (right - left - 70) / 2.2);
  const boxes: Box[] = [0, 1, 2].map((i) => {
    const cx = left + 78 + i * span;
    const cy = (top + bot) / 2;
    return { cx, cy, rw: 28, rh: 64 };
  });
  return { top, bot, left, right, boxes };
}

function inBox(x: number, y: number, b: Box, pad = 6) {
  return x >= b.cx - b.rw / 2 - pad && x <= b.cx + b.rw / 2 + pad && y >= b.cy - b.rh / 2 - pad && y <= b.cy + b.rh / 2 + pad;
}

function drawBall(ctx: CanvasRenderingContext2D, x: number, y: number) {
  ctx.fillStyle = colN;
  ctx.beginPath();
  ctx.arc(x, y, 5, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = "#efece6";
  ctx.beginPath();
  ctx.arc(x - 1.4, y - 1.4, 1.8, 0, Math.PI * 2);
  ctx.fill();
}

function drawResistor(ctx: CanvasRenderingContext2D, b: Box, vertical: boolean) {
  ctx.fillStyle = "#f7f4ee";
  ctx.fillRect(b.cx - b.rw / 2, b.cy - b.rh / 2, b.rw, b.rh);
  ctx.strokeStyle = accent;
  ctx.strokeRect(b.cx - b.rw / 2, b.cy - b.rh / 2, b.rw, b.rh);
  ctx.beginPath();
  if (vertical) {
    const zT = b.cy - b.rh / 2 + 8;
    const zB = b.cy + b.rh / 2 - 8;
    ctx.moveTo(b.cx, zT);
    for (let i = 0; i <= 5; i++) {
      ctx.lineTo(b.cx + (i % 2 === 0 ? -7 : 7), zT + ((zB - zT) * i) / 5);
    }
  } else {
    const zL = b.cx - b.rw / 2 + 8;
    const zR = b.cx + b.rw / 2 - 8;
    ctx.moveTo(zL, b.cy);
    for (let i = 0; i <= 5; i++) {
      ctx.lineTo(zL + ((zR - zL) * i) / 5, b.cy + (i % 2 === 0 ? -7 : 7));
    }
  }
  ctx.stroke();
}

export function ResistorsCanvas({
  isSeries,
  r1,
  r2,
  r3,
  i1,
  i2,
  i3,
  u1,
  u2,
  u3,
  currentA,
  phase,
  dragEnabled,
  onPlaceR,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({
    isSeries,
    r1,
    r2,
    r3,
    i1,
    i2,
    i3,
    u1,
    u2,
    u3,
    currentA,
    phase,
    dragEnabled,
    onPlaceR,
  });
  const drawRef = useRef<() => void>(() => undefined);
  const drag = useRef<{ index: number; startY: number; startVal: number } | null>(null);

  useEffect(() => {
    state.current = {
      isSeries,
      r1,
      r2,
      r3,
      i1,
      i2,
      i3,
      u1,
      u2,
      u3,
      currentA,
      phase,
      dragEnabled,
      onPlaceR,
    };
    drawRef.current();
  }, [isSeries, r1, r2, r3, i1, i2, i3, u1, u2, u3, currentA, phase, dragEnabled, onPlaceR]);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;
    const parent = canvas.parentElement;
    if (!parent) return;

    const pointOnCanvas = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    };

    const boxesNow = () => {
      const s = state.current;
      return s.isSeries
        ? layoutSeries(parent.clientWidth, parent.clientHeight).boxes
        : layoutParallel(parent.clientWidth, parent.clientHeight).boxes;
    };

    const draw = () => {
      const sized = fitCanvas(canvas, parent);
      if (!sized) return;
      const { ctx, w, h } = sized;
      const s = state.current;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      const ohms = [s.r1, s.r2, s.r3];
      const volts = [s.u1, s.u2, s.u3];
      const currents = [s.i1, s.i2, s.i3];

      if (s.isSeries) {
        const g = layoutSeries(w, h);
        ctx.strokeStyle = ink;
        ctx.lineWidth = 1.6;
        ctx.beginPath();
        ctx.moveTo(g.x0, g.y);
        ctx.lineTo(g.x1, g.y);
        ctx.stroke();
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(g.x0, g.y - 12);
        ctx.lineTo(g.x0, g.y + 12);
        ctx.moveTo(g.x0 + 8, g.y - 7);
        ctx.lineTo(g.x0 + 8, g.y + 7);
        ctx.stroke();
        ctx.fillStyle = mute;
        ctx.fillText("U", g.x0 - 4, g.y - 20);

        for (const b of g.boxes) drawResistor(ctx, b, false);

        const n = 18;
        for (let i = 0; i < n; i++) {
          const t = (((s.phase + i / n) % 1) + 1) % 1;
          const x = g.x0 + 18 + t * (g.x1 - g.x0 - 36);
          if (g.boxes.some((b) => inBox(x, g.y, b))) continue;
          drawBall(ctx, x, g.y);
        }

        g.boxes.forEach((b, i) => {
          ctx.fillStyle = mute;
          ctx.fillText(`R${i + 1}`, b.cx - 10, b.cy - b.rh / 2 - 8);
          ctx.fillText(`${ohms[i].toFixed(1)} Ω`, b.cx - 16, b.cy + b.rh / 2 + 16);
          const lenU = forceArrowLen(volts[i], 2.4, 40);
          if (lenU > 1) {
            ctx.strokeStyle = colTr;
            ctx.fillStyle = colTr;
            arrow(ctx, b.cx, b.cy - b.rh / 2 - 22, b.cx, b.cy - b.rh / 2 - 22 - lenU);
            ctx.fillText(`U${i + 1}`, b.cx + 8, b.cy - b.rh / 2 - 22 - lenU - 2);
          }
        });
        const lenI = forceArrowLen(s.currentA, 36, 56);
        if (lenI > 1) {
          ctx.strokeStyle = colN;
          ctx.fillStyle = colN;
          arrow(ctx, g.x0 + 22, g.y + 28, g.x0 + 22 + lenI, g.y + 28);
          ctx.fillText("I", g.x0 + 22 + lenI + 6, g.y + 24);
        }
        return;
      }

      const g = layoutParallel(w, h);
      ctx.strokeStyle = ink;
      ctx.lineWidth = 1.6;
      ctx.beginPath();
      ctx.moveTo(g.left, g.top);
      ctx.lineTo(g.right, g.top);
      ctx.moveTo(g.left, g.bot);
      ctx.lineTo(g.right, g.bot);
      ctx.moveTo(g.left, g.top);
      ctx.lineTo(g.left, g.bot);
      for (const b of g.boxes) {
        ctx.moveTo(b.cx, g.top);
        ctx.lineTo(b.cx, g.bot);
      }
      ctx.stroke();
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(g.left - 8, g.top + 14);
      ctx.lineTo(g.left + 8, g.top + 14);
      ctx.moveTo(g.left - 5, g.bot - 14);
      ctx.lineTo(g.left + 5, g.bot - 14);
      ctx.stroke();
      ctx.fillStyle = mute;
      ctx.fillText("U", g.left - 24, (g.top + g.bot) / 2);

      for (const b of g.boxes) drawResistor(ctx, b, true);

      for (const b of g.boxes) {
        const n = 8;
        for (let k = 0; k < n; k++) {
          const t = (((s.phase + k / n) % 1) + 1) % 1;
          const y = g.top + 10 + t * (g.bot - g.top - 20);
          if (inBox(b.cx, y, b)) continue;
          drawBall(ctx, b.cx, y);
        }
      }

      g.boxes.forEach((b, i) => {
        ctx.fillStyle = mute;
        ctx.fillText(`R${i + 1}`, b.cx + 20, b.cy - 10);
        ctx.fillText(`${ohms[i].toFixed(1)} Ω`, b.cx + 20, b.cy + 8);
        const lenI = forceArrowLen(currents[i], 28, 44);
        if (lenI > 1) {
          ctx.strokeStyle = colN;
          ctx.fillStyle = colN;
          arrow(ctx, b.cx - 22, b.cy - lenI / 2, b.cx - 22, b.cy + lenI / 2);
          ctx.fillText(`I${i + 1}`, b.cx - 48, b.cy + 4);
        }
      });
      const lenU = forceArrowLen(s.u1, 1.8, 48);
      if (lenU > 1) {
        ctx.strokeStyle = colTr;
        ctx.fillStyle = colTr;
        arrow(ctx, g.left + 18, (g.top + g.bot) / 2 + 20, g.left + 18, (g.top + g.bot) / 2 + 20 - lenU);
        ctx.fillText("U", g.left + 26, (g.top + g.bot) / 2 + 16 - lenU);
      }
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const boxes = boxesNow();
      const index = boxes.findIndex((b) => inBox(p.x, p.y, b, 10));
      if (index < 0) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      const vals = [state.current.r1, state.current.r2, state.current.r3];
      drag.current = { index, startY: p.y, startVal: vals[index] };
    };
    const onMove = (e: PointerEvent) => {
      if (!drag.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const dy = drag.current.startY - p.y;
      state.current.onPlaceR(drag.current.index, clamp(drag.current.startVal + dy * 0.35, 0.1, 100));
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
