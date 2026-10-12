export const G = 9.81;

export function archimedesForce(
  rhoFluid: number,
  volumeSubmerged: number,
  g = G,
): number {
  if (rhoFluid < 0 || volumeSubmerged < 0) return 0;
  return rhoFluid * g * volumeSubmerged;
}

export function weight(rhoBody: number, volume: number, g = G): number {
  if (rhoBody < 0 || volume <= 0) return 0;
  return rhoBody * g * volume;
}

export function submergedFraction(rhoBody: number, rhoFluid: number): number {
  if (rhoFluid <= 0) return 1;
  return Math.min(1, Math.max(0, rhoBody / rhoFluid));
}

export function isFloating(rhoBody: number, rhoFluid: number): boolean {
  return rhoFluid > 0 && rhoBody < rhoFluid;
}

export function isSinking(rhoBody: number, rhoFluid: number): boolean {
  return rhoFluid > 0 && rhoBody > rhoFluid;
}

export function isNeutralEquilibrium(rhoBody: number, rhoFluid: number): boolean {
  return rhoFluid > 0 && Math.abs(rhoBody - rhoFluid) < 1e-9;
}
