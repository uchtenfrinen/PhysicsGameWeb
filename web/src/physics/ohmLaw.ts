export function currentClosed(
  emfV: number,
  internalOhm: number,
  externalOhm: number,
): number {
  const sum = internalOhm + externalOhm;
  if (sum <= 0) return 0;
  return emfV / sum;
}

export function emf(
  currentA: number,
  internalOhm: number,
  externalOhm: number,
): number {
  if (currentA <= 0) return 0;
  return currentA * (internalOhm + externalOhm);
}

export function internalResistance(
  emfV: number,
  currentA: number,
  externalOhm: number,
): number {
  if (currentA <= 0) return 0;
  return Math.max(0, emfV / currentA - externalOhm);
}

export function externalResistance(
  emfV: number,
  currentA: number,
  internalOhm: number,
): number {
  if (currentA <= 0) return 0;
  return Math.max(0, emfV / currentA - internalOhm);
}

export function voltageExternal(currentA: number, externalOhm: number): number {
  return currentA * externalOhm;
}

export function voltageInternal(currentA: number, internalOhm: number): number {
  return currentA * internalOhm;
}

export function powerExternal(currentA: number, externalOhm: number): number {
  return currentA * currentA * externalOhm;
}

export function voltage(currentA: number, resistanceOhm: number): number {
  return currentA * resistanceOhm;
}

export function current(voltageV: number, resistanceOhm: number): number {
  return resistanceOhm <= 0 ? 0 : voltageV / resistanceOhm;
}

export function resistance(voltageV: number, currentA: number): number {
  return currentA <= 0 ? 0 : voltageV / currentA;
}

export function power(voltageV: number, currentA: number): number {
  return voltageV * currentA;
}
