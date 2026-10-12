import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { ChargeCanvas } from "./ChargeCanvas.tsx";
import { useChargeField } from "./useChargeField.ts";

export function ChargeLab() {
  const lab = useChargeField();
  const showY = lab.isPlaying || lab.currentTime > 1e-9 ? lab.currentY : lab.y0;

  return (
    <LabShell
      title="заряд в поле"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          a_y = −qE / m
          <br />
          x = v₀ cos α · t
          <br />
          y = y₀ + v₀ sin α · t + a_y t²/2
        </p>
      }
      side={
        <>
          <LabField label="q" value={lab.q} digits={6} onCommit={lab.setQ} hint="Заряд частицы, Кл. Больше 0 — сила вниз, меньше 0 — вверх" />
          <LabField label="E" value={lab.eField} digits={0} onCommit={lab.setEField} hint="Напряжённость однородного поля, В/м. Поле направлено вниз" />
          <LabField label="m" value={lab.m} digits={6} onCommit={lab.setM} hint="Масса частицы, кг" />
          <LabField label="v₀" value={lab.v0} digits={1} onCommit={lab.setV0} hint="Начальная скорость, м/с" />
          <LabField label="α" value={lab.angleDegrees} digits={0} onCommit={lab.setAngleDegrees} hint="Угол к горизонту, градусы" />
          <LabField
            label="y₀"
            value={lab.y0}
            digits={1}
            onCommit={lab.setY0}
            hint="Начальная высота, м. Можно задать мышью на схеме"
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
              <dt>T</dt>
              <dd>{lab.timeOfFlight.toFixed(2)} с</dd>
              <LabHint text="Время полёта до y = 0, с" />
            </div>
            <div>
              <dt>L</dt>
              <dd>{lab.rangeM.toFixed(2)} м</dd>
              <LabHint text="Дальность по горизонтали, м" />
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
        <ChargeCanvas
          points={lab.trajectory}
          currentX={lab.currentX}
          currentY={showY}
          range={lab.rangeM}
          maxHeight={Math.max(lab.heightM, lab.y0)}
          q={lab.q}
          eField={lab.eField}
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
            markerY={showY}
            title="y(t)"
            xLabel="t, с"
            yLabel="м"
          />
          <LabChart
            points={lab.speedPoints}
            markerT={lab.currentTime}
            markerY={lab.runSummary && !lab.isPlaying ? (lab.speedPoints.at(-1)?.y ?? lab.currentSpeed) : lab.currentSpeed}
            title="v(t)"
            xLabel="t, с"
            yLabel="м/с"
          />
        </>
      }
    />
  );
}
