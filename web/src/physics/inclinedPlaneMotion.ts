import { clamp } from "./clamp.ts";
import { compute, G } from "./forceCalculator.ts";

export const planeUMin = 0.14;
export const planeUMax = 0.86;

export function metersFromNorm(u: number, lengthM: number): number {
  const span = planeUMax - planeUMin;
  if (span <= 0 || lengthM <= 0) return 0;
  return ((clamp(u, planeUMin, planeUMax) - planeUMin) / span) * lengthM;
}

export function normFromMeters(s: number, lengthM: number): number {
  if (lengthM <= 0) return planeUMin;
  return planeUMin + (clamp(s, 0, lengthM) / lengthM) * (planeUMax - planeUMin);
}

/** a = g(sin α − μ cos α), знак: вниз по плоскости. Не обнуляется в покое. */
export function signedAccel(angleDegrees: number, frictionMu: number): number {
  const rad = (angleDegrees * Math.PI) / 180;
  return G * (Math.sin(rad) - frictionMu * Math.cos(rad));
}

export function willSlide(v0: number, angleDegrees: number, frictionMu: number): boolean {
  if (Math.abs(v0) > 1e-9) return true;
  return !compute(1, angleDegrees, frictionMu).isAtRest;
}

export function stepAlongPlane(
  s: number,
  v: number,
  dt: number,
  lengthM: number,
  a: number,
  stickIfRest: boolean,
): { s: number; v: number; stopped: boolean; atBottom: boolean } {
  if (dt <= 0) {
    return { s, v, stopped: false, atBottom: s <= 1e-9 };
  }
  if (stickIfRest && Math.abs(v) <= 1e-9) {
    return { s, v: 0, stopped: true, atBottom: s <= 1e-9 };
  }

  const nextV = v + a * dt;
  if (stickIfRest && v * nextV <= 0) {
    return { s, v: 0, stopped: true, atBottom: s <= 1e-9 };
  }

  let nv = nextV;
  let ns = s - nv * dt;
  if (ns <= 0) {
    return { s: 0, v: 0, stopped: true, atBottom: true };
  }
  if (ns >= lengthM) {
    ns = lengthM;
    if (nv < 0) nv = 0;
  }
  if (stickIfRest && Math.abs(nv) <= 1e-6) {
    return { s: ns, v: 0, stopped: true, atBottom: ns <= 1e-9 };
  }
  return { s: ns, v: nv, stopped: false, atBottom: false };
}

const dummy = {
  tMax: 0,
  path: [
    { t: 0, y: 0 },
    { t: 1, y: 0 },
  ],
  vel: [
    { t: 0, y: 0 },
    { t: 1, y: 0 },
  ],
};

export function motionCurves(
  s0: number,
  v0: number,
  lengthM: number,
  angleDegrees: number,
  frictionMu: number,
  pointCount = 60,
) {
  const rest = compute(1, angleDegrees, frictionMu).isAtRest;
  const a = signedAccel(angleDegrees, frictionMu);
  const startS = clamp(s0, 0, Math.max(0, lengthM));
  if (lengthM <= 0 || !willSlide(v0, angleDegrees, frictionMu)) return dummy;
  if (startS <= 1e-9 && v0 >= -1e-9) return dummy;

  const dt = 1 / 200;
  const hist: { t: number; s: number; v: number }[] = [{ t: 0, s: startS, v: v0 }];
  let t = 0;
  let s = startS;
  let v = v0;
  for (let i = 0; i < 20_000; i++) {
    const next = stepAlongPlane(s, v, dt, lengthM, a, rest);
    t += dt;
    if (next.atBottom) {
      hist.push({ t, s: 0, v });
      break;
    }
    s = next.s;
    v = next.v;
    hist.push({ t, s, v });
    if (next.stopped) break;
  }

  const tMax = hist[hist.length - 1]!.t;
  const n = Math.max(2, pointCount);
  const path: { t: number; y: number }[] = [];
  const vel: { t: number; y: number }[] = [];
  for (let i = 0; i < n; i++) {
    const ti = (tMax * i) / (n - 1);
    const sample = sampleHist(hist, ti);
    path.push({ t: ti, y: startS - sample.s });
    vel.push({ t: ti, y: sample.v });
  }
  return { tMax, path, vel };
}

function sampleHist(hist: { t: number; s: number; v: number }[], t: number) {
  if (hist.length === 0) return { s: 0, v: 0 };
  if (t <= hist[0]!.t) return hist[0]!;
  const last = hist[hist.length - 1]!;
  if (t >= last.t) return last;
  let lo = 0;
  let hi = hist.length - 1;
  while (hi - lo > 1) {
    const mid = (lo + hi) >> 1;
    if (hist[mid]!.t <= t) lo = mid;
    else hi = mid;
  }
  const a = hist[lo]!;
  const b = hist[hi]!;
  const u = (t - a.t) / Math.max(1e-12, b.t - a.t);
  return { s: a.s + (b.s - a.s) * u, v: a.v + (b.v - a.v) * u };
}
