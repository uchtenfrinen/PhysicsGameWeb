import { useEffect, useRef } from "react";
import { accent, arrow, colEl, colN, colTr, fitCanvas, forceArrowLen, ink, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  epsilonV: number;
  internalOhm: number;
  externalOhm: number;
  currentA: number;
  voltageExternal: number;
  voltageInternal: number;
  phase: number;
  dragEnabled: boolean;
  onPlaceEpsilon: (e: number) => void;
  onPlaceR: (r: number) => void;
};

type Pt = { x: number; y: number };

function layoutOhm(w: number, h: number) {
  const xL = 56;
  const xR = w - 56;
  const yT = 64;
  const yB = h - 64;
  const cx = (xL + xR) / 2;
  const batGap = 64;
  const batL = cx - batGap / 2;
  const batR = cx + batGap / 2;
  const resW = 96;
  const resL = cx - resW / 2;
  const resR = cx + resW / 2;
  const battery = { x: batL - 12, y: yT - 32, w: batGap + 24, h: 64 };
  const resistor = { x: resL - 10, y: yB - 26, w: resW + 20, h: 52 };
  const path: Pt[] = [
    { x: xL, y: yT },
    { x: batL, y: yT },
    { x: batR, y: yT },
    { x: xR, y: yT },
    { x: xR, y: yB },
    { x: resR, y: yB },
    { x: resL, y: yB },
    { x: xL, y: yB },
    { x: xL, y: yT },
  ];
  return { xL, xR, yT, yB, cx, batL, batR, resW, resL, resR, battery, resistor, path };
}

function hidesBall(p: Pt, g: ReturnType<typeof layoutOhm>) {
  const onBattery = p.x >= g.batL - 8 && p.x <= g.batR + 8 && Math.abs(p.y - g.yT) < 20;
  const onResistor = p.x >= g.resL - 8 && p.x <= g.resR + 8 && Math.abs(p.y - g.yB) < 22;
  return onBattery || onResistor;
}

function dist(a: Pt, b: Pt) {
  return Math.hypot(b.x - a.x, b.y - a.y);
}

function pointOnPath(t: number, pts: Pt[]): Pt {
  if (pts.length < 2) return { x: 0, y: 0 };
  const lens: number[] = [];
  let total = 0;
  for (let i = 0; i < pts.length - 1; i++) {
    const len = dist(pts[i], pts[i + 1]);
    lens.push(len);
    total += len;
  }
  if (total <= 0) return pts[0];
  let s = (((t % 1) + 1) % 1) * total;
  for (let i = 0; i < lens.length; i++) {
    if (s <= lens[i] || i === lens.length - 1) {
      const k = lens[i] <= 0 ? 0 : s / lens[i];
      const a = pts[i];
      const b = pts[i + 1];
      return { x: a.x + (b.x - a.x) * k, y: a.y + (b.y - a.y) * k };
    }
    s -= lens[i];
  }
  return pts[0];
}

function hit(p: Pt, r: { x: number; y: number; w: number; h: number }) {
  return p.x >= r.x && p.x <= r.x + r.w && p.y >= r.y && p.y <= r.y + r.h;
}

