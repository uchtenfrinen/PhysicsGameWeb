import { LabChart } from "../Chart.tsx";
import { LabField, LabHint } from "../LabField.tsx";
import { LabShell } from "../LabShell.tsx";
import { RefractionCanvas } from "./RefractionCanvas.tsx";
import { useRefraction } from "./useRefraction.ts";

export function RefractionLab() {
  const lab = useRefraction();
  const theta2Ok = !lab.tir && Number.isFinite(lab.theta2);
  const theta2Text = theta2Ok ? `${lab.theta2.toFixed(1)}°` : "—";
  const critText = lab.crit != null ? `${lab.crit.toFixed(1)}°` : "—";

  return (
    <LabShell
      title="преломление"
      layout="wide"
      explanation={lab.explanation}
      formulas={
        <p className="eq">
          n₁ sin θ₁ = n₂ sin θ₂
          <br />
          sin θкрит = n₂ / n₁
          <br />
          θ₂ = arcsin(n₁ sin θ₁ / n₂)
        </p>
      }
      side={
        <>
          <LabField
            label="n₁"
            value={lab.n1}
            digits={2}
            onCommit={lab.setN1}
            hint="Показатель преломления верхней среды"
          />
          <LabField
            label="n₂"
            value={lab.n2}
            digits={2}
            onCommit={lab.setN2}
            hint="Показатель преломления нижней среды"
          />
          <LabField
            label="θ₁"
            value={lab.theta1}
            digits={0}
            onCommit={lab.setTheta1}
            hint="Угол падения от нормали, °. Можно задать мышью — верхняя среда"
          />
          {theta2Ok ? (
            <LabField
              label="θ₂"
              value={lab.theta2}
              digits={1}
              onCommit={lab.setTheta2}
              hint="Угол преломления, °. Если задать θ₂, пересчитается θ₁. Можно задать мышью — нижняя среда"
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
              <dt>θ₂</dt>
              <dd>{theta2Text}</dd>
              <LabHint text="Угол преломления. n₁ sin θ₁ = n₂ sin θ₂. При полном внутреннем отражении луча нет" />
            </div>
            <div>
              <dt>θкрит</dt>
              <dd>{critText}</dd>
              <LabHint text="Предельный угол. sin θкрит = n₂/n₁, только при n₁ > n₂" />
            </div>
            <div>
              <dt>полное внутреннее отражение</dt>
              <dd>{lab.tir ? "да" : "нет"}</dd>
              <LabHint text="n₁ > n₂ и θ₁ ≥ θкрит" />
            </div>
            <div>
              <dd>{lab.motionState}</dd>
            </div>
          </dl>
          {lab.runSummary ? (
            <dl className="reads reads-end">
              <div>
                <dt>vmax</dt>
                <dd>{lab.runSummary.vmax.toFixed(2)}</dd>
                <LabHint text="Скорость маркера по лучу на схеме (длина каждого отрезка = 1)" />
              </div>
              <div>
                <dt>t</dt>
                <dd>{lab.runSummary.t.toFixed(2)} с</dd>
                <LabHint text="Время хода луча от входа до конца, с" />
              </div>
            </dl>
          ) : null}
        </>
      }
      stage={
        <RefractionCanvas
          n1={lab.n1}
          n2={lab.n2}
          theta1={lab.theta1}
          theta2={lab.theta2}
          tir={lab.tir}
          progress={lab.progress}
          playing={lab.isPlaying}
          dragEnabled={!lab.isPlaying}
          onPlaceTheta1={lab.placeTheta1}
          onPlaceTheta2={lab.placeTheta2}
        />
      }
      charts={
        <>
          <LabChart
            points={lab.thetaPoints}
            markerT={lab.theta1}
            markerY={theta2Ok ? lab.theta2 : Number.NaN}
            title="θ₂(θ₁)"
            xLabel="θ₁, °"
            yLabel="°"
          />
          <LabChart
            points={lab.n2Points}
            markerT={lab.n2}
            markerY={theta2Ok ? lab.theta2 : Number.NaN}
            title="θ₂(n₂)"
            xLabel="n₂"
            yLabel="°"
          />
        </>
      }
    />
  );
}
