export const mute = "#6e6a62";
export const line = "#d9d4c9";
export const ink = "#171614";
export const accent = "#2a5349";
export const force = "#b44a3a";
export const colFg = "#b44a3a";
export const colN = "#3d7a9a";
export const colTr = "#c48a2a";
export const colEl = "#2a7a54";
export const colFa = "#4a8aad";

export function fitCanvas(canvas: HTMLCanvasElement, parent: HTMLElement) {
  const dpr = window.devicePixelRatio || 1;
  const w = parent.clientWidth;
  const h = parent.clientHeight;
  canvas.width = Math.max(1, Math.floor(w * dpr));
  canvas.height = Math.max(1, Math.floor(h * dpr));
  canvas.style.width = `${w}px`;
  canvas.style.height = `${h}px`;
  const ctx = canvas.getContext("2d");
  if (!ctx || w < 40 || h < 40) return null;
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.clearRect(0, 0, w, h);
  return { ctx, w, h };
}

export function labBox(w: number, h: number) {
  const boxW = Math.min(w - 16, 400);
  const boxH = h - 16;
  const boxX = 8;
  const boxY = 8;
  return {
    boxW,
    boxH,
    boxX,
    boxY,
    left: boxX + 40,
    right: boxX + boxW - 16,
    top: boxY + 28,
    bottom: boxY + boxH - 18,
  };
}

export function arrow(
  ctx: CanvasRenderingContext2D,
  x0: number,
  y0: number,
  x1: number,
  y1: number,
) {
  ctx.beginPath();
  ctx.moveTo(x0, y0);
  ctx.lineTo(x1, y1);
  ctx.stroke();
  const a = Math.atan2(y1 - y0, x1 - x0);
  ctx.beginPath();
  ctx.moveTo(x1, y1);
  ctx.lineTo(x1 - 7 * Math.cos(a - 0.4), y1 - 7 * Math.sin(a - 0.4));
  ctx.lineTo(x1 - 7 * Math.cos(a + 0.4), y1 - 7 * Math.sin(a + 0.4));
  ctx.closePath();
  ctx.fill();
}

export function paintArrow(
  ctx: CanvasRenderingContext2D,
  x0: number,
  y0: number,
  x1: number,
  y1: number,
  color: string,
) {
  ctx.strokeStyle = color;
  ctx.fillStyle = color;
  arrow(ctx, x0, y0, x1, y1);
}

export function forceArrowLen(magnitude: number, pxPerUnit: number, maxPx = 56): number {
  if (!(magnitude > 0) || !(pxPerUnit > 0) || !(maxPx > 0)) return 0;
  return Math.min(maxPx, magnitude * pxPerUnit);
}
