export const G = 9.81;

/** T: время до земли. v₀ > 0 вниз. При v₀ = 0 совпадает с C#: √(2h₀/g). */
export function timeOfFall(height0: number, v0 = 0): number {
  if (height0 < 0) return 0;
  if (height0 <= 0 && v0 >= 0) return 0;
  const disc = v0 * v0 + 2 * G * height0;
  if (disc < 0) return 0;
  return (-v0 + Math.sqrt(disc)) / G;
}

/** Скорость удара вниз: √(v₀² + 2gh₀). */
export function finalSpeed(height0: number, v0 = 0): number {
  const t = timeOfFall(height0, v0);
  if (t <= 0) return Math.max(0, v0);
  return v0 + G * t;
}

/** Наибольшая высота: h₀, либо выше, если v₀ < 0 (бросок вверх). */
export function peakHeight(height0: number, v0 = 0): number {
  if (v0 >= 0) return Math.max(0, height0);
  return height0 + (v0 * v0) / (2 * G);
}

export function heightAt(t: number, height0: number, v0 = 0): number {
  if (t <= 0) return height0;
  const h = height0 - v0 * t - (G * t * t) / 2;
  return Math.max(0, h);
}

export function velocityAt(t: number, v0 = 0): number {
  if (t < 0) return 0;
  return v0 + G * t;
}

export function getHeightPoints(
  height0: number,
  pointCount = 60,
  v0 = 0,
): { t: number; h: number }[] {
  const list: { t: number; h: number }[] = [];
  const tMax = timeOfFall(height0, v0);
  if (tMax <= 0 || pointCount < 2) return list;
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, h: heightAt(t, height0, v0) });
  }
  list.push({ t: tMax, h: 0 });
  return list;
}

export function getVelocityPoints(
  height0: number,
  pointCount = 60,
  v0 = 0,
): { t: number; v: number }[] {
  const list: { t: number; v: number }[] = [];
  const tMax = timeOfFall(height0, v0);
  if (tMax <= 0 || pointCount < 2) return list;
  const vEnd = finalSpeed(height0, v0);
  for (let i = 0; i < pointCount; i++) {
    const t = (tMax * i) / (pointCount - 1);
    list.push({ t, v: velocityAt(t, v0) });
  }
  list.push({ t: tMax, v: vEnd });
  return list;
}
