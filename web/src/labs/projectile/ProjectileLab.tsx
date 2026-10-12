import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { TrajectoryCanvas } from "./TrajectoryCanvas.tsx";
import { useProjectile } from "./useProjectile.ts";

export function ProjectileLab() {
  const lab = useProjectile();

  return (
    <LabShell
      title="бросок под углом"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          x = v₀ cos α · t
          <br />
          y = y₀ + v₀ sin α · t − gt²/2
          <br />
          T = (v₀y + √(v₀y² + 2gy₀))/g
        </p>
      }
      side={
        <>
          <LabField label="v₀" value={lab.v0} digits={1} onCommit={lab.setV0} hint="Начальная скорость броска, м/с" />
          <LabField label="α" value={lab.angleDegrees} digits={0} onCommit={lab.setAngleDegrees} hint="Угол броска к горизонту, градусы" />
          <LabField label="y₀" value={lab.y0} digits={1} onCommit={lab.setY0} hint="Начальная высота тела, м. Можно задать мышью на схеме" />
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
              <dd>{lab.timeOfFlight.toFixed(2)} с</dd>
              <LabHint text="Время полёта до поверхности, с" />
            </div>
            <div>
              <dt>L</dt>
              <dd>{lab.rangeM.toFixed(2)} м</dd>
              <LabHint text="Дальность полёта по горизонтали, м" />
            </div>
            <div>
              <dt>H</dt>
              <dd>{lab.heightM.toFixed(2)} м</dd>
              <LabHint text="Наибольшая высота траектории, м" />
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
                <LabHint text="Наибольшая скорость за полёт, м/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время полёта до поверхности, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <TrajectoryCanvas
          points={lab.trajectory}
          currentX={lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentX : 0}
          currentY={lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentY : lab.y0}
          range={lab.rangeM}
          maxHeight={Math.max(lab.heightM, lab.y0)}
          vx={lab.currentVx}
          vy={lab.currentVy}
          dragEnabled={!lab.isPlaying}
          onPlaceY={lab.placeY0}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.heightPoints}
            markerT={lab.currentTime}
            markerY={lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentY : lab.y0}
            title="y(t)"
            xLabel="t, с"
            yLabel="м"
          />
          <LabChart
            points={lab.speedPoints.map((p) => ({ t: p.t, y: p.v }))}
            markerT={lab.currentTime}
            markerY={lab.runSummary && !lab.isPlaying ? (lab.speedPoints.at(-1)?.v ?? lab.currentSpeed) : lab.currentSpeed}
            title="v(t)"
            xLabel="t, с"
            yLabel="м/с"
          />
        </>
      }
    />
  );
}
