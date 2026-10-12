import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as refr from "../../physics/refractionAtBoundary.ts";
import { requestExplain } from "../groq.ts";

const RAY_RATE = 0.5;

function localExplanation(
  n1: number,
  n2: number,
  theta1: number,
  theta2: number,
  crit: number | null,
  tir: boolean,
) {
  if (tir) {
    const critStr = crit != null ? ` Предельный угол θкрит = ${crit.toFixed(1)}° (sin θкрит = n₂/n₁).` : "";
    return (
      `Закон Снеллиуса: n₁·sin θ₁ = n₂·sin θ₂. Луч идёт из более плотной среды (n₁ = ${n1.toFixed(2)}) в менее плотную (n₂ = ${n2.toFixed(2)}). ` +
      `При θ₁ = ${theta1.toFixed(0)}° — полное внутреннее отражение.${critStr}`
    );
  }
  const critStr = crit != null ? ` Предельный угол θкрит = ${crit.toFixed(1)}°.` : "";
  return (
    `Закон Снеллиуса: n₁·sin θ₁ = n₂·sin θ₂. При n₁ = ${n1.toFixed(2)}, n₂ = ${n2.toFixed(2)}, θ₁ = ${theta1.toFixed(0)}° ` +
    `sin θ₂ = n₁·sin θ₁/n₂ ⇒ θ₂ = ${theta2.toFixed(1)}°.${critStr}`
  );
}

function theta1FromTheta2(n1: number, n2: number, theta2: number): number | null {
  if (n1 <= 0 || n2 <= 0) return null;
  const sin1 = (n2 * Math.sin((theta2 * Math.PI) / 180)) / n1;
  if (!Number.isFinite(sin1) || Math.abs(sin1) >= 1) return null;
  return (180 * Math.asin(sin1)) / Math.PI;
}

export function useRefraction() {
  const [n1, setN1State] = useState(1);
  const [n2, setN2State] = useState(1.33);
  const [theta1, setThetaState] = useState(30);
  const [progress, setProgress] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const progressRef = useRef(0);
  const timeRef = useRef(0);

  const theta2 = refr.refractionAngleDegrees(n1, n2, theta1);
  const crit = refr.criticalAngleDegrees(n1, n2);
  const tir = refr.isTotalInternalReflection(n1, n2, theta1);
  const tMax = 1 / RAY_RATE;
  const pathLen = 2;

  const thetaPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 80; i++) {
      const a = (89 * i) / 80;
      const b = refr.refractionAngleDegrees(n1, n2, a);
      if (Number.isFinite(b) && !refr.isTotalInternalReflection(n1, n2, a)) list.push({ t: a, y: b });
    }
    return list.length > 0 ? list : [{ t: 0, y: 0 }];
  }, [n1, n2]);

  const n2Points = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 60; i++) {
      const n = 1 + (2 * i) / 60;
      const b = refr.refractionAngleDegrees(n1, n, theta1);
      if (Number.isFinite(b) && !refr.isTotalInternalReflection(n1, n, theta1)) list.push({ t: n, y: b });
    }
    return list.length > 0 ? list : [{ t: 1, y: 0 }];
  }, [n1, theta1]);

  const explanation = extraExplanation ?? localExplanation(n1, n2, theta1, theta2, crit, tir);
  const canPlay = theta1 > 1e-9;
  const motionState = (() => {
    if (isPlaying) return "Ход луча";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (currentTime >= tMax - 1e-6) return "Конец пробега";
    return "Пауза";
  })();

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const next = timeRef.current + dt;
      if (next >= tMax) {
        timeRef.current = tMax;
        progressRef.current = 1;
        setCurrentTime(tMax);
        setProgress(1);
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({ vmax: pathLen * RAY_RATE, t: tMax });
        return;
      }
      timeRef.current = next;
      progressRef.current = Math.min(1, next * RAY_RATE);
      setCurrentTime(next);
      setProgress(progressRef.current);
      frame = window.requestAnimationFrame(tick);
    };
    frame = window.requestAnimationFrame(tick);
    return () => window.cancelAnimationFrame(frame);
  }, [isPlaying, tMax]);

  const resetMotion = () => {
    playingRef.current = false;
    setIsPlaying(false);
    progressRef.current = 0;
    setProgress(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setExtraExplanation(null);
    setRunSummary(null);
  };

  const setN1 = (value: number) => {
    setN1State(clamp(value, 0.1, 3));
    resetMotion();
  };
  const setN2 = (value: number) => {
    setN2State(clamp(value, 0.1, 3));
    resetMotion();
  };
  const setTheta1 = (value: number) => {
    setThetaState(clamp(value, 0, 90));
    resetMotion();
  };
  const setTheta2 = (value: number) => {
    const next = theta1FromTheta2(n1, n2, clamp(value, 0, 90));
    if (next == null) return;
    setThetaState(clamp(next, 0, 90));
    resetMotion();
  };

  const placeTheta1 = (value: number) => {
    if (playingRef.current) return;
    setThetaState(clamp(value, 0, 90));
    progressRef.current = 0;
    setProgress(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const placeTheta2 = (value: number) => {
    if (playingRef.current) return;
    const next = theta1FromTheta2(n1, n2, clamp(value, 0, 90));
    if (next == null) return;
    setThetaState(clamp(next, 0, 90));
    progressRef.current = 0;
    setProgress(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setThetaState(30);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    progressRef.current = 0;
    setProgress(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setRunSummary(null);
    playingRef.current = true;
    setIsPlaying(true);
  };

  const explainGroq = async () => {
    setBusy(true);
    setExtraExplanation("Запрос к нейросети…");
    try {
      const prompt =
        `Объясни кратко по-русски преломление на границе двух сред. Закон Снеллиуса n₁·sin θ₁ = n₂·sin θ₂. ` +
        `Дано: n₁ = ${n1.toFixed(2)}, n₂ = ${n2.toFixed(2)}, угол падения θ₁ = ${theta1.toFixed(0)}°. ` +
        (tir
          ? "Наблюдается полное внутреннее отражение. Объясни предельный угол."
          : `Угол преломления θ₂ = ${theta2.toFixed(1)}°. Объясни ход луча.`);
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    n1,
    n2,
    theta1,
    setN1,
    setN2,
    setTheta1,
    setTheta2,
    placeTheta1,
    placeTheta2,
    theta2,
    crit,
    tir,
    thetaPoints,
    n2Points,
    progress,
    isPlaying,
    canPlay,
    runSummary,
    motionState,
    explanation,
    busy,
    build,
    togglePlay,
    explainGroq,
  };
}
