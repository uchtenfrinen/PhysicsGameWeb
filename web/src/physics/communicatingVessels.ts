export const G = 9.81;

export function levelHeight(volume: number, area1: number, area2: number): number {
  const totalArea = area1 + area2;
  if (totalArea <= 0 || volume < 0) return 0;
  return volume / totalArea;
}

export function pressureAtDepth(rho: number, h: number, g = G): number {
  if (rho < 0 || h < 0) return 0;
  return rho * g * h;
}

export function volumeFromLevel(
  level: number,
  area1: number,
  area2: number,
): number {
  if (level < 0) return 0;
  return level * (area1 + area2);
}

export function heightLightFromHeavy(
  rhoLight: number,
  rhoHeavy: number,
  heightHeavy: number,
): number {
  if (rhoLight <= 0 || heightHeavy < 0) return 0;
  return heightHeavy * (rhoHeavy / rhoLight);
}

export function pressureAtInterface(
  rhoHeavy: number,
  heightHeavy: number,
  g = G,
): number {
  return pressureAtDepth(rhoHeavy, heightHeavy, g);
}
