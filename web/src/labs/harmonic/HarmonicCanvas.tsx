import { useEffect, useRef } from "react";
import { arrow, accent, colEl, colN, fitCanvas, forceArrowLen, ink, line, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  x: number;
  v: number;
  a: number;
  amplitude: number;
  dragEnabled: boolean;
  onPlace: (x: number) => void;
};

function layoutHarmonic(w: number, h: number, amplitude: number) {
  const left = 48;
  const right = w - 36;
  const cy = h * 0.55;
  const A = Math.max(amplitude, 0.05);
  const span = (right - left) / 2;
  const ppm = (span * 0.72) / A;
  const xMid = (left + right) / 2;
  return { left, right, cy, A, ppm, xMid };
}

export function HarmonicCanvas({ x, v, a, amplitude, dragEnabled, onPlace }: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ x, v, a, amplitude, dragEnabled, onPlace });
  const drawRef = useRef<() => void>(() => undefined);
  const drag = useRef<{ xMid: number; ppm: number } | null>(null);

  useEffect(() => {
    state.current = { x, v, a, amplitude, dragEnabled, onPlace };
    drawRef.current();
  }, [x, v, a, amplitude, dragEnabled, onPlace]);

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
      const g = layoutHarmonic(w, h, s.amplitude);
      const { left, right, cy, ppm, xMid } = g;
      const px = xMid + s.x * ppm;
      const r = 22;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(left, cy);
      ctx.lineTo(right, cy);
      ctx.stroke();

      ctx.setLineDash([5, 4]);
      ctx.strokeStyle = line;
      ctx.beginPath();
      ctx.moveTo(xMid, cy - 36);
      ctx.lineTo(xMid, cy + 36);
      ctx.stroke();
      const aPx = xMid + s.amplitude * ppm;
      const aMx = xMid - s.amplitude * ppm;
      ctx.beginPath();
      ctx.moveTo(aPx, cy - 10);
      ctx.lineTo(aPx, cy + 10);
      ctx.moveTo(aMx, cy - 10);
      ctx.lineTo(aMx, cy + 10);
      ctx.stroke();
      ctx.setLineDash([]);

      ctx.fillStyle = mute;
      ctx.fillText("0", xMid - 4, cy + 50);
      ctx.fillText("A", aPx - 4, cy + 50);
      ctx.fillText("x", right - 8, cy + 18);

      ctx.fillStyle = accent;
      ctx.beginPath();
      ctx.arc(px, cy, r, 0, Math.PI * 2);
      ctx.fill();

      const lenA = forceArrowLen(Math.abs(s.a), 10, 70);
      if (lenA > 1) {
        const dir = s.a >= 0 ? 1 : -1;
        ctx.strokeStyle = colEl;
        ctx.fillStyle = colEl;
        arrow(ctx, px, cy, px + dir * lenA, cy);
        ctx.fillText("a", px + dir * lenA + (dir > 0 ? 8 : -16), cy - 12);
      }
      const lenV = forceArrowLen(Math.abs(s.v), 22, 70);
      if (lenV > 1) {
        const dir = s.v >= 0 ? 1 : -1;
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, px, cy + 18, px + dir * lenV, cy + 18);
        ctx.fillText("v", px + dir * lenV + (dir > 0 ? 8 : -16), cy + 32);
      }

      ctx.fillStyle = mute;
      ctx.fillText(`x ${s.x.toFixed(2)}`, px - 16, cy - r - 8);
    };

    const apply = (px: number, xMid: number, ppm: number) => {
      state.current.onPlace(clamp((px - xMid) / ppm, -10, 10));
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutHarmonic(parent.clientWidth, parent.clientHeight, state.current.amplitude);
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      drag.current = { xMid: g.xMid, ppm: g.ppm };
      apply(p.x, g.xMid, g.ppm);
    };
    const onMove = (e: PointerEvent) => {
      if (!drag.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      apply(p.x, drag.current.xMid, drag.current.ppm);
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
