export function seriesEquivalent(r1: number, r2: number, r3?: number): number {
  if (r3 === undefined) return Math.max(0, r1) + Math.max(0, r2);
  return Math.max(0, r1) + Math.max(0, r2) + Math.max(0, r3);
}

export function parallelEquivalent(r1: number, r2: number, r3?: number): number {
  if (r3 === undefined) {
    if (r1 <= 0 && r2 <= 0) return 0;
    if (r1 <= 0) return Math.max(0, r2);
    if (r2 <= 0) return Math.max(0, r1);
    return (r1 * r2) / (r1 + r2);
  }
  let s = 0;
  if (r1 > 0) s += 1 / r1;
  if (r2 > 0) s += 1 / r2;
  if (r3 > 0) s += 1 / r3;
  return s > 0 ? 1 / s : 0;
}

export function current(voltageV: number, resistanceOhm: number): number {
  if (resistanceOhm <= 0) return 0;
  return voltageV / resistanceOhm;
}

export function power(voltageV: number, currentA: number): number {
  return voltageV * currentA;
}

export function voltageDropSeries(currentA: number, resistanceOhm: number): number {
  return currentA * Math.max(0, resistanceOhm);
}

export function branchCurrentParallel(
  voltageV: number,
  resistanceOhm: number,
): number {
  if (resistanceOhm <= 0) return 0;
  return voltageV / resistanceOhm;
}
