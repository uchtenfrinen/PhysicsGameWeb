import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { VesselsCanvas } from "./VesselsCanvas.tsx";
import { useVessels } from "./useVessels.ts";

export function VesselsLab() {
  const lab = useVessels();

  return (
    <LabShell
      title="сообщающиеся сосуды"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          ρ₁ h₁ = ρ₂ h₂
          <br />
          h₁/h₂ = ρ₂/ρ₁
          <br />
          p = ρ₂ g h₂
        </p>
      }
      side={
        <>
          <LabField
            label="ρ₁"
            value={lab.rhoLight}
            digits={0}
            onCommit={lab.setRhoLight}
            hint="Плотность лёгкой жидкости (масло), кг/м³"
          />
          <LabField
            label="ρ₂"
            value={lab.rhoHeavy}
            digits={0}
            onCommit={lab.setRhoHeavy}
            hint="Плотность тяжёлой жидкости (вода), кг/м³"
          />
          <LabField
            label="h₂"
            value={lab.heightHeavy}
            digits={3}
            onCommit={lab.setHeightHeavy}
            hint="Высота столба воды над границей, м. Можно задать мышью"
          />
          <div className="acts">
            <button type="button" className="act-primary" disabled={!lab.canPlay && !lab.isPlaying} onClick={lab.togglePlay}>
              {lab.isPlaying ? "Остановить" : "Запустить"}
            </button>
            <button type="button" onClick={lab.build}>
              Сбросить
            </button>
            <button type="button" disabled={lab.busy} onClick={() => void lab.explainGroq()}>
              Объяснить
            </button>
          </div>
          <dl className="reads">
            <div>
              <dt>h₁</dt>
              <dd>{lab.heightLight.toFixed(3)} м</dd>
              <LabHint text="Высота столба масла над границей, м. h₁ = h₂·(ρ₂/ρ₁)" />
            </div>
            <div>
              <dt>p</dt>
              <dd>{lab.pressure.toFixed(0)} Па</dd>
              <LabHint text="Давление на границе p = ρ₂ g h₂, Па" />
            </div>
            <div>
              <dt>h₁/h₂</dt>
              <dd>{lab.heightHeavy > 0 ? (lab.heightLight / lab.heightHeavy).toFixed(2) : "—"}</dd>
              <LabHint text="Отношение высот. При равновесии равно ρ₂/ρ₁" />
            </div>
            <div>
              <dd>{lab.motionState}</dd>
            </div>
          </dl>
          {lab.runSummary ? (
            <dl className="reads reads-end">
              <div>
                <dt>vmax</dt>
                <dd>{lab.runSummary.vmax.toFixed(2)} м/с</dd>
                <LabHint text="Наибольшая скорость поверхности за пробег, м/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время налива до равновесия, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <VesselsCanvas
          hLight={lab.hL}
          hHeavy={lab.hH}
          hLightMax={lab.heightLight}
          hHeavyMax={lab.heightHeavy}
          vLight={lab.currentVLight}
          vHeavy={lab.currentVHeavy}
          dragEnabled={!lab.isPlaying}
          onPlaceLight={lab.placeLight}
          onPlaceHeavy={lab.placeHeavy}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.rhoPoints}
            markerT={lab.rhoLight}
            markerY={lab.heightLight}
            title="h₁(ρ₁)"
            xLabel="ρ, кг/м³"
            yLabel="м"
          />
          <LabChart
            points={lab.pPoints}
            markerT={lab.heightHeavy}
            markerY={lab.pressure}
            title="p(h₂)"
            xLabel="h, м"
            yLabel="Па"
          />
        </>
      }
    />
  );
}
