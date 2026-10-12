import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { LensCanvas } from "./LensCanvas.tsx";
import { useThinLens } from "./useThinLens.ts";

export function ThinLensLab() {
  const lab = useThinLens();
  const dPrimeOk = Number.isFinite(lab.dPrime);
  const dPrimeText = dPrimeOk ? `${lab.dPrime.toFixed(1)} см` : "—";

  return (
    <LabShell
      title="тонкая линза"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          1/f = 1/d + 1/d'
          <br />
          d' = f d / (d − f)
          <br />
          Г = −d' / d
        </p>
      }
      side={
        <>
          <LabField
            label="d"
            value={lab.d}
            digits={1}
            onCommit={lab.setD}
            hint="Расстояние от предмета до линзы, см. Можно задать мышью"
          />
          <LabField
            label="f"
            value={lab.f}
            digits={1}
            onCommit={lab.setF}
            hint="Фокусное расстояние, см. Можно задать мышью — точки F и F'"
          />
          <LabField
            label="h"
            value={lab.objectHeight}
            digits={1}
            onCommit={lab.setObjectHeight}
            hint="Высота предмета, см. Можно задать мышью"
          />
          {dPrimeOk ? (
            <LabField
              label="d'"
              value={lab.dPrime}
              digits={1}
              onCommit={lab.setDPrime}
              hint="Расстояние до изображения, см. Если задать d', пересчитается d"
            />
          ) : null}
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
              <dt>d'</dt>
              <dd>{dPrimeText}</dd>
              <LabHint text="Расстояние от линзы до изображения. d' = f d / (d − f)" />
            </div>
            <div>
              <dt>Г</dt>
              <dd>{lab.mag.toFixed(2)}</dd>
              <LabHint text="Увеличение Г = −d'/d. Меньше 0 — перевёрнутое" />
            </div>
            <div>
              <dt>h'</dt>
              <dd>{lab.imageH.toFixed(2)} см</dd>
              <LabHint text="Высота изображения h' = |Г|·h, см" />
            </div>
            <div>
              <dd>{lab.character}</dd>
            </div>
            <div>
              <dd>{lab.motionState}</dd>
            </div>
          </dl>
          {lab.runSummary ? (
            <dl className="reads reads-end">
              <div>
                <dt>vmax</dt>
                <dd>{lab.runSummary.vmax.toFixed(1)} см/с</dd>
                <LabHint text="Скорость маркера по центральному лучу на схеме, см/с" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время построения хода лучей, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <LensCanvas
          d={lab.d}
          f={lab.f}
          objectHeight={lab.objectHeight}
          dPrime={lab.dPrime}
          imageH={lab.imageH}
          real={lab.real}
          progress={lab.progress}
          playing={lab.isPlaying}
          dragEnabled={!lab.isPlaying}
          onPlaceObject={lab.placeObject}
          onPlaceF={lab.placeF}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.dpPoints}
            markerT={lab.d}
            markerY={Number.isFinite(lab.dPrime) ? lab.dPrime : 0}
            title="d'(d)"
            xLabel="d, см"
            yLabel="см"
          />
          <LabChart
            points={lab.magPoints}
            markerT={lab.d}
            markerY={lab.mag}
            title="Г(d)"
            xLabel="d, см"
            yLabel=""
          />
        </>
      }
    />
  );
}
