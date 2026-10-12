import { clamp } from "./clamp.ts";

export class InclinedPlane {
  #angleDegrees = 30;

  get angleDegrees(): number {
    return this.#angleDegrees;
  }

  set angleDegrees(value: number) {
    this.#angleDegrees = clamp(value, 10, 60);
  }

  get angleRadians(): number {
    return (this.#angleDegrees * Math.PI) / 180.0;
  }
}
