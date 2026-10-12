import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { FallCanvas } from "./FallCanvas.tsx";
import { useFreeFall } from "./useFreeFall.ts";

export function FreeFallLab() {
  const lab = useFreeFall();

  return (
    <LabShell
      title="свободное падение"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          h(t) = h₀ − v₀t − gt²/2
          <br />
          v(t) = v₀ + gt
          <br />
          T = (−v₀ + √(v₀² + 2gh₀))/g
        </p>
      }
      side={
        <>
          <LabField label="h₀" value={lab.height0} digits={1} onCommit={lab.setHeight0} hint="Начальная высота тела над поверхностью, м" />
          <LabField label="m" value={lab.massKg} digits={2} onCommit={lab.setMassKg} hint="Масса тела, кг. В этой модели T и v от массы не зависят" />
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
              <dd>{lab.timeOfFall.toFixed(2)} с</dd>
              <LabHint text="Время до поверхности, с" />
            </div>
            <div>
              <dt>v</dt>
              <dd>{lab.currentSpeed.toFixed(2)} м/с</dd>
              <LabHint text="Текущая скорость вниз, м/с" />
            </div>
            <div>
              <dt>удар</dt>
              <dd>{lab.finalSpeed.toFixed(2)} м/с</dd>
              <LabHint text="Скорость в момент приземления, м/с" />
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
                <LabHint text="Наибольшая скорость за пробег (у земли), м/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время движения до поверхности, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <FallCanvas
          height0={lab.height0}
          currentHeight={lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentHeight : lab.height0}
          massKg={lab.massKg}
          scaleH={lab.scaleH}
          dragEnabled={!lab.isPlaying}
          onPlace={lab.placeHeight}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.heightPoints.map((p) => ({ t: p.t, y: p.h }))}
            markerT={lab.currentTime}
            markerY={lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentHeight : lab.height0}
            title="h(t)"
            xLabel="t, с"
            yLabel="м"
          />
          <LabChart
            points={lab.velocityPoints.map((p) => ({ t: p.t, y: p.v }))}
            markerT={lab.currentTime}
            markerY={lab.runSummary && !lab.isPlaying ? (lab.velocityPoints.at(-1)?.v ?? lab.currentSpeed) : lab.currentSpeed}
            title="v(t)"
            xLabel="t, с"
            yLabel="м/с"
          />
        </>
      }
    />
  );
}
