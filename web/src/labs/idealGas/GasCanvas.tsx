import { useEffect, useRef } from "react";
import { accent, arrow, colN, fitCanvas, forceArrowLen, ink, labBox, line, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  currentV: number;
  vMax: number;
  currentP: number;
  vDot: number;
  dragEnabled: boolean;
  onPlace: (v: number) => void;
};

function layoutGas(w: number, h: number, vMax: number) {
  const box = labBox(w, h);
  const cx = (box.left + box.right) / 2;
  const cylW = 96;
  const pistonH = 12;
  const left = cx - cylW / 2;
  const bottom = box.bottom - 8;
  const top = box.top + 20;
  const vCeil = Math.max(vMax, 0.05);
  const ppm = Math.max(80, (bottom - top - pistonH - 16) / vCeil);
  const vMaxDrag = Math.max(1e-6, (bottom - top - pistonH - 8) / ppm);
  return { box, cx, cylW, pistonH, left, bottom, top, vCeil, ppm, vMaxDrag };
}

function vFromY(my: number, layout: ReturnType<typeof layoutGas>): number {
  const gasH = layout.bottom - layout.pistonH - my;
  return gasH / layout.ppm;
}

export function GasCanvas({ currentV, vMax, currentP, vDot, dragEnabled, onPlace }: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ currentV, vMax, currentP, vDot, dragEnabled, onPlace });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef(false);

  useEffect(() => {
    state.current = { currentV, vMax, currentP, vDot, dragEnabled, onPlace };
    drawRef.current();
  }, [currentV, vMax, currentP, vDot, dragEnabled, onPlace]);

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
      const g = layoutGas(w, h, s.vMax);
      const { box, cx, cylW, pistonH, left, bottom, top, ppm } = g;

      ctx.strokeStyle = line;
      ctx.strokeRect(box.boxX, box.boxY, box.boxW, box.boxH);

      const gasH = Math.max(8, s.currentV * ppm);
      const pistonY = bottom - pistonH - gasH;

      ctx.strokeStyle = ink;
      ctx.strokeRect(left, top, cylW, bottom - top);
      ctx.fillStyle = "rgba(42, 83, 73, 0.22)";
      ctx.fillRect(left + 2, pistonY + pistonH, cylW - 4, bottom - pistonY - pistonH - 2);
      ctx.fillStyle = accent;
      ctx.fillRect(left - 6, pistonY, cylW + 12, pistonH);
      ctx.strokeRect(left - 6, pistonY, cylW + 12, pistonH);

      const py = pistonY + pistonH / 2;
      const lenP = forceArrowLen(s.currentP, 0.00045, 90);
      const lenV = forceArrowLen(Math.abs(s.vDot), 2500, 90);
      if (lenP > 1) {
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, cx - 10, py, cx - 10, py - lenP);
        ctx.fillText("P", cx - 10 + 8, py - lenP - 4);
      }
      if (lenV > 1) {
        const dir = s.vDot >= 0 ? -1 : 1;
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, cx + 10, py, cx + 10, py + dir * lenV);
        ctx.fillText("v", cx + 10 + 8, py + dir * lenV + (dir < 0 ? -4 : 12));
      }

      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText(`V ${s.currentV.toFixed(4)}`, left + cylW + 14, pistonY + 10);
      ctx.fillText(`P ${s.currentP.toFixed(0)}`, left + cylW + 14, pistonY + 26);
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = true;
      const p = pointOnCanvas(e);
      const g = layoutGas(parent.clientWidth, parent.clientHeight, state.current.vMax);
      state.current.onPlace(clamp(vFromY(p.y, g), 1e-6, g.vMaxDrag));
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutGas(parent.clientWidth, parent.clientHeight, state.current.vMax);
      state.current.onPlace(clamp(vFromY(p.y, g), 1e-6, g.vMaxDrag));
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
