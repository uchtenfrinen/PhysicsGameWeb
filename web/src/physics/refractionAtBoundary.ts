export function refractionAngleDegrees(
  n1: number,
  n2: number,
  incidenceAngleDegrees: number,
): number {
  if (n1 <= 0 || n2 <= 0) return 0;
  const rad = (incidenceAngleDegrees * Math.PI) / 180.0;
  const sin2 = (n1 * Math.sin(rad)) / n2;
  if (sin2 >= 1 || sin2 <= -1) return Number.NaN;
  return (180.0 * Math.asin(sin2)) / Math.PI;
}

export function criticalAngleDegrees(n1: number, n2: number): number | null {
  if (n1 <= 0 || n2 <= 0 || n1 <= n2) return null;
  const sinCrit = n2 / n1;
  if (sinCrit > 1) return null;
  return (180.0 * Math.asin(sinCrit)) / Math.PI;
}

export function isTotalInternalReflection(
  n1: number,
  n2: number,
  incidenceAngleDegrees: number,
): boolean {
  if (n1 <= n2) return false;
  const crit = criticalAngleDegrees(n1, n2);
  if (crit === null) return false;
  return incidenceAngleDegrees >= crit;
}
