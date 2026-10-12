import { useEffect, useRef } from "react";
import { accent, arrow, colN, fitCanvas, forceArrowLen, ink, mute } from "../draw.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  hLight: number;
  hHeavy: number;
  hLightMax: number;
  hHeavyMax: number;
  vLight: number;
  vHeavy: number;
  dragEnabled: boolean;
  onPlaceLight: (h: number) => void;
  onPlaceHeavy: (h: number) => void;
};

function layoutVessels(w: number, h: number, hLightMax: number, hHeavyMax: number) {
  const padT = 16;
  const padB = 26;
  const padL = 28;
  const padR = 20;
  const fluidTop = padT;
  const fluidBottom = h - padB;
  const vesselH = Math.max(80, fluidBottom - fluidTop);
  const connH = 24;
  const yConnTop = fluidBottom - connH;
  const interfaceY = Math.min(fluidTop + vesselH * 0.55, yConnTop - 36);
  const maxH = Math.max(hLightMax, hHeavyMax, 0.25);
  const ppm = Math.max(40, (interfaceY - fluidTop - 20) / (maxH * 1.08));
  const colW = Math.min(96, Math.max(56, w * 0.14));
  const inner = w - padL - padR;
  const gap = Math.min(inner * 0.3, 168);
  const cx = padL + inner / 2;
  const x1 = cx - gap / 2 - colW;
  const x2 = cx + gap / 2;
  const hMaxDrag = Math.max(1e-4, (interfaceY - fluidTop - 12) / ppm);
  return { fluidTop, fluidBottom, yConnTop, interfaceY, ppm, colW, x1, x2, connH, hMaxDrag };
}

function hFromY(my: number, interfaceY: number, ppm: number, hMax: number): number {
  return clamp((interfaceY - my) / ppm, 1e-4, hMax);
}

export function VesselsCanvas({
  hLight,
  hHeavy,
  hLightMax,
  hHeavyMax,
  vLight,
  vHeavy,
  dragEnabled,
  onPlaceLight,
  onPlaceHeavy,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({
    hLight,
    hHeavy,
    hLightMax,
    hHeavyMax,
    vLight,
    vHeavy,
    dragEnabled,
    onPlaceLight,
    onPlaceHeavy,
  });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef<"light" | "heavy" | null>(null);

  useEffect(() => {
    state.current = {
      hLight,
      hHeavy,
      hLightMax,
      hHeavyMax,
      vLight,
      vHeavy,
      dragEnabled,
      onPlaceLight,
      onPlaceHeavy,
    };
    drawRef.current();
  }, [hLight, hHeavy, hLightMax, hHeavyMax, vLight, vHeavy, dragEnabled, onPlaceLight, onPlaceHeavy]);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;
    const parent = canvas.parentElement;
    if (!parent) return;

    const pointOnCanvas = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    };

    const layoutNow = () =>
      layoutVessels(parent.clientWidth, parent.clientHeight, state.current.hLightMax, state.current.hHeavyMax);

    const columnAt = (px: number, g: ReturnType<typeof layoutVessels>) => {
      if (px >= g.x1 && px <= g.x1 + g.colW) return "light" as const;
      if (px >= g.x2 && px <= g.x2 + g.colW) return "heavy" as const;
      return null;
    };

    const applyPlace = (which: "light" | "heavy", my: number, g: ReturnType<typeof layoutVessels>) => {
      const h = hFromY(my, g.interfaceY, g.ppm, g.hMaxDrag);
      if (which === "light") state.current.onPlaceLight(h);
      else state.current.onPlaceHeavy(h);
    };

    const draw = () => {
      const sized = fitCanvas(canvas, parent);
      if (!sized) return;
      const { ctx, w, h } = sized;
      const s = state.current;
      const g = layoutVessels(w, h, s.hLightMax, s.hHeavyMax);
      const { fluidTop, fluidBottom, yConnTop, interfaceY, ppm, colW, x1, x2 } = g;

      const oilTop = interfaceY - Math.max(0, s.hLight) * ppm;
      const waterTop = interfaceY - Math.max(0, s.hHeavy) * ppm;
      const oilH = Math.max(0, interfaceY - oilTop);

      ctx.fillStyle = "#5a7a8a";
      ctx.fillRect(x1 + 2, interfaceY, colW - 4, fluidBottom - interfaceY);
      ctx.fillRect(x2 + 2, waterTop, colW - 4, fluidBottom - waterTop);
      ctx.fillRect(x1 + colW, yConnTop + 2, x2 - x1 - colW, fluidBottom - yConnTop - 4);
      if (oilH > 0.5) {
        ctx.fillStyle = "#c4a574";
        ctx.fillRect(x1 + 2, oilTop, colW - 4, oilH);
      }

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(x1, fluidTop);
      ctx.lineTo(x1, fluidBottom);
      ctx.lineTo(x2 + colW, fluidBottom);
      ctx.lineTo(x2 + colW, fluidTop);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x1 + colW, fluidTop);
      ctx.lineTo(x1 + colW, yConnTop);
      ctx.lineTo(x2, yConnTop);
      ctx.lineTo(x2, fluidTop);
      ctx.stroke();

      ctx.strokeStyle = mute;
      ctx.setLineDash([5, 4]);
      ctx.beginPath();
      ctx.moveTo(x1, interfaceY);
      ctx.lineTo(x2 + colW, interfaceY);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText("граница", x1 + colW + 8, interfaceY - 6);

      ctx.fillStyle = accent;
      if (oilH > 0.5) ctx.fillRect(x1 + 2, oilTop - 1, colW - 4, 2);
      ctx.fillRect(x2 + 2, waterTop - 1, colW - 4, 2);

      ctx.strokeStyle = mute;
      ctx.beginPath();
      ctx.moveTo(x1 + colW + 10, oilTop);
      ctx.lineTo(x1 + colW + 10, interfaceY);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x2 - 10, waterTop);
      ctx.lineTo(x2 - 10, interfaceY);
      ctx.stroke();

      const mid1x = x1 + colW / 2;
      const mid2x = x2 + colW / 2;
      const lenV1 = forceArrowLen(Math.abs(s.vLight), 500, 90);
      const lenV2 = forceArrowLen(Math.abs(s.vHeavy), 500, 90);
      if (lenV1 > 1 && oilH > 8) {
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, mid1x, oilTop, mid1x, oilTop - lenV1);
        ctx.fillText("v", mid1x + 8, oilTop - lenV1 - 4);
      }
      if (lenV2 > 1) {
        ctx.strokeStyle = colN;
        ctx.fillStyle = colN;
        arrow(ctx, mid2x, waterTop, mid2x, waterTop - lenV2);
        ctx.fillText("v", mid2x + 8, waterTop - lenV2 - 4);
      }

      ctx.fillStyle = ink;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText(`h₁ ${s.hLight.toFixed(3)}`, x1, oilTop - 8);
      ctx.fillText(`h₂ ${s.hHeavy.toFixed(3)}`, x2, waterTop - 8);
      ctx.fillStyle = mute;
      ctx.fillText("масло", x1, fluidBottom + 16);
      ctx.fillText("вода", x2, fluidBottom + 16);
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      const g = layoutNow();
      const which = columnAt(p.x, g);
      if (!which) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = which;
      applyPlace(which, p.y, g);
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      applyPlace(dragging.current, p.y, layoutNow());
    };
    const onUp = (e: PointerEvent) => {
      if (!dragging.current) return;
      dragging.current = null;
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
