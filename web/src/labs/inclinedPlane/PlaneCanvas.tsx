import { useEffect, useRef } from "react";
import { clamp } from "../../physics/clamp.ts";
import type { ForceResult } from "../../physics/forceCalculator.ts";
import { planeUMax, planeUMin } from "../../physics/inclinedPlaneMotion.ts";
import { arrow, accent, fitCanvas, colFg, colN, colTr, forceArrowLen, ink, mute } from "../draw.ts";

const WOOD = "#b0895a";
const WOOD_DARK = "#8a6840";
const WOOD_GRAIN = "#c9a36e";

type Props = {
  angleDegrees: number;
  positionAlongPlane: number;
  forces: ForceResult;
  dragEnabled: boolean;
  onPlace: (u: number) => void;
};

function layoutPlane(w: number, h: number, alpha: number) {
  const rad = (alpha * Math.PI) / 180;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  const left = 48;
  const rightPad = 20;
  const top = 10;
  const plotW = w - left - rightPad;
  let L = plotW / Math.max(cos, 0.2);
  let plank = Math.max(16, L * 0.055);
  const needH = L * sin + plank + 44;
  const availH = h - top - 12;
  if (needH > availH && availH > 40) {
    L *= availH / needH;
    plank = Math.max(16, L * 0.055);
  }
  const ox = left;
  const oy = top + 36 + L * sin;
  const alongX = cos;
  const alongY = -sin;
  const nX = -sin;
  const nY = -cos;
  const wx = (u: number, v: number) => ox + (u * alongX + v * nX) * L;
  const wy = (u: number, v: number) => oy + (u * alongY + v * nY) * L;
  return { wx, wy, ox, oy, L, alongX, alongY, nX, nY, plank, cos, sin, rad };
}

function projectU(mx: number, my: number, layout: ReturnType<typeof layoutPlane>): number {
  const x0 = layout.wx(0, 0);
  const y0 = layout.wy(0, 0);
  const x1 = layout.wx(1, 0);
  const y1 = layout.wy(1, 0);
  const dx = x1 - x0;
  const dy = y1 - y0;
  const len2 = dx * dx + dy * dy;
  if (len2 < 1e-10) return 0.5;
  const t = ((mx - x0) * dx + (my - y0) * dy) / len2;
  return clamp(t, planeUMin, planeUMax);
}

export function PlaneCanvas({
  angleDegrees,
  positionAlongPlane,
  forces,
  dragEnabled,
  onPlace,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ angleDegrees, positionAlongPlane, forces, dragEnabled, onPlace });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef(false);

  useEffect(() => {
    state.current = { angleDegrees, positionAlongPlane, forces, dragEnabled, onPlace };
    drawRef.current();
  }, [angleDegrees, positionAlongPlane, forces, dragEnabled, onPlace]);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;
    const parent = canvas.parentElement;
    if (!parent) return;

    const pointOnCanvas = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    };

    const layoutNow = () => {
      const w = parent.clientWidth;
      const h = parent.clientHeight;
      return layoutPlane(w, h, state.current.angleDegrees);
    };

    const draw = () => {
      const sized = fitCanvas(canvas, parent);
      if (!sized) return;
      const { ctx, w, h } = sized;
      const { angleDegrees: alpha, positionAlongPlane: t, forces: f } = state.current;
      const g = layoutPlane(w, h, alpha);
      const { wx, wy, ox, oy, L, alongX, alongY, nX, nY, plank, rad } = g;

      ctx.beginPath();
      ctx.moveTo(wx(0, 0), wy(0, 0));
      ctx.lineTo(wx(1, 0), wy(1, 0));
      ctx.lineTo(wx(1, -plank / L), wy(1, -plank / L));
      ctx.lineTo(wx(0, -plank / L), wy(0, -plank / L));
      ctx.closePath();
      ctx.fillStyle = WOOD;
      ctx.fill();
      ctx.strokeStyle = WOOD_DARK;
      ctx.stroke();

      ctx.save();
      ctx.clip();
      ctx.strokeStyle = WOOD_GRAIN;
      ctx.lineWidth = 1.2;
      for (let i = 1; i <= 7; i++) {
        const v = -((plank / L) * i) / 8;
        ctx.beginPath();
        ctx.moveTo(wx(0.02, v), wy(0.02, v));
        ctx.lineTo(wx(0.98, v), wy(0.98, v));
        ctx.stroke();
      }
      ctx.restore();
      ctx.lineWidth = 1;

      ctx.strokeStyle = mute;
      ctx.beginPath();
      ctx.moveTo(ox, oy);
      ctx.lineTo(ox + Math.min(w - 68, L * 1.08), oy);
      ctx.stroke();

      ctx.beginPath();
      ctx.arc(ox, oy, 32, -rad, 0);
      ctx.stroke();
      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText(`α ${alpha.toFixed(0)}°`, ox + 38, oy - 8);

      const hw = 0.07;
      const hh = 0.045;
      const corners = [
        [t - hw, 0],
        [t + hw, 0],
        [t + hw, hh],
        [t - hw, hh],
      ] as const;
      ctx.beginPath();
      corners.forEach(([u, v], i) => {
        const x = wx(u, v);
        const y = wy(u, v);
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      });
      ctx.closePath();
      ctx.fillStyle = accent;
      ctx.fill();
      ctx.strokeStyle = ink;
      ctx.stroke();

      const cx = wx(t, hh / 2);
      const cy = wy(t, hh / 2);
      const pxPerN = 0.011 * L;
      const maxPx = 0.2 * L;

      ctx.strokeStyle = colFg;
      ctx.fillStyle = colFg;
      const lenW = forceArrowLen(f.weightN, pxPerN, maxPx);
      if (lenW > 1) {
        arrow(ctx, cx, cy, cx, cy + lenW);
        ctx.fillText("Fg", cx + 8, cy + lenW * 0.55);
      }
      ctx.strokeStyle = colN;
      ctx.fillStyle = colN;
      const lenN = forceArrowLen(f.normalN, pxPerN, maxPx);
      if (lenN > 1) {
        arrow(ctx, cx, cy, cx + nX * lenN, cy + nY * lenN);
        ctx.fillText("N", cx + nX * lenN - 14, cy + nY * lenN);
      }
      ctx.strokeStyle = colTr;
      ctx.fillStyle = colTr;
      const lenTr = forceArrowLen(f.frictionN, pxPerN, maxPx);
      if (lenTr > 1) {
        arrow(ctx, cx, cy, cx + alongX * lenTr, cy + alongY * lenTr);
        ctx.fillText("Fтр", cx + alongX * lenTr + 4, cy + alongY * lenTr);
      }
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = true;
      const p = pointOnCanvas(e);
      state.current.onPlace(projectU(p.x, p.y, layoutNow()));
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      state.current.onPlace(projectU(p.x, p.y, layoutNow()));
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
