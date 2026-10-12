import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { SpringCanvas } from "./SpringCanvas.tsx";
import { useSpring } from "./useSpring.ts";

export function SpringLab() {
  const lab = useSpring();
  const showX = lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentX : lab.x0;
  const showV = lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentV : lab.v0;

  return (
    <LabShell
      title="пружина с грузом"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          F = −kx
          <br />
          ω = √(k/m)
          <br />
          x(t) = x₀ cos(ωt) + (v₀/ω) sin(ωt)
        </p>
      }
      side={
        <>
          <LabField label="k" value={lab.k} digits={1} onCommit={lab.setK} hint="Жёсткость пружины, Н/м" />
          <LabField label="m" value={lab.m} digits={2} onCommit={lab.setM} hint="Масса груза, кг" />
          <LabField label="x₀" value={lab.x0} digits={2} onCommit={lab.setX0} hint="Начальное смещение от равновесия, м. Больше 0 — вниз. Можно задать мышью" />
          <LabField label="v₀" value={lab.v0} digits={2} onCommit={lab.setV0} hint="Начальная скорость, м/с. Больше 0 — вниз, меньше 0 — вверх" />
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
              <dt>T</dt>
              <dd>{lab.periodVal.toFixed(2)} с</dd>
              <LabHint text="Период колебаний T = 2π√(m/k), с" />
            </div>
            <div>
              <dt>ω</dt>
              <dd>{lab.omegaVal.toFixed(2)} рад/с</dd>
              <LabHint text="Циклическая частота ω = √(k/m), рад/с" />
            </div>
            <div>
              <dt>x</dt>
              <dd>{showX.toFixed(2)} м</dd>
              <LabHint text="Текущее смещение от равновесия, м" />
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
                <LabHint text="Наибольшая скорость за два периода, м/с" />
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
        <SpringCanvas
          x={showX}
          v={showV}
          k={lab.k}
          m={lab.m}
          dragEnabled={!lab.isPlaying}
          onPlace={lab.placeX}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.posPoints.map((p) => ({ t: p.t, y: p.x }))}
            markerT={lab.currentTime}
            markerY={showX}
            title="x(t)"
            xLabel="t, с"
            yLabel="м"
          />
          <LabChart
            points={lab.velPoints.map((p) => ({ t: p.t, y: p.v }))}
            markerT={lab.currentTime}
            markerY={lab.runSummary && !lab.isPlaying ? (lab.velPoints.at(-1)?.v ?? showV) : showV}
            title="v(t)"
            xLabel="t, с"
            yLabel="м/с"
          />
        </>
      }
    />
  );
}
