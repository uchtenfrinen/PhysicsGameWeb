import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as harm from "../../physics/harmonicOscillations.ts";
import { requestExplain } from "../groq.ts";

function localExplanation(A: number, T: number, phi0: number, omega: number, freq: number) {
  return (
    `Гармонические колебания: x(t) = A·cos(ωt + φ₀). ` +
    `A = ${A.toFixed(2)} м, T = ${T.toFixed(2)} с ⇒ ω = 2π/T = ${omega.toFixed(2)} рад/с, f = ${freq.toFixed(2)} Гц. ` +
    `φ₀ = ${phi0.toFixed(0)}°. v(t) = −Aω·sin(ωt + φ₀), a(t) = −ω²·x.`
  );
}

function phiFromX(x: number, A: number) {
  if (!(A > 1e-9)) return 0;
  return (Math.acos(clamp(x / A, -1, 1)) * 180) / Math.PI;
}

export function useHarmonic() {
  const [amplitude, setAState] = useState(1);
  const [periodSec, setTState] = useState(2);
  const [phi0, setPhiState] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const paramsRef = useRef({ amplitude: 1, periodSec: 2, phi0: 0 });

  const omega = periodSec > 0 ? (2 * Math.PI) / periodSec : 0;
  const freq = harm.frequency(omega);
  const tMax = harm.period(omega) * 2;
  const posPoints = useMemo(
    () => harm.getChartPoints(amplitude, omega, phi0, 2).map((p) => ({ t: p.t, y: p.x })),
    [amplitude, omega, phi0],
  );
  const velPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    const p = harm.period(omega);
    if (p <= 0) return list;
    const end = p * 2;
    for (let i = 0; i <= 120; i++) {
      const t = (end * i) / 120;
      list.push({ t, y: harm.velocityAt(amplitude, omega, phi0, t) });
    }
    return list;
  }, [amplitude, omega, phi0]);

  const currentX = harm.positionAt(amplitude, omega, phi0, currentTime);
  const currentV = harm.velocityAt(amplitude, omega, phi0, currentTime);
  const currentA = harm.accelerationAt(amplitude, omega, phi0, currentTime);
  const explanation = extraExplanation ?? localExplanation(amplitude, periodSec, phi0, omega, freq);
  const canPlay = amplitude > 1e-6 && periodSec > 1e-6;
  const motionState = (() => {
    if (isPlaying) return "Колебание";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (tMax > 0 && currentTime >= tMax - 1e-6) return "Конец пробега";
    return "Пауза";
  })();

  useEffect(() => {
    paramsRef.current = { amplitude, periodSec, phi0 };
  }, [amplitude, periodSec, phi0]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const p = paramsRef.current;
      const w = p.periodSec > 0 ? (2 * Math.PI) / p.periodSec : 0;
      const end = harm.period(w) * 2;
      const next = timeRef.current + dt;
      if (end > 0 && next >= end) {
        timeRef.current = end;
        setCurrentTime(end);
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({ vmax: p.amplitude * w, t: end });
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

  const setAmplitude = (value: number) => {
    setAState(clamp(value, 0.01, 10));
    resetMotion();
  };
  const setPeriodSec = (value: number) => {
    setTState(clamp(value, 0.1, 20));
    resetMotion();
  };
  const setPhi0 = (value: number) => {
    setPhiState(clamp(value, -180, 180));
    resetMotion();
  };
  const setOmega = (value: number) => {
    if (!(value > 0)) return;
    setTState(clamp((2 * Math.PI) / value, 0.1, 20));
    resetMotion();
  };

  const placeX = (x: number) => {
    if (playingRef.current) return;
    const nextX = clamp(x, -10, 10);
    const A = amplitude;
    if (Math.abs(nextX) > A) {
      setAState(clamp(Math.abs(nextX), 0.01, 10));
      setPhiState(nextX >= 0 ? 0 : 180);
    } else {
      setPhiState(phiFromX(nextX, A));
    }
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setPhiState(0);
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
        `Объясни кратко по-русски гармонические колебания. Уравнение x(t) = A·cos(ωt + φ₀). ` +
        `Параметры: амплитуда A = ${amplitude.toFixed(2)}, период T = ${periodSec.toFixed(2)} с, начальная фаза φ₀ = ${phi0.toFixed(0)}°. ` +
        `Циклическая частота ω = 2π/T = ${omega.toFixed(2)} рад/с, частота f = ${freq.toFixed(2)} Гц. Связь v(t) и a(t) с x(t).`;
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    amplitude,
    periodSec,
    phi0,
    setAmplitude,
    setPeriodSec,
    setPhi0,
    setOmega,
    placeX,
    omega,
    freq,
    tMax,
    posPoints,
    velPoints,
    currentTime,
    currentX,
    currentV,
    currentA,
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
