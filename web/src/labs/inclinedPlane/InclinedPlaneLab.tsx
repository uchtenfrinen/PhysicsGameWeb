import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { PlaneCanvas } from "./PlaneCanvas.tsx";
import { useInclinedPlane } from "./useInclinedPlane.ts";

export function InclinedPlaneLab() {
  const lab = useInclinedPlane();

  return (
    <LabShell
      title="наклонная плоскость"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          N = mg cos α
          <br />
          Fтр = μN
          <br />
          a = g(sin α − μ cos α)
        </p>
      }
      side={
        <>
          <LabField label="m" value={lab.massKg} digits={2} onCommit={lab.setMassKg} hint="Масса груза, кг" />
          <LabField label="α" value={lab.angleDegrees} digits={0} onCommit={lab.setAngleDegrees} hint="Угол наклона плоскости к горизонту, градусы" />
          <LabField label="μ" value={lab.frictionMu} digits={2} onCommit={lab.setFrictionMu} hint="Коэффициент трения между грузом и плоскостью" />
          <LabField label="L" value={lab.lengthM} digits={2} onCommit={lab.setLengthM} hint="Длина наклонной плоскости, м" />
          <LabField label="s₀" value={lab.isPlaying ? lab.runS0 : lab.alongM} digits={2} onCommit={lab.setS0} hint="Расстояние груза от основания плоскости, м" />
          <LabField label="v₀" value={lab.v0} digits={2} onCommit={lab.setV0} hint="Начальная скорость вдоль плоскости, м/с. Больше 0 — вниз, меньше 0 — вверх" />
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
              <dt>a</dt>
              <dd>{lab.accelMps2.toFixed(2)} м/с²</dd>
              <LabHint text="Ускорение вдоль плоскости: a = g(sin α − μ cos α)" />
            </div>
            <div>
              <dt>v</dt>
              <dd>{lab.speed.toFixed(2)} м/с</dd>
              <LabHint text="Текущая скорость груза вдоль плоскости, м/с" />
            </div>
            <div>
              <dt>1 м</dt>
              <dd>{lab.timeFor1M == null ? "—" : `${lab.timeFor1M.toFixed(2)} с`}</dd>
              <LabHint text="Время, за которое груз из покоя проходит 1 м" />
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
                <LabHint text="Наибольшая скорость за пробег, м/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время движения до остановки, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <PlaneCanvas
          angleDegrees={lab.angleDegrees}
          positionAlongPlane={lab.position}
          forces={lab.forces}
          dragEnabled={!lab.isPlaying}
          onPlace={lab.placeU}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.curves.path}
            markerT={lab.currentTime}
            markerY={lab.pathM}
            title="s(t)"
            xLabel="t, с"
            yLabel="м"
          />
          <LabChart
            points={lab.curves.vel}
            markerT={lab.currentTime}
            markerY={lab.runSummary && !lab.isPlaying ? (lab.curves.vel.at(-1)?.y ?? lab.speed) : lab.speed}
            title="v(t)"
            xLabel="t, с"
            yLabel="м/с"
          />
        </>
      }
    />
  );
}
