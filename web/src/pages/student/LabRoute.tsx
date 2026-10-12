import type { ComponentType } from "react";
import { Navigate, useParams } from "react-router-dom";
import { BuoyancyLab } from "../../labs/buoyancy/BuoyancyLab.tsx";
import { ChargeLab } from "../../labs/chargeField/ChargeLab.tsx";
import { FreeFallLab } from "../../labs/freeFall/FreeFallLab.tsx";
import { HarmonicLab } from "../../labs/harmonic/HarmonicLab.tsx";
import { IdealGasLab } from "../../labs/idealGas/IdealGasLab.tsx";
import { InclinedPlaneLab } from "../../labs/inclinedPlane/InclinedPlaneLab.tsx";
import { OhmLab } from "../../labs/ohmLaw/OhmLab.tsx";
import { ProjectileLab } from "../../labs/projectile/ProjectileLab.tsx";
import { RefractionLab } from "../../labs/refraction/RefractionLab.tsx";
import { ResistorsLab } from "../../labs/resistors/ResistorsLab.tsx";
import { SpringLab } from "../../labs/spring/SpringLab.tsx";
import { ThinLensLab } from "../../labs/thinLens/ThinLensLab.tsx";
import { VesselsLab } from "../../labs/vessels/VesselsLab.tsx";

const LABS: Record<string, ComponentType> = {
  free_fall: FreeFallLab,
  inclined_plane: InclinedPlaneLab,
  projectile: ProjectileLab,
  spring: SpringLab,
  buoyancy: BuoyancyLab,
  communicating_vessels: VesselsLab,
  ideal_gas: IdealGasLab,
  ohm_law: OhmLab,
  resistors_connection: ResistorsLab,
  charge_in_field: ChargeLab,
  thin_lens: ThinLensLab,
  refraction_at_boundary: RefractionLab,
  harmonic_oscillations: HarmonicLab,
};

export function LabRoute() {
  const { setupId } = useParams();
  const Lab = setupId ? LABS[setupId] : undefined;
  if (!Lab) return <Navigate to="/student/labs" replace />;
  return <Lab />;
}
