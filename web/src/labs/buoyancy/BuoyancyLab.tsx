import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { BuoyancyCanvas } from "./BuoyancyCanvas.tsx";
import { useBuoyancy } from "./useBuoyancy.ts";

export function BuoyancyLab() {
  const lab = useBuoyancy();

  return (
    <LabShell
      title="плавание тела"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          F_A = ρж g Vпогр
          <br />
          P = ρт g V
          <br />
          η = ρт / ρж
        </p>
      }
      side={
        <>
          <LabField label="ρт" value={lab.rhoBody} digits={0} onCommit={lab.setRhoBody} hint="Плотность тела, кг/м³" />
          <LabField label="ρж" value={lab.rhoFluid} digits={0} onCommit={lab.setRhoFluid} hint="Плотность жидкости, кг/м³" />
          <LabField label="V" value={lab.volume} digits={4} onCommit={lab.setVolume} hint="Объём тела, м³" />
          <LabField
            label="η₀"
            value={lab.eta0}
            digits={2}
            onCommit={lab.setEta0}
            hint="Начальная доля погружения. 0 — на поверхности. Можно задать мышью"
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
              <dt>P</dt>
              <dd>{lab.weightN.toFixed(2)} Н</dd>
              <LabHint text="Вес тела P = ρт g V, Н" />
            </div>
            <div>
              <dt>F_A</dt>
              <dd>{lab.fa.toFixed(2)} Н</dd>
              <LabHint text="Сила Архимеда в равновесии F_A = ρж g Vпогр, Н" />
            </div>
            <div>
              <dt>η</dt>
              <dd>{(lab.frac * 100).toFixed(1)}%</dd>
              <LabHint text="Доля погружения в равновесии η = min(1, ρт/ρж)" />
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
                <LabHint text="Наибольшая скорость куба за пробег, м/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время движения до равновесия, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <BuoyancyCanvas
          currentFrac={lab.showFrac}
          volume={lab.volume}
          fa={lab.faNow}
          weightN={lab.weightN}
          v={lab.currentV}
          dragEnabled={!lab.isPlaying}
          onPlace={lab.placeFrac}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.etaPoints}
            markerT={lab.rhoBody}
            markerY={lab.frac}
            title="η(ρт)"
            xLabel="ρ, кг/м³"
            yLabel="η"
          />
          <LabChart
            points={lab.faPoints}
            markerT={lab.rhoBody}
            markerY={lab.fa}
            title="F_A(ρт)"
            xLabel="ρ, кг/м³"
            yLabel="Н"
          />
        </>
      }
    />
  );
}
