import { describe, expect, it } from "vitest";
import gold from "./gold.json";
import { Block } from "./block.ts";
import * as buoyancy from "./buoyancy.ts";
import * as chargeInField from "./chargeInField.ts";
import * as communicatingVessels from "./communicatingVessels.ts";
import * as forceCalculator from "./forceCalculator.ts";
import * as freeFall from "./freeFall.ts";
import * as harmonicOscillations from "./harmonicOscillations.ts";
import * as idealGas from "./idealGas.ts";
import { InclinedPlane } from "./inclinedPlane.ts";
import * as inclinedPlaneMotion from "./inclinedPlaneMotion.ts";
import * as ohmLaw from "./ohmLaw.ts";
import { PhysicsEngine } from "./physicsEngine.ts";
import * as projectileMotion from "./projectileMotion.ts";
import * as refractionAtBoundary from "./refractionAtBoundary.ts";
import * as resistorsConnection from "./resistorsConnection.ts";
import * as springOscillator from "./springOscillator.ts";
import * as thinLens from "./thinLens.ts";

function close(actual: number, expected: number): void {
  const scale = Math.max(1, Math.abs(actual), Math.abs(expected));
  expect(Math.abs(actual - expected)).toBeLessThanOrEqual(scale * 1e-12);
}

describe("physics vs C# gold", () => {
  it("free fall", () => {
    close(freeFall.timeOfFall(4.905), gold["freeFall.timeOfFall.4.905"]);
    close(freeFall.finalSpeed(10), gold["freeFall.finalSpeed.10"]);
    close(freeFall.heightAt(1, 10), gold["freeFall.heightAt.1_10"]);
    close(freeFall.velocityAt(1.5), gold["freeFall.velocityAt.1.5"]);
    expect(freeFall.getHeightPoints(10)).toHaveLength(gold["freeFall.heightPoints.len"]);
    close(freeFall.getHeightPoints(10).at(-1)!.h, gold["freeFall.heightPoints.lastH"]);
    close(freeFall.timeOfFall(10, 0), freeFall.timeOfFall(10));
    close(freeFall.finalSpeed(10, 0), freeFall.finalSpeed(10));
    expect(freeFall.timeOfFall(20, 5)).toBeGreaterThan(0);
    expect(freeFall.finalSpeed(20, 5)).toBeGreaterThan(freeFall.finalSpeed(20, 0));
  });

  it("projectile", () => {
    close(projectileMotion.timeOfFlight(20, 45), gold["projectile.timeOfFlight.20_45"]);
    close(projectileMotion.range(20, 45), gold["projectile.range.20_45"]);
    close(projectileMotion.maxHeight(20, 45), gold["projectile.maxHeight.20_45"]);
    close(projectileMotion.timeOfFlight(15, 30, 2), gold["projectile.timeOfFlight.15_30_2"]);
    const p = projectileMotion.positionAt(20, 45, 0.5);
    close(p.x, gold["projectile.pos.20_45_0.5.x"]);
    close(p.y, gold["projectile.pos.20_45_0.5.y"]);
    expect(projectileMotion.getTrajectoryPoints(20, 45, 0)).toHaveLength(
      gold["projectile.traj.len"],
    );
  });

  it("spring", () => {
    close(springOscillator.omega(40, 1), gold["spring.omega.40_1"]);
    close(springOscillator.period(40, 1), gold["spring.period.40_1"]);
    close(springOscillator.frequency(40, 1), gold["spring.freq.40_1"]);
    close(springOscillator.positionAt(40, 1, 0.1, 0, 0.2), gold["spring.x.40_1_0.1_0_0.2"]);
    close(springOscillator.velocityAt(40, 1, 0.1, 0, 0.2), gold["spring.v.40_1_0.1_0_0.2"]);
    close(springOscillator.accelerationAt(40, 1, 0.05), gold["spring.a.40_1_0.05"]);
  });

  it("inclined plane forces and engine", () => {
    close(forceCalculator.weight(2), gold["force.weight.2"]);
    close(forceCalculator.forceAlongPlane(2, 30), gold["force.along.2_30"]);
    close(forceCalculator.normalForce(2, 30), gold["force.normal.2_30"]);
    close(forceCalculator.acceleration(2, 30, 0.1), gold["force.accel.2_30_0.1"]);
    close(forceCalculator.acceleration(2, 10, 0.8), gold["force.accel.rest.2_10_0.8"]);
    expect(forceCalculator.compute(2, 10, 0.8).isAtRest).toBe(gold["force.compute.atRest"]);

    const plane = new InclinedPlane();
    plane.angleDegrees = 30;
    const block = new Block();
    block.massKg = 2;
    block.positionAlongPlane = 0.8;
    block.velocityAlongPlane = 0;
    const engine = new PhysicsEngine(plane, block, 0.1);
    engine.step(0.04);
    engine.step(0.04);
    close(engine.block.positionAlongPlane, gold["engine.pos"]);
    close(engine.block.velocityAlongPlane, gold["engine.vel"]);
    expect(engine.reachedBottom).toBe(gold["engine.reached"]);
  });

  it("inclined plane length and v0 motion", () => {
    close(inclinedPlaneMotion.metersFromNorm(inclinedPlaneMotion.planeUMin, 2), 0);
    close(inclinedPlaneMotion.metersFromNorm(inclinedPlaneMotion.planeUMax, 2), 2);
    close(inclinedPlaneMotion.metersFromNorm(0.5, 2), 1);
    close(inclinedPlaneMotion.normFromMeters(1, 2), 0.5);
    close(inclinedPlaneMotion.signedAccel(30, 0.5), forceCalculator.acceleration(2, 30, 0.5));
    expect(inclinedPlaneMotion.willSlide(0, 10, 0.8)).toBe(false);
    expect(inclinedPlaneMotion.willSlide(1, 10, 0.8)).toBe(true);
    const curves = inclinedPlaneMotion.motionCurves(1, 0, 2, 30, 0.5);
    expect(curves.tMax).toBeGreaterThan(1.6);
    expect(curves.tMax).toBeLessThan(1.9);
    expect(Math.abs(curves.path.at(-1)!.y - 1)).toBeLessThan(0.05);
    expect(curves.vel.at(-1)!.y).toBeGreaterThan(1);
    expect(inclinedPlaneMotion.motionCurves(1, 0, 2, 10, 0.8).tMax).toBe(0);
  });

  it("buoyancy", () => {
    close(buoyancy.archimedesForce(1000, 0.002), gold["buoyancy.FA"]);
    close(buoyancy.weight(500, 0.002), gold["buoyancy.weight"]);
    close(buoyancy.submergedFraction(500, 1000), gold["buoyancy.frac.500_1000"]);
    close(buoyancy.submergedFraction(1200, 1000), gold["buoyancy.frac.1200_1000"]);
    expect(buoyancy.isFloating(500, 1000)).toBe(gold["buoyancy.float"]);
    expect(buoyancy.isSinking(1200, 1000)).toBe(gold["buoyancy.sink"]);
    expect(buoyancy.isNeutralEquilibrium(1000, 1000)).toBe(gold["buoyancy.neutral"]);
  });

  it("communicating vessels", () => {
    close(communicatingVessels.levelHeight(0.004, 0.02, 0.02), gold["vessels.h"]);
    close(communicatingVessels.pressureAtDepth(1000, 0.1), gold["vessels.p"]);
    close(communicatingVessels.volumeFromLevel(0.1, 0.02, 0.03), gold["vessels.V"]);
    close(communicatingVessels.heightLightFromHeavy(800, 1000, 0.1), gold["vessels.hLight"]);
  });

  it("ideal gas", () => {
    close(idealGas.pressure(1, 300, 0.024), gold["gas.P"]);
    close(idealGas.volume(1, 300, 101325), gold["gas.V"]);
    close(idealGas.temperature(101325, 0.024465, 1), gold["gas.T"]);
    expect(idealGas.isothermPoints(1, 300, 0.01, 0.05, 80)).toHaveLength(gold["gas.iso.len"]);
  });

  it("ohm and resistors", () => {
    close(ohmLaw.currentClosed(12, 1, 5), gold["ohm.I"]);
    close(ohmLaw.emf(2, 1, 5), gold["ohm.E"]);
    close(ohmLaw.internalResistance(12, 2, 5), gold["ohm.r"]);
    close(ohmLaw.externalResistance(12, 2, 1), gold["ohm.R"]);
    close(ohmLaw.voltageExternal(2, 5), gold["ohm.UR"]);
    close(ohmLaw.powerExternal(2, 5), gold["ohm.P"]);
    close(resistorsConnection.seriesEquivalent(10, 20, 30), gold["res.series3"]);
    close(resistorsConnection.parallelEquivalent(10, 20, 30), gold["res.parallel3"]);
    close(resistorsConnection.seriesEquivalent(10, 20), gold["res.series2"]);
    close(resistorsConnection.parallelEquivalent(10, 20), gold["res.parallel2"]);
    close(resistorsConnection.current(12, 6), gold["res.I"]);
  });

  it("charge in field", () => {
    close(chargeInField.accelerationMagnitude(1.6e-19, 200, 9.1e-31), gold["charge.a"]);
    close(chargeInField.accelerationY(1.6e-19, 200, 9.1e-31), gold["charge.ay"]);
    close(chargeInField.timeOfFlight(1e6, 40, 1.6e-19, 200, 9.1e-31), gold["charge.T"]);
    close(chargeInField.range(1e6, 40, 1.6e-19, 200, 9.1e-31), gold["charge.R"]);
    close(chargeInField.maxHeight(1e6, 40, 1.6e-19, 200, 9.1e-31), gold["charge.H"]);
  });

  it("thin lens", () => {
    close(thinLens.imageDistance(0.3, 0.1), gold["lens.d"]);
    close(thinLens.magnification(0.3, 0.1), gold["lens.G"]);
    close(thinLens.imageHeight(0.05, 0.3, 0.1), gold["lens.h"]);
    expect(thinLens.isInverted(0.3, 0.1)).toBe(gold["lens.inv"]);
    expect(thinLens.isRealImage(0.3, 0.1)).toBe(gold["lens.real"]);
    expect(thinLens.isRealImage(0.08, 0.1)).toBe(gold["lens.virtual"]);
    expect(Number.isNaN(thinLens.imageDistance(0.1, 0.1))).toBe(true);
  });

  it("refraction", () => {
    close(refractionAtBoundary.refractionAngleDegrees(1.0, 1.5, 30), gold["refr.theta"]);
    close(refractionAtBoundary.criticalAngleDegrees(1.5, 1.0)!, gold["refr.crit"]);
    expect(refractionAtBoundary.isTotalInternalReflection(1.5, 1.0, 50)).toBe(gold["refr.tir"]);
    expect(Number.isNaN(refractionAtBoundary.refractionAngleDegrees(1.5, 1.0, 50))).toBe(true);
    expect(refractionAtBoundary.criticalAngleDegrees(1.0, 1.5)).toBeNull();
  });

  it("harmonic oscillations", () => {
    close(harmonicOscillations.positionAt(0.1, 4, 30, 0.5), gold["harm.x"]);
    close(harmonicOscillations.velocityAt(0.1, 4, 30, 0.5), gold["harm.v"]);
    close(harmonicOscillations.accelerationAt(0.1, 4, 30, 0.5), gold["harm.a"]);
    close(harmonicOscillations.period(4), gold["harm.T"]);
    close(harmonicOscillations.frequency(4), gold["harm.f"]);
    expect(harmonicOscillations.getChartPoints(0.1, 4, 30)).toHaveLength(gold["harm.chart.len"]);
  });
});
