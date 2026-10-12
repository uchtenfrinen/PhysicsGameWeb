export function accelerationMagnitude(q: number, E: number, m: number): number {
  if (m <= 0 || E < 0) return 0;
  return (Math.abs(q) * E) / m;
}

export function accelerationY(q: number, E: number, m: number): number {
  if (m <= 0 || E < 0) return 0;
  return (-q * E) / m;
}

export function timeOfFlight(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  y0 = 0,
): number {
  if (v0 <= 0 || m <= 0 || E <= 0) return 0;
  const aY = accelerationY(q, E, m);
  if (Math.abs(aY) < 1e-12) return 0;
  const rad = (angleDegrees * Math.PI) / 180.0;
  const v0y = v0 * Math.sin(rad);
  if (y0 <= 0) return (-2 * v0y) / aY;
  const disc = v0y * v0y - 2 * aY * y0;
  if (disc < 0) return 0;
  const t1 = (-v0y + Math.sqrt(disc)) / aY;
  const t2 = (-v0y - Math.sqrt(disc)) / aY;
  const t = Math.max(t1, t2);
  return t > 0 ? t : 0;
}

export function range(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  y0 = 0,
): number {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const t = timeOfFlight(v0, angleDegrees, q, E, m, y0);
  return v0 * Math.cos(rad) * t;
}

export function maxHeight(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  y0 = 0,
): number {
  if (m <= 0 || E <= 0) return Math.max(0, y0);
  const aY = accelerationY(q, E, m);
  const rad = (angleDegrees * Math.PI) / 180.0;
  const v0y = v0 * Math.sin(rad);
  return y0 + (v0y * v0y) / (2 * Math.abs(aY));
}

export function positionAt(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  t: number,
  y0 = 0,
): { x: number; y: number } {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const aY = accelerationY(q, E, m);
  const x = v0 * Math.cos(rad) * t;
  const y = y0 + v0 * Math.sin(rad) * t + 0.5 * aY * t * t;
  return { x, y };
}

export function velocityAt(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  t: number,
): { vx: number; vy: number } {
  const rad = (angleDegrees * Math.PI) / 180.0;
  const aY = accelerationY(q, E, m);
  return {
    vx: v0 * Math.cos(rad),
    vy: v0 * Math.sin(rad) + aY * t,
  };
}

export function speedAt(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  t: number,
): number {
  const { vx, vy } = velocityAt(v0, angleDegrees, q, E, m, t);
  return Math.hypot(vx, vy);
}

export function getTrajectoryPoints(
  v0: number,
  angleDegrees: number,
  q: number,
  E: number,
  m: number,
  pointCount = 80,
  y0 = 0,
): { x: number; y: number }[] {
  const list: { x: number; y: number }[] = [];
  if (v0 <= 0 || m <= 0 || E <= 0 || pointCount < 2) return list;
  const tMax = timeOfFlight(v0, angleDegrees, q, E, m, y0);
  if (tMax <= 0) return list;
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push(positionAt(v0, angleDegrees, q, E, m, t, y0));
  }
  return list;
}
