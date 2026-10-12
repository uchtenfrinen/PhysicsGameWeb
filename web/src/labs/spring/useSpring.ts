import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as spring from "../../physics/springOscillator.ts";
import { requestExplain } from "../groq.ts";

function localExplanation(k: number, m: number, x0: number, v0: number, omega: number, period: number, freq: number) {
  return (
    `Колебания груза на пружине. k = ${k.toFixed(1)} Н/м, m = ${m.toFixed(2)} кг, x₀ = ${x0.toFixed(2)} м, v₀ = ${v0.toFixed(2)} м/с. ` +
    `F = −k·x, ω = √(k/m) = ${omega.toFixed(2)} рад/с, T = 2π√(m/k) = ${period.toFixed(2)} с, f = ${freq.toFixed(2)} Гц. ` +
    `x(t) = x₀·cos(ωt) + (v₀/ω)·sin(ωt).`
  );
}

export function useSpring() {
  const [k, setKState] = useState(50);
  const [m, setMState] = useState(1);
  const [x0, setX0State] = useState(0.2);
  const [v0, setV0State] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const paramsRef = useRef({ k: 50, m: 1, x0: 0.2, v0: 0 });

  const omegaVal = useMemo(() => spring.omega(k, m), [k, m]);
  const periodVal = useMemo(() => spring.period(k, m), [k, m]);
  const freqVal = useMemo(() => spring.frequency(k, m), [k, m]);
  const tMax = periodVal * 2;
  const posPoints = useMemo(() => spring.getPositionPoints(k, m, x0, v0, 2), [k, m, x0, v0]);
  const velPoints = useMemo(() => spring.getVelocityPoints(k, m, x0, v0, 2), [k, m, x0, v0]);
  const currentX = spring.positionAt(k, m, x0, v0, currentTime);
  const currentV = spring.velocityAt(k, m, x0, v0, currentTime);
  const explanation = extraExplanation ?? localExplanation(k, m, x0, v0, omegaVal, periodVal, freqVal);
  const canPlay = periodVal > 1e-6 && (Math.abs(x0) > 1e-6 || Math.abs(v0) > 1e-6);
  const motionState = (() => {
    if (isPlaying) return "Колебание";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (tMax > 0 && currentTime >= tMax - 1e-6) return "Конец пробега";
    return "Пауза";
  })();

  useEffect(() => {
    paramsRef.current = { k, m, x0, v0 };
  }, [k, m, x0, v0]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, (now - last) / 1000);
      last = now;
      const p = paramsRef.current;
      const end = spring.period(p.k, p.m) * 2;
      const next = timeRef.current + dt;
      if (end > 0 && next >= end) {
        timeRef.current = end;
        setCurrentTime(end);
        playingRef.current = false;
        setIsPlaying(false);
        const vmax = spring.getVelocityPoints(p.k, p.m, p.x0, p.v0, 2).reduce(
          (acc, pt) => Math.max(acc, Math.abs(pt.v)),
          0,
        );
        setRunSummary({ vmax, t: end });
        return;
      }
      timeRef.current = next;
      setCurrentTime(next);
      frame = window.requestAnimationFrame(tick);
    };
    frame = window.requestAnimationFrame(tick);
    return () => window.cancelAnimationFrame(frame);
  }, [isPlaying]);

  const resetMotion = () => {
    playingRef.current = false;
    setIsPlaying(false);
    timeRef.current = 0;
    setCurrentTime(0);
    setExtraExplanation(null);
    setRunSummary(null);
  };

  const setK = (value: number) => {
    setKState(clamp(value, 0.1, 2000));
    resetMotion();
  };
  const setM = (value: number) => {
    setMState(clamp(value, 0.01, 100));
    resetMotion();
  };
  const setX0 = (value: number) => {
    setX0State(clamp(value, -2, 2));
    resetMotion();
  };
  const setV0 = (value: number) => {
    setV0State(clamp(value, -20, 20));
    resetMotion();
  };

  const placeX = (x: number) => {
    if (playingRef.current) return;
    setX0State(clamp(x, -2, 2));
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setX0State(0.2);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (!canPlay) return;
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
        `Объясни кратко по-русски гармонические колебания груза на пружине (без демпфирования). ` +
        `Параметры: жёсткость k = ${k.toFixed(1)} Н/м, масса m = ${m.toFixed(2)} кг, начальное смещение x₀ = ${x0.toFixed(2)} м, начальная скорость v₀ = ${v0.toFixed(2)} м/с. ` +
        `Период T = ${periodVal.toFixed(2)} с, частота f = ${freqVal.toFixed(2)} Гц, ω = ${omegaVal.toFixed(2)} рад/с. ` +
        "Дай короткое объяснение с формулами (Гук, ω² = k/m, x(t)).";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    k,
    m,
    x0,
    v0,
    setK,
    setM,
    setX0,
    setV0,
    placeX,
    omegaVal,
    periodVal,
    freqVal,
    tMax,
    posPoints,
    velPoints,
    currentTime,
    currentX,
    currentV,
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
