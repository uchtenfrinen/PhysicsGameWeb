import { useEffect, useRef } from "react";
import {
  arrow,
  accent,
  colEl,
  colFg,
  colN,
  colTr,
  fitCanvas,
  forceArrowLen,
  ink,
  line,
  mute,
} from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";
import { G } from "../../physics/projectileMotion.ts";

type Props = {
  points: { x: number; y: number }[];
  currentX: number;
  currentY: number;
  range: number;
  maxHeight: number;
  vx: number;
  vy: number;
  dragEnabled: boolean;
  onPlaceY: (y: number) => void;
};

function layoutPlot(w: number, h: number, range: number, maxHeight: number) {
  const left = 48;
  const rightPad = 40;
  const top = 14;
  const bottomPad = 28;
  const plotW = w - left - rightPad;
  const plotH = h - top - bottomPad;
  const maxX = Math.max(range, 1);
  const maxY = Math.max(maxHeight, 0.5);
  const scale = plotW > 0 && plotH > 0 ? Math.min(plotW / maxX, plotH / maxY) : 1;
  const x0 = left;
  const y0 = h - bottomPad;
  return {
    plotW,
    plotH,
    maxX,
    maxY,
    scale,
    x0,
    y0,
    xOf: (x: number) => x0 + x * scale,
    yOf: (y: number) => y0 - y * scale,
    yFromPx: (py: number) => (y0 - py) / scale,
  };
}

export function TrajectoryCanvas({
  points,
  currentX,
  currentY,
  range,
  maxHeight,
  vx,
  vy,
  dragEnabled,
  onPlaceY,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ points, currentX, currentY, range, maxHeight, vx, vy, dragEnabled, onPlaceY });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef(false);

  useEffect(() => {
    state.current = { points, currentX, currentY, range, maxHeight, vx, vy, dragEnabled, onPlaceY };
    drawRef.current();
  }, [points, currentX, currentY, range, maxHeight, vx, vy, dragEnabled, onPlaceY]);

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
      const g = layoutPlot(w, h, s.range, s.maxHeight);
      if (g.plotW < 40 || g.plotH < 40) return;
      const { xOf, yOf, maxX, maxY, x0, y0 } = g;

      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText("y, м", x0, yOf(maxY) - 8);

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(x0, yOf(maxY));
      ctx.lineTo(x0, y0);
      ctx.lineTo(xOf(maxX), y0);
      ctx.stroke();

      ctx.font = "10px Outfit, ui-sans-serif, sans-serif";
      for (let i = 0; i <= 4; i++) {
        const val = (maxX * i) / 4;
        const x = xOf(val);
        ctx.strokeStyle = line;
        ctx.beginPath();
        ctx.moveTo(x, y0);
        ctx.lineTo(x, y0 + 4);
        ctx.stroke();
        ctx.fillStyle = mute;
        if (i === 4) continue;
        const label = val.toFixed(0);
        ctx.fillText(label, x - ctx.measureText(label).width / 2, y0 + 16);
      }
      for (let i = 1; i <= 4; i++) {
        const val = (maxY * i) / 4;
        const y = yOf(val);
        ctx.strokeStyle = line;
        ctx.beginPath();
        ctx.moveTo(x0, y);
        ctx.lineTo(x0 + 5, y);
        ctx.stroke();
        if (i === 4) continue;
        ctx.fillStyle = mute;
        const label = val.toFixed(0);
        ctx.fillText(label, x0 - ctx.measureText(label).width - 6, y + 3);
      }
      ctx.fillStyle = mute;
      ctx.fillText("x, м", xOf(maxX) + 6, y0 + 16);

      if (s.points.length >= 2) {
        ctx.strokeStyle = accent;
        ctx.lineWidth = 1.6;
        ctx.beginPath();
        s.points.forEach((p, i) => {
          if (i === 0) ctx.moveTo(xOf(p.x), yOf(Math.max(0, p.y)));
          else ctx.lineTo(xOf(p.x), yOf(Math.max(0, p.y)));
        });
        ctx.stroke();
        ctx.lineWidth = 1;
      }

      const cx = xOf(s.currentX);
      const cy = yOf(Math.max(0, s.currentY));
      const r = 20;
      ctx.fillStyle = accent;
      ctx.beginPath();
      ctx.arc(cx, cy, r, 0, Math.PI * 2);
      ctx.fill();

      const lenVx = forceArrowLen(Math.abs(s.vx), 4, 110);
      const lenVy = forceArrowLen(Math.abs(s.vy), 4, 110);
      const vxEndX = cx + Math.sign(s.vx || 1) * lenVx * (s.vx === 0 ? 0 : 1);
      const vxEndY = cy;
      const vyEndX = cx;
      const vyEndY = cy - Math.sign(s.vy || 1) * lenVy * (s.vy === 0 ? 0 : 1);
      const vEndX = vxEndX;
      const vEndY = vyEndY;

      ctx.setLineDash([5, 4]);
      ctx.strokeStyle = mute;
      ctx.beginPath();
      ctx.moveTo(vxEndX, vxEndY);
      ctx.lineTo(vEndX, vEndY);
      ctx.moveTo(vyEndX, vyEndY);
      ctx.lineTo(vEndX, vEndY);
      ctx.stroke();
      ctx.setLineDash([]);

      if (lenVx > 1) {
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, cx, cy, vxEndX, vxEndY);
        ctx.fillText("vx", vxEndX + 6, vxEndY + 4);
      }
      if (lenVy > 1) {
        ctx.strokeStyle = colEl;
        ctx.fillStyle = colEl;
        arrow(ctx, cx, cy, vyEndX, vyEndY);
        ctx.fillText("vy", vyEndX + 8, vyEndY + (s.vy >= 0 ? -4 : 12));
      }
      const lenV = forceArrowLen(Math.hypot(s.vx, s.vy), 4, 110);
      if (lenV > 1) {
        ctx.strokeStyle = colTr;
        ctx.fillStyle = colTr;
        arrow(ctx, cx, cy, vEndX, vEndY);
        ctx.fillText("v", vEndX + 6, vEndY + (s.vy >= 0 ? -4 : 12));
      }

      const lenFg = forceArrowLen(G, 3.8, 90);
      if (lenFg > 1) {
        ctx.strokeStyle = colFg;
        ctx.fillStyle = colFg;
        arrow(ctx, cx, cy, cx, cy + lenFg);
        ctx.fillText("Fg", cx + 10, cy + lenFg * 0.7);
      }
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = true;
      const p = pointOnCanvas(e);
      const g = layoutPlot(parent.clientWidth, parent.clientHeight, state.current.range, state.current.maxHeight);
      state.current.onPlaceY(clamp(g.yFromPx(p.y), 0, 200));
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutPlot(parent.clientWidth, parent.clientHeight, state.current.range, state.current.maxHeight);
      state.current.onPlaceY(clamp(g.yFromPx(p.y), 0, 200));
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
