export function omega(k: number, m: number): number {
  if (m <= 0 || k < 0) return 0;
  return Math.sqrt(k / m);
}

export function period(k: number, m: number): number {
  const w = omega(k, m);
  return w <= 0 ? 0 : (2 * Math.PI) / w;
}

export function frequency(k: number, m: number): number {
  const t = period(k, m);
  return t <= 0 ? 0 : 1 / t;
}

export function positionAt(
  k: number,
  m: number,
  x0: number,
  v0: number,
  t: number,
): number {
  const w = omega(k, m);
  if (w <= 0) return x0;
  return x0 * Math.cos(w * t) + (v0 / w) * Math.sin(w * t);
}

export function velocityAt(
  k: number,
  m: number,
  x0: number,
  v0: number,
  t: number,
): number {
  const w = omega(k, m);
  if (w <= 0) return v0;
  return -x0 * w * Math.sin(w * t) + v0 * Math.cos(w * t);
}

export function accelerationAt(k: number, m: number, x: number): number {
  return -(k / m) * x;
}

export function forceAt(k: number, x: number): number {
  return -k * x;
}

export function getPositionPoints(
  k: number,
  m: number,
  x0: number,
  v0: number,
  periods = 2,
  pointCount = 120,
): { t: number; x: number }[] {
  const list: { t: number; x: number }[] = [];
  const p = period(k, m);
  if (p <= 0 || pointCount < 2) return list;
  const tMax = periods * p;
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, x: positionAt(k, m, x0, v0, t) });
  }
  return list;
}

export function getVelocityPoints(
  k: number,
  m: number,
  x0: number,
  v0: number,
  periods = 2,
  pointCount = 120,
): { t: number; v: number }[] {
  const list: { t: number; v: number }[] = [];
  const p = period(k, m);
  if (p <= 0 || pointCount < 2) return list;
  const tMax = periods * p;
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, v: velocityAt(k, m, x0, v0, t) });
  }
  return list;
}

export function getAccelerationPoints(
  k: number,
  m: number,
  x0: number,
  v0: number,
  periods = 2,
  pointCount = 120,
): { t: number; a: number }[] {
  const list: { t: number; a: number }[] = [];
  const p = period(k, m);
  if (p <= 0 || pointCount < 2) return list;
  const tMax = periods * p;
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    const x = positionAt(k, m, x0, v0, t);
    list.push({ t, a: accelerationAt(k, m, x) });
  }
  return list;
}
