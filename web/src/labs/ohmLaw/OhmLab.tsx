import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { OhmCanvas } from "./OhmCanvas.tsx";
import { useOhmLaw } from "./useOhmLaw.ts";

export function OhmLab() {
  const lab = useOhmLaw();

  return (
    <LabShell
      title="закон ома"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          I = ε / (r + R)
          <br />
          U_R = I · R
          <br />
          P = I² · R
        </p>
      }
      side={
        <>
          <LabField
            label="ε"
            value={lab.epsilonV}
            digits={1}
            onCommit={lab.setEpsilonV}
            hint="ЭДС источника, В. Можно задать мышью — источник сверху"
          />
          <LabField
            label="r"
            value={lab.internalOhm}
            digits={2}
            onCommit={lab.setInternalOhm}
            hint="Внутреннее сопротивление источника, Ом"
          />
          <LabField
            label="R"
            value={lab.externalOhm}
            digits={1}
            onCommit={lab.setExternalOhm}
            hint="Внешнее сопротивление, Ом. Можно задать мышью — резистор снизу"
          />
          <LabField
            label="I"
            value={lab.currentA}
            digits={3}
            onCommit={lab.setCurrentA}
            hint="Сила тока, А. I = ε/(r+R). Если задать I, пересчитается R"
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
              <dt>U_R</dt>
              <dd>{lab.voltageExternal.toFixed(2)} В</dd>
              <LabHint text="Напряжение на внешнем участке U_R = I·R, В" />
            </div>
            <div>
              <dt>U_r</dt>
              <dd>{lab.voltageInternal.toFixed(2)} В</dd>
              <LabHint text="Падение на источнике U_r = I·r, В" />
            </div>
            <div>
              <dt>P</dt>
              <dd>{lab.powerW.toFixed(2)} Вт</dd>
              <LabHint text="Мощность на внешнем участке P = I²·R, Вт" />
            </div>
            <div>
              <dt>η</dt>
              <dd>{lab.efficiency.toFixed(1)}%</dd>
              <LabHint text="КПД цепи η = U_R/ε · 100%" />
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
                <LabHint text="Сила тока в цепи за пробег, А" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время двух обходов контура, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <OhmCanvas
          epsilonV={lab.epsilonV}
          internalOhm={lab.internalOhm}
          externalOhm={lab.externalOhm}
          currentA={lab.currentA}
          voltageExternal={lab.voltageExternal}
          voltageInternal={lab.voltageInternal}
          phase={lab.phase}
          dragEnabled={!lab.isPlaying}
          onPlaceEpsilon={lab.placeEpsilon}
          onPlaceR={lab.placeR}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.uiPoints}
            markerT={lab.currentA}
            markerY={lab.voltageExternal}
            title="U(I)"
            xLabel="I, А"
            yLabel="В"
          />
          <LabChart
            points={lab.irPoints}
            markerT={lab.externalOhm}
            markerY={lab.currentA}
            title="I(R)"
            xLabel="R, Ом"
            yLabel="А"
          />
        </>
      }
    />
  );
}
