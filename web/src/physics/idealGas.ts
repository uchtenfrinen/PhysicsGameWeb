export const R = 8.314;

export function pressure(n: number, T: number, V: number): number {
  if (V <= 0 || n < 0 || T < 0) return 0;
  return (n * R * T) / V;
}

export function volume(n: number, T: number, P: number): number {
  if (P <= 0 || n < 0 || T < 0) return 0;
  return (n * R * T) / P;
}

export function temperature(P: number, V: number, n: number): number {
  if (n <= 0 || V <= 0) return 0;
  return (P * V) / (n * R);
}

export function isothermPoints(
  n: number,
  T: number,
  vMin: number,
  vMax: number,
  count = 80,
): { v: number; p: number }[] {
  const list: { v: number; p: number }[] = [];
  if (vMin <= 0 || vMax <= vMin || count < 2) return list;
  for (let i = 0; i < count; i++) {
    const V = vMin + ((vMax - vMin) * i) / (count - 1);
    list.push({ v: V, p: pressure(n, T, V) });
  }
  return list;
}
