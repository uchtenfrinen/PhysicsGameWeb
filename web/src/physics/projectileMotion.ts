export const G = 9.81;

export function timeOfFlight(v0: number, angleDegrees: number, y0 = 0): number {
  if (v0 <= 0) return 0;
  const rad = (angleDegrees * Math.PI) / 180.0;
  const v0y = v0 * Math.sin(rad);
  if (y0 <= 0) return (2 * v0y) / G;
  const disc = v0y * v0y + 2 * G * y0;
  if (disc < 0) return 0;
  return (v0y + Math.sqrt(disc)) / G;
}

export function range(v0: number, angleDegrees: number, y0 = 0): number {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const t = timeOfFlight(v0, angleDegrees, y0);
  return v0 * Math.cos(rad) * t;
}

export function maxHeight(v0: number, angleDegrees: number, y0 = 0): number {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const v0y = v0 * Math.sin(rad);
  return y0 + (v0y * v0y) / (2 * G);
}

export function positionAt(
  v0: number,
  angleDegrees: number,
  t: number,
  y0 = 0,
): { x: number; y: number } {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const x = v0 * Math.cos(rad) * t;
  const y = y0 + v0 * Math.sin(rad) * t - (G * t * t) / 2;
  return { x, y };
}

export function velocityAt(
  v0: number,
  angleDegrees: number,
  t: number,
): { vx: number; vy: number } {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const vx = v0 * Math.cos(rad);
  const vy = v0 * Math.sin(rad) - G * t;
  return { vx, vy };
}

export function speedAt(v0: number, angleDegrees: number, t: number): number {
  const { vx, vy } = velocityAt(v0, angleDegrees, t);
  return Math.sqrt(vx * vx + vy * vy);
}

export function getHeightTimePoints(
  v0: number,
  angleDegrees: number,
  y0 = 0,
  pointCount = 60,
): { t: number; y: number }[] {
  const list: { t: number; y: number }[] = [];
  if (v0 <= 0 || pointCount < 2) return list;
  const tMax = timeOfFlight(v0, angleDegrees, y0);
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, y: Math.max(0, positionAt(v0, angleDegrees, t, y0).y) });
  }
  return list;
}

export function getSpeedTimePoints(
  v0: number,
  angleDegrees: number,
  y0 = 0,
  pointCount = 60,
): { t: number; v: number }[] {
  const list: { t: number; v: number }[] = [];
  if (v0 <= 0 || pointCount < 2) return list;
  const tMax = timeOfFlight(v0, angleDegrees, y0);
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, v: speedAt(v0, angleDegrees, t) });
  }
  return list;
}

export function getTrajectoryPoints(
  v0: number,
  angleDegrees: number,
  y0: number,
  pointCount = 80,
): { x: number; y: number }[] {
  const list: { x: number; y: number }[] = [];
  if (v0 <= 0 || pointCount < 2) return list;
  const tMax = timeOfFlight(v0, angleDegrees, y0);
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push(positionAt(v0, angleDegrees, t, y0));
  }
  return list;
}