export function OhmCanvas({
  epsilonV,
  internalOhm,
  externalOhm,
  currentA,
  voltageExternal,
  voltageInternal,
  phase,
  dragEnabled,
  onPlaceEpsilon,
  onPlaceR,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({
    epsilonV,
    internalOhm,
    externalOhm,
    currentA,
    voltageExternal,
    voltageInternal,
    phase,
    dragEnabled,
    onPlaceEpsilon,
    onPlaceR,
  });
  const drawRef = useRef<() => void>(() => undefined);
  const drag = useRef<{ kind: "e" | "r"; startY: number; startVal: number } | null>(null);

  useEffect(() => {
    state.current = {
      epsilonV,
      internalOhm,
      externalOhm,
      currentA,
      voltageExternal,
      voltageInternal,
      phase,
      dragEnabled,
      onPlaceEpsilon,
      onPlaceR,
    };
    drawRef.current();
  }, [
    epsilonV,
    internalOhm,
    externalOhm,
    currentA,
    voltageExternal,
    voltageInternal,
    phase,
    dragEnabled,
    onPlaceEpsilon,
    onPlaceR,
  ]);

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
      const g = layoutOhm(w, h);
      const { xL, xR, yT, yB, cx, batL, batR, resW, resL, resR, path } = g;

      ctx.strokeStyle = ink;
      ctx.lineWidth = 1.6;
      ctx.beginPath();
      ctx.moveTo(xL, yT);
      ctx.lineTo(batL, yT);
      ctx.moveTo(batR, yT);
      ctx.lineTo(xR, yT);
      ctx.lineTo(xR, yB);
      ctx.lineTo(resR, yB);
      ctx.moveTo(resL, yB);
      ctx.lineTo(xL, yB);
      ctx.lineTo(xL, yT);
      ctx.stroke();
      ctx.lineWidth = 1;

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo((batL + batR) / 2 - 8, yT - 16);
      ctx.lineTo((batL + batR) / 2 - 8, yT + 16);
      ctx.moveTo((batL + batR) / 2 + 8, yT - 9);
      ctx.lineTo((batL + batR) / 2 + 8, yT + 9);
      ctx.stroke();

      ctx.strokeStyle = accent;
      ctx.strokeRect(resL, yB - 14, resW, 28);
      ctx.beginPath();
      const zL = resL + 8;
      const zR = resR - 8;
      const zMid = yB;
      ctx.moveTo(zL, zMid);
      for (let i = 0; i <= 6; i++) {
        const t = i / 6;
        ctx.lineTo(zL + t * (zR - zL), zMid + (i % 2 === 0 ? -8 : 8));
      }
      ctx.stroke();

      const n = 22;
      const radius = 5;
      for (let i = 0; i < n; i++) {
        const p = pointOnPath(s.phase + i / n, path);
        if (hidesBall(p, g)) continue;
        ctx.fillStyle = colN;
        ctx.beginPath();
        ctx.arc(p.x, p.y, radius, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "#efece6";
        ctx.beginPath();
        ctx.arc(p.x - 1.4, p.y - 1.4, 1.8, 0, Math.PI * 2);
        ctx.fill();
      }

      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillStyle = mute;
      ctx.fillText("ε, r", cx - 14, yT - 26);
      ctx.fillText("R", resL - 2, yB - 32);
      ctx.fillText(`${s.epsilonV.toFixed(1)} В`, batR + 18, yT - 10);
      ctx.fillText(`${s.internalOhm.toFixed(2)} Ω`, batR + 18, yT + 8);
      ctx.fillText(`${s.externalOhm.toFixed(1)} Ω`, resR + 16, yB + 22);

      const midRightY = (yT + yB) / 2;
      const lenI = forceArrowLen(s.currentA, 48, 70);
      if (lenI > 1) {
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, xR - 28, midRightY - lenI / 2, xR - 28, midRightY + lenI / 2);
        ctx.fillText("I", xR - 46, midRightY + 4);
      }
      const lenU = forceArrowLen(s.voltageExternal, 1.8, 48);
      if (lenU > 1) {
        const uBase = yB - 20;
        ctx.strokeStyle = colTr;
        ctx.fillStyle = colTr;
        arrow(ctx, cx + 22, uBase, cx + 22, uBase - lenU);
        ctx.fillText("U_R", cx + 32, uBase - lenU - 2);
      }
      const lenUr = forceArrowLen(s.voltageInternal, 28, 36);
      if (lenUr > 1) {
        const ux = Math.max(xL + 18, batL - 26);
        ctx.strokeStyle = colEl;
        ctx.fillStyle = colEl;
        arrow(ctx, ux, yT, ux, yT + lenUr);
        ctx.fillText("U_r", ux - 28, yT + lenUr * 0.6);
      }
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutOhm(parent.clientWidth, parent.clientHeight);
      if (hit(p, g.battery)) {
        e.preventDefault();
        canvas.setPointerCapture(e.pointerId);
        drag.current = { kind: "e", startY: p.y, startVal: state.current.epsilonV };
        return;
      }
      if (hit(p, g.resistor)) {
        e.preventDefault();
        canvas.setPointerCapture(e.pointerId);
        drag.current = { kind: "r", startY: p.y, startVal: state.current.externalOhm };
      }
    };
    const onMove = (e: PointerEvent) => {
      if (!drag.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const dy = drag.current.startY - p.y;
      if (drag.current.kind === "e") {
        state.current.onPlaceEpsilon(clamp(drag.current.startVal + dy * 0.08, 0.5, 50));
      } else {
        state.current.onPlaceR(clamp(drag.current.startVal + dy * 0.35, 0.1, 100));
      }
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
