import { clamp } from "./clamp.ts";

export class Block {
  #massKg = 2;
  #positionAlongPlane = 0.5;

  get massKg(): number {
    return this.#massKg;
  }

  set massKg(value: number) {
    this.#massKg = clamp(value, 0.5, 50);
  }

  get positionAlongPlane(): number {
    return this.#positionAlongPlane;
  }

  set positionAlongPlane(value: number) {
    this.#positionAlongPlane = clamp(value, 0, 1);
  }

  velocityAlongPlane = 0;
}
