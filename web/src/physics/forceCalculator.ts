export const G = 9.81;

export type ForceResult = {
  weightN: number;
  normalN: number;
  forceAlongPlaneN: number;
  frictionN: number;
  accelerationMps2: number;
  isAtRest: boolean;
};

export function weight(massKg: number): number {
  return massKg * G;
}

export function forceAlongPlane(massKg: number, angleDegrees: number): number {
  const rad = (angleDegrees * Math.PI) / 180.0;
  return massKg * G * Math.sin(rad);
}

export function normalForce(massKg: number, angleDegrees: number): number {
  const rad = (angleDegrees * Math.PI) / 180.0;
  return massKg * G * Math.cos(rad);
}

export function frictionForce(frictionMu: number, normal: number): number {
  return frictionMu * normal;
}

export function isAtRest(forceAlong: number, friction: number): boolean {
  return friction >= forceAlong;
}

export function acceleration(
  massKg: number,
  angleDegrees: number,
  frictionMu: number,
): number {
  const fAlong = forceAlongPlane(massKg, angleDegrees);
  const n = normalForce(massKg, angleDegrees);
  const fTr = frictionForce(frictionMu, n);
  if (isAtRest(fAlong, fTr)) return 0;
  return (fAlong - fTr) / massKg;
}

export function compute(
  massKg: number,
  angleDegrees: number,
  frictionMu: number,
): ForceResult {
  const f = weight(massKg);
  const fAlong = forceAlongPlane(massKg, angleDegrees);
  const n = normalForce(massKg, angleDegrees);
  const fTr = frictionForce(frictionMu, n);
  const atRest = isAtRest(fAlong, fTr);
  const a = atRest ? 0 : (fAlong - fTr) / massKg;
  return {
    weightN: f,
    normalN: n,
    forceAlongPlaneN: fAlong,
    frictionN: fTr,
    accelerationMps2: a,
    isAtRest: atRest,
  };
}
