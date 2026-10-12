import { useEffect, useRef } from "react";
import { arrow, accent, colFa, colFg, colN, fitCanvas, forceArrowLen, ink, labBox, line, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  currentFrac: number;
  volume: number;
  fa: number;
  weightN: number;
  v: number;
  dragEnabled: boolean;
  onPlace: (frac: number) => void;
};

function layoutBuoyancy(w: number, h: number, volume: number, currentFrac: number) {
  const box = labBox(w, h);
  const tankL = box.left + 8;
  const tankR = box.right - 8;
  const tankT = box.top + 8;
  const tankB = box.bottom;
  const waterY = tankT + (tankB - tankT) * 0.48;
  const scale = Math.cbrt(Math.max(volume, 1e-6) / 0.001);
  const bw = Math.min(180, Math.max(72, 108 * scale));
  const bh = Math.min(220, Math.max(88, 128 * scale));
  const cx = (tankL + tankR) / 2;
  const frac = Math.min(1, Math.max(0, currentFrac));
  let topY = waterY - bh * (1 - frac);
  if (frac >= 0.999) topY = tankB - bh - 4;
  topY = Math.min(tankB - bh - 2, Math.max(tankT + 4, topY));
  return { box, tankL, tankR, tankT, tankB, waterY, bw, bh, cx, topY, frac };
}

function fracFromY(my: number, layout: ReturnType<typeof layoutBuoyancy>): number {
  const bodyBottom = clamp(my + layout.bh / 2, layout.waterY, layout.tankB - 2);
  return clamp((bodyBottom - layout.waterY) / layout.bh, 0, 1);
}

export function BuoyancyCanvas({ currentFrac, volume, fa, weightN, v, dragEnabled, onPlace }: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ currentFrac, volume, fa, weightN, v, dragEnabled, onPlace });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef(false);

  useEffect(() => {
    state.current = { currentFrac, volume, fa, weightN, v, dragEnabled, onPlace };
    drawRef.current();
  }, [currentFrac, volume, fa, weightN, v, dragEnabled, onPlace]);

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
      const g = layoutBuoyancy(w, h, s.volume, s.currentFrac);
      const { box, tankL, tankR, tankT, tankB, waterY, bw, bh, cx, topY } = g;

      ctx.strokeStyle = line;
      ctx.strokeRect(box.boxX, box.boxY, box.boxW, box.boxH);

      ctx.fillStyle = "rgba(90, 122, 138, 0.35)";
      ctx.fillRect(tankL, waterY, tankR - tankL, tankB - waterY);
      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(tankL, tankT);
      ctx.lineTo(tankL, tankB);
      ctx.lineTo(tankR, tankB);
      ctx.lineTo(tankR, tankT);
      ctx.stroke();
      ctx.strokeStyle = mute;
      ctx.setLineDash([5, 4]);
      ctx.beginPath();
      ctx.moveTo(tankL, waterY);
      ctx.lineTo(tankR, waterY);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText("жидкость", tankL + 8, waterY - 6);

      ctx.fillStyle = accent;
      ctx.fillRect(cx - bw / 2, topY, bw, bh);
      ctx.strokeStyle = ink;
      ctx.strokeRect(cx - bw / 2, topY, bw, bh);

      const midY = topY + bh / 2;
      const lenP = forceArrowLen(s.weightN, 3.8, 90);
      const lenFa = forceArrowLen(s.fa, 3.8, 90);
      const lenV = forceArrowLen(Math.abs(s.v), 500, 90);
      if (lenP > 1) {
        ctx.strokeStyle = colFg;
        ctx.fillStyle = colFg;
        arrow(ctx, cx, midY, cx, midY + lenP);
        ctx.fillText("P", cx + 8, midY + lenP * 0.7);
      }
      if (lenFa > 1) {
        ctx.strokeStyle = colFa;
        ctx.fillStyle = colFa;
        arrow(ctx, cx, midY, cx, midY - lenFa);
        ctx.fillText("F_A", cx + 8, midY - lenFa - 4);
      }
      if (lenV > 1) {
        const dir = s.v >= 0 ? 1 : -1;
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, cx, midY, cx, midY + dir * lenV);
        ctx.fillText("v", cx - 18, midY + dir * lenV + (dir < 0 ? -4 : 12));
      }
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = true;
      const p = pointOnCanvas(e);
      const g = layoutBuoyancy(parent.clientWidth, parent.clientHeight, state.current.volume, state.current.currentFrac);
      state.current.onPlace(fracFromY(p.y, g));
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutBuoyancy(parent.clientWidth, parent.clientHeight, state.current.volume, state.current.currentFrac);
      state.current.onPlace(fracFromY(p.y, g));
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
