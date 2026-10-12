export function positionAt(
  A: number,
  omega: number,
  phi0Degrees: number,
  t: number,
): number {
  const phi0 = (phi0Degrees * Math.PI) / 180.0;
  return A * Math.cos(omega * t + phi0);
}

export function velocityAt(
  A: number,
  omega: number,
  phi0Degrees: number,
  t: number,
): number {
  const phi0 = (phi0Degrees * Math.PI) / 180.0;
  return -A * omega * Math.sin(omega * t + phi0);
}

export function accelerationAt(
  A: number,
  omega: number,
  phi0Degrees: number,
  t: number,
): number {
  const x = positionAt(A, omega, phi0Degrees, t);
  return -omega * omega * x;
}

export function period(omega: number): number {
  return omega <= 0 ? 0 : (2 * Math.PI) / omega;
}

export function frequency(omega: number): number {
  return omega <= 0 ? 0 : omega / (2 * Math.PI);
}

export function getChartPoints(
  A: number,
  omega: number,
  phi0Degrees: number,
  periods = 2,
  pointCount = 150,
): { t: number; x: number }[] {
  const list: { t: number; x: number }[] = [];
  const p = period(omega);
  if (p <= 0 || pointCount < 2) return list;
  const tMax = periods * p;
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, x: positionAt(A, omega, phi0Degrees, t) });
  }
  return list;
}
