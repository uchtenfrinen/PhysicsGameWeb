import { useEffect, useRef } from "react";
import { arrow, accent, colEl, colFg, colN, fitCanvas, ink, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  d: number;
  f: number;
  objectHeight: number;
  dPrime: number;
  imageH: number;
  real: boolean;
  progress: number;
  playing: boolean;
  dragEnabled: boolean;
  onPlaceObject: (d: number, h: number) => void;
  onPlaceF: (f: number) => void;
};

type Pt = { x: number; y: number };

type Drag =
  | { kind: "object"; cx: number; cy: number; scale: number }
  | { kind: "f"; cx: number; scale: number };

function layoutLens(
  w: number,
  h: number,
  d: number,
  f: number,
  dPrime: number,
  objectHeight: number,
  imageH: number,
) {
  const margin = 28;
  const plotW = w - 2 * margin;
  const plotH = h - 2 * margin;
  const cx = margin + plotW / 2;
  const cy = margin + plotH / 2;
  const finite = Number.isFinite(dPrime);
  const dP = finite ? dPrime : f;
  const xLeft = Math.min(-d, dP < 0 ? dP : -d) - f * 0.5;
  const xRight = Math.max(dP > 0 ? dP : f, f) + f * 1.2;
  const yExtent = Math.max(objectHeight, finite ? imageH : objectHeight, 2) + 2;
  const scale = Math.min(plotW / Math.max(xRight - xLeft, 1), plotH / (2 * yExtent));
  const xOf = (xw: number) => cx + xw * scale;
  const yOf = (yw: number) => cy - yw * scale;
  const xFromPx = (px: number) => (px - cx) / scale;
  const yFromPx = (py: number) => (cy - py) / scale;
  return { cx, cy, scale, xOf, yOf, xFromPx, yFromPx, xLeft, xRight, finite };
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

function dist(a: Pt, b: Pt) {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

function strokeRay(ctx: CanvasRenderingContext2D, ray: Pt[]) {
  ctx.beginPath();
  ray.forEach((p, i) => (i === 0 ? ctx.moveTo(p.x, p.y) : ctx.lineTo(p.x, p.y)));
  ctx.stroke();
}

function drawBall(ctx: CanvasRenderingContext2D, p: Pt) {
  ctx.fillStyle = colN;
  ctx.beginPath();
  ctx.arc(p.x, p.y, 5, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = "#efece6";
  ctx.beginPath();
  ctx.arc(p.x - 1.4, p.y - 1.4, 1.8, 0, Math.PI * 2);
  ctx.fill();
}

export function LensCanvas({
  d,
  f,
  objectHeight,
  dPrime,
  imageH,
  real,
  progress,
  playing,
  dragEnabled,
  onPlaceObject,
  onPlaceF,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({
    d,
    f,
    objectHeight,
    dPrime,
    imageH,
    real,
    progress,
    playing,
    dragEnabled,
    onPlaceObject,
    onPlaceF,
  });
  const drawRef = useRef<() => void>(() => undefined);
  const drag = useRef<Drag | null>(null);

  useEffect(() => {
    state.current = {
      d,
      f,
      objectHeight,
      dPrime,
      imageH,
      real,
      progress,
      playing,
      dragEnabled,
      onPlaceObject,
      onPlaceF,
    };
    drawRef.current();
  }, [d, f, objectHeight, dPrime, imageH, real, progress, playing, dragEnabled, onPlaceObject, onPlaceF]);

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
      const s = state.current;
      return layoutLens(
        parent.clientWidth,
        parent.clientHeight,
        s.d,
        s.f,
        s.dPrime,
        s.objectHeight,
        s.imageH,
      );
    };

    const draw = () => {
      const sized = fitCanvas(canvas, parent);
      if (!sized) return;
      const { ctx, w, h } = sized;
      const s = state.current;
      const g = layoutLens(w, h, s.d, s.f, s.dPrime, s.objectHeight, s.imageH);
      const { xOf, yOf, cy } = g;
      const lensX = xOf(0);
      const half = h / 2 - 34;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(xOf(g.xLeft), cy);
      ctx.lineTo(xOf(g.xRight), cy);
      ctx.stroke();

      ctx.strokeStyle = accent;
      ctx.beginPath();
      ctx.moveTo(lensX, cy - half);
      ctx.quadraticCurveTo(lensX + 18, cy, lensX, cy + half);
      ctx.quadraticCurveTo(lensX - 18, cy, lensX, cy - half);
      ctx.stroke();

      ctx.fillStyle = mute;
      ctx.beginPath();
      ctx.arc(xOf(-s.f), cy, 4, 0, Math.PI * 2);
      ctx.arc(xOf(s.f), cy, 4, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillText("F", xOf(-s.f) - 14, cy - 12);
      ctx.fillText("F'", xOf(s.f) + 8, cy - 12);
      ctx.fillText("O", lensX + 8, cy + 18);

      const objX = xOf(-s.d);
      ctx.strokeStyle = colEl;
      ctx.fillStyle = colEl;
      arrow(ctx, objX, cy, objX, yOf(s.objectHeight));
      ctx.fillText("h", objX - 16, yOf(s.objectHeight / 2));
      ctx.fillText("d", (objX + lensX) / 2 - 6, cy + 16);

      if (!g.finite) {
        ctx.fillStyle = mute;
        ctx.fillText("d = f: изображение в бесконечности", 24, 22);
        const sx0 = objX;
        const sy0 = yOf(s.objectHeight);
        ctx.strokeStyle = accent;
        strokeRay(ctx, [
          { x: sx0, y: sy0 },
          { x: lensX, y: sy0 },
          { x: xOf(g.xRight), y: yOf(s.objectHeight * (1 - g.xRight / s.f)) },
        ]);
        strokeRay(ctx, [
          { x: sx0, y: sy0 },
          { x: xOf(g.xRight), y: yOf((-s.objectHeight * g.xRight) / s.d) },
        ]);
        return;
      }

      const imgX = xOf(s.dPrime);
      const imgTip = s.real ? -s.imageH : s.imageH;
      ctx.strokeStyle = colFg;
      ctx.fillStyle = colFg;
      arrow(ctx, imgX, cy, imgX, yOf(imgTip));
      ctx.fillText("d'", (lensX + imgX) / 2 - 6, cy + 16);

      const oy = s.objectHeight;
      const sx0 = xOf(-s.d);
      const sy0 = yOf(oy);
      const imageTipY = (-oy * s.dPrime) / s.d;
      const yAtLens3 = (-oy * s.f) / (s.d - s.f);

      const ray1: Pt[] = [
        { x: sx0, y: sy0 },
        { x: lensX, y: sy0 },
        { x: xOf(s.dPrime), y: yOf(oy * (1 - s.dPrime / s.f)) },
      ];
      const ray2: Pt[] = [
        { x: sx0, y: sy0 },
        { x: lensX, y: cy },
        { x: xOf(s.dPrime), y: yOf(imageTipY) },
      ];
      const ray3: Pt[] = [
        { x: sx0, y: sy0 },
        { x: xOf(-s.f), y: cy },
        { x: lensX, y: yOf(yAtLens3) },
        { x: xOf(s.dPrime), y: yOf(yAtLens3) },
      ];

      ctx.strokeStyle = accent;
      for (const ray of [ray1, ray2, ray3]) strokeRay(ctx, ray);

      const t = s.playing || s.progress > 1e-6 ? s.progress : 0;
      if (t > 0) {
        for (const ray of [ray1, ray2, ray3]) drawBall(ctx, along(ray, t));
      }
    };

    const hitKind = (p: Pt): "object" | "f" | null => {
      const s = state.current;
      const g = layoutNow();
      const fL = { x: g.xOf(-s.f), y: g.cy };
      const fR = { x: g.xOf(s.f), y: g.cy };
      const obj = { x: g.xOf(-s.d), y: g.yOf(s.objectHeight) };
      const nearF = dist(p, fL) < 16 || dist(p, fR) < 16;
      const nearObj =
        Math.abs(p.x - obj.x) < 18 && p.y <= g.cy + 10 && p.y >= obj.y - 18;
      if (nearObj) return "object";
      if (nearF) return "f";
      if (g.xFromPx(p.x) < -0.4) return "object";
      return null;
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const kind = hitKind(p);
      if (!kind) return;
      const g = layoutNow();
      const s = state.current;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      if (kind === "f") {
        drag.current = { kind: "f", cx: g.cx, scale: g.scale };
        s.onPlaceF(clamp(Math.abs((p.x - g.cx) / g.scale), 0.1, 200));
        return;
      }
      drag.current = { kind: "object", cx: g.cx, cy: g.cy, scale: g.scale };
      s.onPlaceObject(
        clamp(-(p.x - g.cx) / g.scale, 0.1, 200),
        clamp(Math.abs((g.cy - p.y) / g.scale), 0.1, 50),
      );
    };
    const onMove = (e: PointerEvent) => {
      if (!drag.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const dnd = drag.current;
      if (dnd.kind === "f") {
        state.current.onPlaceF(clamp(Math.abs((p.x - dnd.cx) / dnd.scale), 0.1, 200));
        return;
      }
      state.current.onPlaceObject(
        clamp(-(p.x - dnd.cx) / dnd.scale, 0.1, 200),
        clamp(Math.abs((dnd.cy - p.y) / dnd.scale), 0.1, 50),
      );
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
