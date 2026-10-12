using System.Globalization;
using System.Text.Json;
using WPFPhisics1._1.Physics;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var plane = new InclinedPlane { AngleDegrees = 30 };
var block = new Block { MassKg = 2, PositionAlongPlane = 0.8, VelocityAlongPlane = 0 };
var engine = new PhysicsEngine(plane, block, 0.1);
engine.Step(0.04);
engine.Step(0.04);

var gold = new Dictionary<string, object?>
{
    ["freeFall.timeOfFall.4.905"] = FreeFall.TimeOfFall(4.905),
    ["freeFall.finalSpeed.10"] = FreeFall.FinalSpeed(10),
    ["freeFall.heightAt.1_10"] = FreeFall.HeightAt(1, 10),
    ["freeFall.velocityAt.1.5"] = FreeFall.VelocityAt(1.5),
    ["freeFall.heightPoints.len"] = FreeFall.GetHeightPoints(10).Count,
    ["freeFall.heightPoints.lastH"] = FreeFall.GetHeightPoints(10)[^1].H,
    ["projectile.timeOfFlight.20_45"] = ProjectileMotion.TimeOfFlight(20, 45),
    ["projectile.range.20_45"] = ProjectileMotion.Range(20, 45),
    ["projectile.maxHeight.20_45"] = ProjectileMotion.MaxHeight(20, 45),
    ["projectile.timeOfFlight.15_30_2"] = ProjectileMotion.TimeOfFlight(15, 30, 2),
    ["projectile.pos.20_45_0.5.x"] = ProjectileMotion.PositionAt(20, 45, 0.5).X,
    ["projectile.pos.20_45_0.5.y"] = ProjectileMotion.PositionAt(20, 45, 0.5).Y,
    ["projectile.traj.len"] = ProjectileMotion.GetTrajectoryPoints(20, 45, 0).Count,
    ["spring.omega.40_1"] = SpringOscillator.Omega(40, 1),
    ["spring.period.40_1"] = SpringOscillator.Period(40, 1),
    ["spring.freq.40_1"] = SpringOscillator.Frequency(40, 1),
    ["spring.x.40_1_0.1_0_0.2"] = SpringOscillator.PositionAt(40, 1, 0.1, 0, 0.2),
    ["spring.v.40_1_0.1_0_0.2"] = SpringOscillator.VelocityAt(40, 1, 0.1, 0, 0.2),
    ["spring.a.40_1_0.05"] = SpringOscillator.AccelerationAt(40, 1, 0.05),
    ["force.weight.2"] = ForceCalculator.Weight(2),
    ["force.along.2_30"] = ForceCalculator.ForceAlongPlane(2, 30),
    ["force.normal.2_30"] = ForceCalculator.NormalForce(2, 30),
    ["force.accel.2_30_0.1"] = ForceCalculator.Acceleration(2, 30, 0.1),
    ["force.accel.rest.2_10_0.8"] = ForceCalculator.Acceleration(2, 10, 0.8),
    ["force.compute.atRest"] = ForceCalculator.Compute(2, 10, 0.8).IsAtRest,
    ["engine.pos"] = engine.Block.PositionAlongPlane,
    ["engine.vel"] = engine.Block.VelocityAlongPlane,
    ["engine.reached"] = engine.ReachedBottom,
    ["buoyancy.FA"] = Buoyancy.ArchimedesForce(1000, 0.002),
    ["buoyancy.weight"] = Buoyancy.Weight(500, 0.002),
    ["buoyancy.frac.500_1000"] = Buoyancy.SubmergedFraction(500, 1000),
    ["buoyancy.frac.1200_1000"] = Buoyancy.SubmergedFraction(1200, 1000),
    ["buoyancy.float"] = Buoyancy.IsFloating(500, 1000),
    ["buoyancy.sink"] = Buoyancy.IsSinking(1200, 1000),
    ["buoyancy.neutral"] = Buoyancy.IsNeutralEquilibrium(1000, 1000),
    ["vessels.h"] = CommunicatingVessels.LevelHeight(0.004, 0.02, 0.02),
    ["vessels.p"] = CommunicatingVessels.PressureAtDepth(1000, 0.1),
    ["vessels.V"] = CommunicatingVessels.VolumeFromLevel(0.1, 0.02, 0.03),
    ["vessels.hLight"] = CommunicatingVessels.HeightLightFromHeavy(800, 1000, 0.1),
    ["gas.P"] = IdealGas.Pressure(1, 300, 0.024),
    ["gas.V"] = IdealGas.Volume(1, 300, 101325),
    ["gas.T"] = IdealGas.Temperature(101325, 0.024465, 1),
    ["gas.iso.len"] = IdealGas.IsothermPoints(1, 300, 0.01, 0.05, 80).Count,
    ["ohm.I"] = OhmLaw.CurrentClosed(12, 1, 5),
    ["ohm.E"] = OhmLaw.Emf(2, 1, 5),
    ["ohm.r"] = OhmLaw.InternalResistance(12, 2, 5),
    ["ohm.R"] = OhmLaw.ExternalResistance(12, 2, 1),
    ["ohm.UR"] = OhmLaw.VoltageExternal(2, 5),
    ["ohm.P"] = OhmLaw.PowerExternal(2, 5),
    ["res.series3"] = ResistorsConnection.SeriesEquivalent(10, 20, 30),
    ["res.parallel3"] = ResistorsConnection.ParallelEquivalent(10, 20, 30),
    ["res.series2"] = ResistorsConnection.SeriesEquivalent(10, 20),
    ["res.parallel2"] = ResistorsConnection.ParallelEquivalent(10, 20),
    ["res.I"] = ResistorsConnection.Current(12, 6),
    ["charge.a"] = ChargeInField.AccelerationMagnitude(1.6e-19, 200, 9.1e-31),
    ["charge.ay"] = ChargeInField.AccelerationY(1.6e-19, 200, 9.1e-31),
    ["charge.T"] = ChargeInField.TimeOfFlight(1e6, 40, 1.6e-19, 200, 9.1e-31),
    ["charge.R"] = ChargeInField.Range(1e6, 40, 1.6e-19, 200, 9.1e-31),
    ["charge.H"] = ChargeInField.MaxHeight(1e6, 40, 1.6e-19, 200, 9.1e-31),
    ["lens.d"] = ThinLens.ImageDistance(0.3, 0.1),
    ["lens.G"] = ThinLens.Magnification(0.3, 0.1),
    ["lens.h"] = ThinLens.ImageHeight(0.05, 0.3, 0.1),
    ["lens.inv"] = ThinLens.IsInverted(0.3, 0.1),
    ["lens.real"] = ThinLens.IsRealImage(0.3, 0.1),
    ["lens.virtual"] = ThinLens.IsRealImage(0.08, 0.1),
    ["refr.theta"] = RefractionAtBoundary.RefractionAngleDegrees(1.0, 1.5, 30),
    ["refr.crit"] = RefractionAtBoundary.CriticalAngleDegrees(1.5, 1.0),
    ["refr.tir"] = RefractionAtBoundary.IsTotalInternalReflection(1.5, 1.0, 50),
    ["harm.x"] = HarmonicOscillations.PositionAt(0.1, 4, 30, 0.5),
    ["harm.v"] = HarmonicOscillations.VelocityAt(0.1, 4, 30, 0.5),
    ["harm.a"] = HarmonicOscillations.AccelerationAt(0.1, 4, 30, 0.5),
    ["harm.T"] = HarmonicOscillations.Period(4),
    ["harm.f"] = HarmonicOscillations.Frequency(4),
    ["harm.chart.len"] = HarmonicOscillations.GetChartPoints(0.1, 4, 30).Count,
};

var json = JsonSerializer.Serialize(gold, new JsonSerializerOptions
{
    WriteIndented = true,
    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
});
Console.Write(json);
