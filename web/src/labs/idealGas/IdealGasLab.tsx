import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { GasCanvas } from "./GasCanvas.tsx";
import { useIdealGas } from "./useIdealGas.ts";

export function IdealGasLab() {
  const lab = useIdealGas();

  return (
    <LabShell
      title="идеальный газ"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          PV = νRT
          <br />
          P = νRT / V
          <br />
          T = const
        </p>
      }
      side={
        <>
          <LabField label="ν" value={lab.n} digits={2} onCommit={lab.setN} hint="Количество вещества, моль" />
          <LabField
            label="T"
            value={lab.temperature}
            digits={0}
            onCommit={lab.setTemperature}
            hint="Температура, К. Изотерма: T постоянна"
          />
          <LabField
            label="V"
            value={lab.volume}
            digits={4}
            onCommit={lab.setVolume}
            hint="Объём газа, м³. Можно задать мышью — поршень"
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
              <dd>{lab.currentP.toFixed(0)} Па</dd>
              <LabHint text="Давление P = νRT/V, Па" />
            </div>
            <div>
              <dt>V</dt>
              <dd>{lab.currentV.toFixed(4)} м³</dd>
              <LabHint text="Текущий объём газа, м³" />
            </div>
            <div>
              <dd>{lab.motionState}</dd>
            </div>
          </dl>
          {lab.runSummary ? (
            <dl className="reads reads-end">
              <div>
                <dt>vmax</dt>
                <dd>{lab.runSummary.vmax.toFixed(4)} м³/с</dd>
                <LabHint text="Наибольшая |dV/dt| за цикл, м³/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Длительность пробега (один цикл), с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <GasCanvas
          currentV={lab.currentV}
          vMax={lab.vMax}
          currentP={lab.currentP}
          vDot={lab.currentVDot}
          dragEnabled={!lab.isPlaying}
          onPlace={lab.placeVolume}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.isoPoints}
            markerT={lab.currentV}
            markerY={lab.currentP}
            title="P(V)"
            xLabel="V, м³"
            yLabel="Па"
          />
          <LabChart
            points={lab.vTimePoints}
            markerT={lab.currentTime}
            markerY={lab.currentV}
            title="V(t)"
            xLabel="t, с"
            yLabel="м³"
          />
        </>
      }
    />
  );
}
