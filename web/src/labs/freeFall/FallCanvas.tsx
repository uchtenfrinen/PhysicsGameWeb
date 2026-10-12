import { useEffect, useRef } from "react";
import { arrow, colFg, forceArrowLen } from "../draw.ts";
import { G } from "../../physics/freeFall.ts";
import { clamp } from "../../physics/clamp.ts";

type Props = {
  height0: number;
  currentHeight: number;
  massKg: number;
  scaleH: number;
  dragEnabled: boolean;
  onPlace: (h: number) => void;
};

function layoutFall(w: number, h: number, scaleH: number, hNow: number, h0: number) {
  const boxW = Math.min(w - 16, 400);
  const boxH = h - 16;
  const boxX = 8;
  const boxY = 8;
  const left = boxX + 40;
  const right = boxX + boxW - 16;
  const top = boxY + 28;
  const bottom = boxY + boxH - 18;
  const plotH = bottom - top;
  const maxH = Math.max(scaleH, 1);
  const scaleY = plotH / maxH;
  const yGround = bottom;
  return {
    boxW,
    boxH,
    boxX,
    boxY,
    left,
    right,
    top,
    yGround,
    scaleY,
    maxH,
    yTop: yGround - h0 * scaleY,
    yBall: yGround - hNow * scaleY,
    cx: (left + right) / 2,
    r: 13,
  };
}

function heightFromY(my: number, layout: ReturnType<typeof layoutFall>): number {
  return (layout.yGround - my) / layout.scaleY;
}

export function FallCanvas({
  height0,
  currentHeight,
  massKg,
  scaleH,
  dragEnabled,
  onPlace,
}: Props) {
  const ref = useRef<HTMLCanvasElement>(null);
  const state = useRef({ height0, currentHeight, massKg, scaleH, dragEnabled, onPlace });
  const drawRef = useRef<() => void>(() => undefined);
  const dragging = useRef(false);

  useEffect(() => {
    state.current = { height0, currentHeight, massKg, scaleH, dragEnabled, onPlace };
    drawRef.current();
  }, [height0, currentHeight, massKg, scaleH, dragEnabled, onPlace]);

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
      return layoutFall(parent.clientWidth, parent.clientHeight, s.scaleH, s.currentHeight, s.height0);
    };

    const draw = () => {
      const s = state.current;
      const dpr = window.devicePixelRatio || 1;
      const w = parent.clientWidth;
      const h = parent.clientHeight;
      canvas.width = Math.max(1, Math.floor(w * dpr));
      canvas.height = Math.max(1, Math.floor(h * dpr));
      canvas.style.width = `${w}px`;
      canvas.style.height = `${h}px`;
      const ctx = canvas.getContext("2d");
      if (!ctx || w < 40 || h < 40) return;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, w, h);

      const mute = "#6e6a62";
      const line = "#d9d4c9";
      const ink = "#171614";
      const accent = "#2a5349";
      const g = layoutFall(w, h, s.scaleH, s.currentHeight, s.height0);
      const { boxX, boxY, boxW, boxH, left, right, top, yGround, scaleY, maxH, yTop, yBall, cx, r } = g;

      ctx.strokeStyle = line;
      ctx.strokeRect(boxX, boxY, boxW, boxH);

      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText("h, м", left, boxY + 16);

      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(left, top);
      ctx.lineTo(left, yGround);
      ctx.lineTo(right, yGround);
      ctx.stroke();

      ctx.font = "10px Outfit, ui-sans-serif, sans-serif";
      for (let i = 0; i <= 4; i++) {
        const val = (maxH * i) / 4;
        const y = yGround - val * scaleY;
        ctx.strokeStyle = line;
        ctx.beginPath();
        ctx.moveTo(left, y);
        ctx.lineTo(left + 5, y);
        ctx.stroke();
        ctx.fillStyle = mute;
        if (i === 4) continue;
        const label = val === 0 ? "0" : val.toFixed(0);
        ctx.fillText(label, left - ctx.measureText(label).width - 6, y + 3);
      }

      ctx.setLineDash([5, 4]);
      ctx.strokeStyle = line;
      ctx.beginPath();
      ctx.moveTo(cx, yTop);
      ctx.lineTo(cx, yGround);
      ctx.stroke();
      ctx.setLineDash([]);

      ctx.strokeStyle = mute;
      ctx.beginPath();
      ctx.arc(cx, yGround, r * 0.85, 0, Math.PI * 2);
      ctx.stroke();

      ctx.fillStyle = accent;
      ctx.beginPath();
      ctx.arc(cx, yBall, r, 0, Math.PI * 2);
      ctx.fill();

      ctx.fillStyle = mute;
      ctx.font = "11px Outfit, ui-sans-serif, sans-serif";
      ctx.fillText(`h₀ ${s.height0.toFixed(1)}`, Math.min(cx + r + 10, right - 56), yTop + 16);

      const nearTop = Math.abs(yBall - yTop) < 18;
      const nearGround = Math.abs(yBall - yGround) < 18;
      if (!nearTop && !nearGround) {
        ctx.fillText(`h ${s.currentHeight.toFixed(1)}`, cx + r + 8, yBall + 4);
      }

      const lenFg = forceArrowLen(s.massKg * G, 2.4);
      if (lenFg > 1) {
        ctx.strokeStyle = colFg;
        ctx.fillStyle = colFg;
        arrow(ctx, cx, yBall, cx, yBall + lenFg);
        ctx.fillText("Fg", cx + 8, yBall + lenFg * 0.7);
      }
    };

    const onDown = (e: PointerEvent) => {
      if (!state.current.dragEnabled) return;
      e.preventDefault();
      canvas.setPointerCapture(e.pointerId);
      dragging.current = true;
      const p = pointOnCanvas(e);
      state.current.onPlace(clamp(heightFromY(p.y, layoutNow()), 0.1, 1000));
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging.current || !state.current.dragEnabled) return;
      const p = pointOnCanvas(e);
      state.current.onPlace(clamp(heightFromY(p.y, layoutNow()), 0.1, 1000));
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
      className="fall-canvas"
      style={{
        cursor: dragEnabled ? "grab" : "default",
        touchAction: "none",
      }}
    />
  );
}
