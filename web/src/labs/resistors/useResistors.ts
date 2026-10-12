import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as resistors from "../../physics/resistorsConnection.ts";
import { requestExplain } from "../groq.ts";

const LOOPS = 2;

function chargeSpeed(currentA: number) {
  return 0.12 + Math.min(2.2, Math.max(0, currentA) * 0.45);
}

function localExplanation(
  isSeries: boolean,
  u: number,
  r1: number,
  r2: number,
  r3: number,
  rTotal: number,
  i: number,
  u1: number,
  u2: number,
  u3: number,
  i1: number,
  i2: number,
  i3: number,
  p: number,
) {
  if (isSeries) {
    return (
      `Последовательное соединение: R = R1+R2+R3 = ${r1.toFixed(1)}+${r2.toFixed(1)}+${r3.toFixed(1)} = ${rTotal.toFixed(2)} Ом. ` +
      `I = U/R = ${u.toFixed(1)}/${rTotal.toFixed(2)} = ${i.toFixed(3)} А. ` +
      `U1 = ${u1.toFixed(2)} В, U2 = ${u2.toFixed(2)} В, U3 = ${u3.toFixed(2)} В. P = ${p.toFixed(2)} Вт.`
    );
  }
  return (
    `Параллельное соединение: 1/R = 1/R1+1/R2+1/R3 ⇒ R = ${rTotal.toFixed(2)} Ом. ` +
    `U = ${u.toFixed(1)} В на всех ветвях. I1 = ${i1.toFixed(3)} А, I2 = ${i2.toFixed(3)} А, I3 = ${i3.toFixed(3)} А. ` +
    `I = ${i.toFixed(3)} А. P = ${p.toFixed(2)} Вт.`
  );
}

export function useResistors() {
  const [voltageV, setVoltageState] = useState(12);
  const [r1, setR1State] = useState(4);
  const [r2, setR2State] = useState(6);
  const [r3, setR3State] = useState(12);
  const [isSeries, setIsSeriesState] = useState(true);
  const [phase, setPhase] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const phaseRef = useRef(0);
  const timeRef = useRef(0);
  const currentRef = useRef(0);

  const rTotal = isSeries
    ? resistors.seriesEquivalent(r1, r2, r3)
    : resistors.parallelEquivalent(r1, r2, r3);
  const currentA = resistors.current(voltageV, rTotal);
  const u1 = isSeries ? resistors.voltageDropSeries(currentA, r1) : voltageV;
  const u2 = isSeries ? resistors.voltageDropSeries(currentA, r2) : voltageV;
  const u3 = isSeries ? resistors.voltageDropSeries(currentA, r3) : voltageV;
  const i1 = isSeries ? currentA : resistors.branchCurrentParallel(voltageV, r1);
  const i2 = isSeries ? currentA : resistors.branchCurrentParallel(voltageV, r2);
  const i3 = isSeries ? currentA : resistors.branchCurrentParallel(voltageV, r3);
  const powerW = resistors.power(voltageV, currentA);
  const speed = chargeSpeed(currentA);
  const tMax = speed > 1e-6 ? LOOPS / speed : 0;

  const iuPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 60; i++) {
      const U = (20 * i) / 60;
      list.push({ t: U, y: resistors.current(U, rTotal) });
    }
    return list;
  }, [rTotal]);

  const puPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 60; i++) {
      const U = (20 * i) / 60;
      const I = resistors.current(U, rTotal);
      list.push({ t: U, y: resistors.power(U, I) });
    }
    return list;
  }, [rTotal]);

  const explanation =
    extraExplanation ??
    localExplanation(isSeries, voltageV, r1, r2, r3, rTotal, currentA, u1, u2, u3, i1, i2, i3, powerW);
  const canPlay = currentA > 1e-4 && tMax > 1e-6;
  const motionState = (() => {
    if (isPlaying) return "Ток в цепи";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (tMax > 0 && currentTime >= tMax - 1e-6) return "Конец пробега";
    return "Пауза";
  })();

  currentRef.current = currentA;

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const i = currentRef.current;
      const spd = chargeSpeed(i);
      const end = spd > 1e-6 ? LOOPS / spd : 0;
      const next = timeRef.current + dt;
      if (end > 0 && next >= end) {
        timeRef.current = end;
        phaseRef.current = (end * spd) % 1;
        setCurrentTime(end);
        setPhase(phaseRef.current);
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({ vmax: i, t: end });
        return;
      }
      timeRef.current = next;
      phaseRef.current = (next * spd) % 1;
      setCurrentTime(next);
      setPhase(phaseRef.current);
      frame = window.requestAnimationFrame(tick);
    };
    frame = window.requestAnimationFrame(tick);
    return () => window.cancelAnimationFrame(frame);
  }, [isPlaying]);

  const resetMotion = () => {
    playingRef.current = false;
    setIsPlaying(false);
    phaseRef.current = 0;
    setPhase(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setExtraExplanation(null);
    setRunSummary(null);
  };

  const setVoltageV = (value: number) => {
    setVoltageState(clamp(value, 0, 20));
    resetMotion();
  };
  const setR1 = (value: number) => {
    setR1State(clamp(value, 0.1, 100));
    resetMotion();
  };
  const setR2 = (value: number) => {
    setR2State(clamp(value, 0.1, 100));
    resetMotion();
  };
  const setR3 = (value: number) => {
    setR3State(clamp(value, 0.1, 100));
    resetMotion();
  };
  const setCurrentA = (value: number) => {
    const i = clamp(value, 1e-4, 100);
    if (rTotal <= 0) return;
    setVoltageState(clamp(i * rTotal, 0, 20));
    resetMotion();
  };
  const setSeries = (value: boolean) => {
    setIsSeriesState(value);
    resetMotion();
  };

  const placeR = (index: number, value: number) => {
    if (playingRef.current) return;
    const next = clamp(value, 0.1, 100);
    if (index === 0) setR1State(next);
    else if (index === 1) setR2State(next);
    else setR3State(next);
    phaseRef.current = 0;
    setPhase(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setR1State(4);
    setR2State(6);
    setR3State(12);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (currentA <= 1e-4) return;
    phaseRef.current = 0;
    setPhase(0);
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
        "Объясни кратко по-русски последовательное и параллельное соединение резисторов. " +
        "Последовательно: I = I1 = I2 = I3, U = U1 + U2 + U3, R = R1 + R2 + R3. " +
        "Параллельно: I = I1 + I2 + I3, U = U1 = U2 = U3, 1/R = 1/R1 + 1/R2 + 1/R3. Дай короткое объяснение.";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    voltageV,
    r1,
    r2,
    r3,
    isSeries,
    setVoltageV,
    setR1,
    setR2,
    setR3,
    setCurrentA,
    setSeries,
    placeR,
    rTotal,
    currentA,
    u1,
    u2,
    u3,
    i1,
    i2,
    i3,
    powerW,
    iuPoints,
    puPoints,
    phase,
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
