import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { ResistorsCanvas } from "./ResistorsCanvas.tsx";
import { useResistors } from "./useResistors.ts";

export function ResistorsLab() {
  const lab = useResistors();

  return (
    <LabShell
      title="резисторы"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          {lab.isSeries ? (
            <>
              R = R1 + R2 + R3
              <br />
              I = U / R
              <br />
              U = U1 + U2 + U3
            </>
          ) : (
            <>
              1/R = 1/R1 + 1/R2 + 1/R3
              <br />
              I = I1 + I2 + I3
              <br />
              U = U1 = U2 = U3
            </>
          )}
        </p>
      }
      side={
        <>
          <LabField
            label="U"
            value={lab.voltageV}
            digits={1}
            onCommit={lab.setVoltageV}
            hint="Напряжение источника, В"
          />
          <LabField
            label="R1"
            value={lab.r1}
            digits={1}
            onCommit={lab.setR1}
            hint="Сопротивление первого резистора, Ом. Можно задать мышью"
          />
          <LabField
            label="R2"
            value={lab.r2}
            digits={1}
            onCommit={lab.setR2}
            hint="Сопротивление второго резистора, Ом. Можно задать мышью"
          />
          <LabField
            label="R3"
            value={lab.r3}
            digits={1}
            onCommit={lab.setR3}
            hint="Сопротивление третьего резистора, Ом. Можно задать мышью"
          />
          <LabField
            label="I"
            value={lab.currentA}
            digits={3}
            onCommit={lab.setCurrentA}
            hint="Общий ток, А. I = U/R. Если задать I, пересчитается U"
          />
          <div className="acts">
            <button
              type="button"
              className={lab.isSeries ? "act-primary" : undefined}
              onClick={() => lab.setSeries(true)}
            >
              последовательно
            </button>
            <button
              type="button"
              className={lab.isSeries ? undefined : "act-primary"}
              onClick={() => lab.setSeries(false)}
            >
              параллельно
            </button>
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
              <dt>R</dt>
              <dd>{lab.rTotal.toFixed(2)} Ом</dd>
              <LabHint text="Эквивалентное сопротивление цепи, Ом" />
            </div>
            <div>
              <dt>P</dt>
              <dd>{lab.powerW.toFixed(2)} Вт</dd>
              <LabHint text="Мощность P = U·I, Вт" />
            </div>
            <div>
              <dt>U1</dt>
              <dd>{lab.u1.toFixed(2)} В</dd>
              <LabHint text={lab.isSeries ? "Напряжение на R1, U1 = I·R1" : "Напряжение на ветви, равно U"} />
            </div>
            <div>
              <dd>{lab.motionState}</dd>
            </div>
          </dl>
          {lab.runSummary ? (
            <dl className="reads reads-end">
              <div>
                <dt>vmax</dt>
                <dd>{lab.runSummary.vmax.toFixed(3)} А</dd>
                <LabHint text="Общий ток в цепи за пробег, А" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время двух обходов цепи, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <ResistorsCanvas
          isSeries={lab.isSeries}
          r1={lab.r1}
          r2={lab.r2}
          r3={lab.r3}
          i1={lab.i1}
          i2={lab.i2}
          i3={lab.i3}
          u1={lab.u1}
          u2={lab.u2}
          u3={lab.u3}
          currentA={lab.currentA}
          phase={lab.phase}
          dragEnabled={!lab.isPlaying}
          onPlaceR={lab.placeR}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.iuPoints}
            markerT={lab.voltageV}
            markerY={lab.currentA}
            title="I(U)"
            xLabel="U, В"
            yLabel="А"
          />
          <LabChart
            points={lab.puPoints}
            markerT={lab.voltageV}
            markerY={lab.powerW}
            title="P(U)"
            xLabel="U, В"
            yLabel="Вт"
          />
        </>
      }
    />
  );
}
