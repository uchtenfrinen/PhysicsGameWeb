export function imageDistance(d: number, f: number): number {
  if (f <= 0 || d <= 0) return 0;
  if (Math.abs(d - f) < 1e-10) return Number.NaN;
  return (f * d) / (d - f);
}

export function magnification(d: number, f: number): number {
  const dPrime = imageDistance(d, f);
  if (Number.isNaN(dPrime)) return 0;
  return -dPrime / d;
}

export function magnificationAbs(d: number, f: number): number {
  return Math.abs(magnification(d, f));
}

export function imageHeight(objectHeight: number, d: number, f: number): number {
  return objectHeight * magnificationAbs(d, f);
}

export function isInverted(d: number, f: number): boolean {
  return magnification(d, f) < 0;
}

export function isRealImage(d: number, f: number): boolean {
  return d > f;
}
