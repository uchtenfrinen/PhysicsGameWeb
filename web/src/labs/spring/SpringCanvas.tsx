import { useEffect, useRef } from "react";
import { arrow, accent, colEl, colFg, colN, fitCanvas, forceArrowLen, ink, labBox, line, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";
import { G } from "../../physics/forceCalculator.ts";

const CUBE = 78;

type Props = {
  x: number;
  v: number;
  k: number;
  m: number;
  dragEnabled: boolean;
  onPlace: (x: number) => void;
};

function layoutSpring(w: number, h: number, x: number) {
  const box = labBox(w, h);
  const cx = (box.left + box.right) / 2;
  const plotH = box.bottom - box.top;
  const ppm = Math.max(80, plotH * 0.55);
  const barY = box.top + 10;
  const springTop = barY + 16;
  const eqY = springTop + plotH * 0.38;
  const cubeTop = eqY + x * ppm;
  const cubeCenter = cubeTop + CUBE / 2;
  return { box, cx, ppm, barY, springTop, eqY, cubeTop, cubeCenter };
}

function xFromY(my: number, layout: ReturnType<typeof layoutSpring>): number {
  return (my - layout.eqY - CUBE / 2) / layout.ppm;
}

export function SpringCanvas({ x, v, k, m, dragEnabled, onPlace }: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ x, v, k, m, dragEnabled, onPlace });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef(false);

  useEffect(() => {
    state.current = { x, v, k, m, dragEnabled, onPlace };
    drawRef.current();
  }, [x, v, k, m, dragEnabled, onPlace]);

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
      const g = layoutSpring(w, h, s.x);
      const { box, cx, barY, springTop, eqY, cubeTop, cubeCenter } = g;
      const springBottom = cubeTop;
      const springLen = Math.max(28, springBottom - springTop);
      const amp = Math.min(14, springLen / 7);
      const coils = 12;

      ctx.strokeStyle = line;
      ctx.strokeRect(box.boxX, box.boxY, box.boxW, box.boxH);

      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText("x, м", box.left, box.boxY + 16);

      ctx.fillStyle = ink;
      ctx.fillRect(cx - 48, barY, 96, 8);

      ctx.strokeStyle = accent;
      ctx.beginPath();
      ctx.moveTo(cx, springTop);
      for (let i = 1; i < coils; i++) {
        const t = i / coils;
        ctx.lineTo(cx + (i % 2 === 0 ? amp : -amp), springTop + springLen * t);
      }
      ctx.lineTo(cx, springBottom);
      ctx.stroke();

      ctx.setLineDash([5, 4]);
      ctx.strokeStyle = line;
      ctx.beginPath();
      ctx.moveTo(cx - 56, eqY);
      ctx.lineTo(cx + 56, eqY);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = mute;
      ctx.fillText("равновесие", cx - 56 - ctx.measureText("равновесие").width - 6, eqY + 3);

      ctx.fillStyle = accent;
      ctx.fillRect(cx - CUBE / 2, cubeTop, CUBE, CUBE);
      ctx.fillStyle = "#efece6";
      ctx.font = "12px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText("m", cx - 5, cubeCenter + 4);

      const fg = s.m * G;
      const fel = Math.abs(s.k * s.x);
      const lenFg = forceArrowLen(fg, 3.8, 90);
      const lenEl = forceArrowLen(fel, 3.8, 90);
      const lenV = forceArrowLen(Math.abs(s.v), 18, 90);
      if (lenEl > 1) {
        const dir = s.x >= 0 ? -1 : 1;
        ctx.strokeStyle = colEl;
        ctx.fillStyle = colEl;
        arrow(ctx, cx, cubeCenter, cx, cubeCenter + dir * lenEl);
        ctx.fillText("Fупр", cx + 8, cubeCenter + dir * lenEl + (dir < 0 ? -4 : 12));
      }
      if (lenFg > 1) {
        ctx.strokeStyle = colFg;
        ctx.fillStyle = colFg;
        arrow(ctx, cx, cubeCenter, cx, cubeCenter + lenFg);
        ctx.fillText("Fg", cx + 8, cubeCenter + lenFg * 0.7);
      }
      if (lenV > 1) {
        const dir = s.v >= 0 ? 1 : -1;
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, cx, cubeCenter, cx, cubeCenter + dir * lenV);
        ctx.fillText("v", cx - 18, cubeCenter + dir * lenV + (dir < 0 ? -4 : 12));
      }

      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText(`x ${s.x.toFixed(2)}`, cx + CUBE / 2 + 8, cubeTop + CUBE + 14);
      const omega = Math.sqrt(s.k / s.m);
      ctx.fillText(`ω = ${omega.toFixed(2)}`, box.left, box.bottom - 4);
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = true;
      const p = pointOnCanvas(e);
      const g = layoutSpring(parent.clientWidth, parent.clientHeight, state.current.x);
      state.current.onPlace(clamp(xFromY(p.y, g), -2, 2));
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutSpring(parent.clientWidth, parent.clientHeight, state.current.x);
      state.current.onPlace(clamp(xFromY(p.y, g), -2, 2));
    };
    const onUp = (e: PointerEvent) => {
      if (!dragging.current) return;
      dragging.current = false;
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
