import { Block } from "./block.ts";
import { clamp } from "./clamp.ts";
import { acceleration, compute, type ForceResult } from "./forceCalculator.ts";
import { InclinedPlane } from "./inclinedPlane.ts";

export class PhysicsEngine {
  static readonly minT = 0.14;

  frictionMu: number;
  readonly plane: InclinedPlane;
  readonly block: Block;

  constructor(plane: InclinedPlane, block: Block, frictionMu: number) {
    this.plane = plane;
    this.block = block;
    this.frictionMu = clamp(frictionMu, 0, 1);
  }

  get acceleration(): number {
    return acceleration(this.block.massKg, this.plane.angleDegrees, this.frictionMu);
  }

  get currentForces(): ForceResult {
    return compute(this.block.massKg, this.plane.angleDegrees, this.frictionMu);
  }

  step(dt: number): void {
    const a = this.acceleration;
    this.block.velocityAlongPlane += a * dt;
    let t = this.block.positionAlongPlane;
    t -= this.block.velocityAlongPlane * dt;
    t = Math.max(PhysicsEngine.minT, t);
    this.block.positionAlongPlane = t;
  }

  get reachedBottom(): boolean {
    return this.block.positionAlongPlane <= PhysicsEngine.minT;
  }
}
