import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { HarmonicCanvas } from "./HarmonicCanvas.tsx";
import { useHarmonic } from "./useHarmonic.ts";

export function HarmonicLab() {
  const lab = useHarmonic();
  const atRest = !lab.isPlaying && lab.currentTime <= 1e-9;
  const showX = atRest ? lab.amplitude * Math.cos((lab.phi0 * Math.PI) / 180) : lab.currentX;
  const showV = atRest ? -lab.amplitude * lab.omega * Math.sin((lab.phi0 * Math.PI) / 180) : lab.currentV;
  const showA = atRest ? -lab.omega * lab.omega * showX : lab.currentA;

  return (
    <LabShell
      title="гармонические колебания"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          x = A cos(ωt + φ₀)
          <br />
          ω = 2π / T
          <br />
          a = −ω² x
        </p>
      }
      side={
        <>
          <LabField
            label="A"
            value={lab.amplitude}
            digits={2}
            onCommit={lab.setAmplitude}
            hint="Амплитуда, м. Можно увеличить мышью — дальше меток ±A"
          />
          <LabField
            label="T"
            value={lab.periodSec}
            digits={2}
            onCommit={lab.setPeriodSec}
            hint="Период колебаний, с. T = 2π/ω"
          />
          <LabField
            label="φ₀"
            value={lab.phi0}
            digits={0}
            onCommit={lab.setPhi0}
            hint="Начальная фаза, °. Можно задать мышью — положение шарика в пределах ±A"
          />
          <LabField
            label="ω"
            value={lab.omega}
            digits={2}
            onCommit={lab.setOmega}
            hint="Циклическая частота, рад/с. ω = 2π/T. Если задать ω, пересчитается T"
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
              <dt>f</dt>
              <dd>{lab.freq.toFixed(2)} Гц</dd>
              <LabHint text="Частота f = 1/T = ω/(2π), Гц" />
            </div>
            <div>
              <dt>x</dt>
              <dd>{showX.toFixed(2)} м</dd>
              <LabHint text="Смещение x = A·cos(ωt + φ₀), м" />
            </div>
            <div>
              <dt>v</dt>
              <dd>{showV.toFixed(2)} м/с</dd>
              <LabHint text="Скорость v = −Aω·sin(ωt + φ₀), м/с" />
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
                <LabHint text="Наибольшая скорость vmax = A·ω, м/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Длительность пробега (два периода), с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <HarmonicCanvas
          x={showX}
          v={showV}
          a={showA}
          amplitude={lab.amplitude}
          dragEnabled={!lab.isPlaying}
          onPlace={lab.placeX}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.posPoints}
            markerT={lab.currentTime}
            markerY={showX}
            title="x(t)"
            xLabel="t, с"
            yLabel="м"
          />
          <LabChart
            points={lab.velPoints}
            markerT={lab.currentTime}
            markerY={lab.runSummary && !lab.isPlaying ? (lab.velPoints.at(-1)?.y ?? showV) : showV}
            title="v(t)"
            xLabel="t, с"
            yLabel="м/с"
          />
        </>
      }
    />
  );
}
