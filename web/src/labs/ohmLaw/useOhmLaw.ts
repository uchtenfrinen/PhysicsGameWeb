import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as ohm from "../../physics/ohmLaw.ts";
import { requestExplain } from "../groq.ts";

const LOOPS = 2;

function chargeSpeed(currentA: number) {
  return 0.12 + Math.min(2.2, Math.max(0, currentA) * 0.45);
}

function localExplanation(e: number, r: number, R: number, i: number, uR: number, ur: number, p: number) {
  return (
    `Закон Ома для замкнутой цепи: I = ε/(r+R). ` +
    `ε = ${e.toFixed(2)} В, r = ${r.toFixed(2)} Ом, R = ${R.toFixed(2)} Ом. ` +
    `I = ${i.toFixed(3)} А. U_R = I·R = ${uR.toFixed(2)} В, U_r = I·r = ${ur.toFixed(2)} В. ` +
    `P = I²·R = ${p.toFixed(2)} Вт.`
  );
}

export function useOhmLaw() {
  const [epsilonV, setEpsilonState] = useState(12);
  const [internalOhm, setInternalState] = useState(0.5);
  const [externalOhm, setExternalState] = useState(23.5);
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

  const currentA = ohm.currentClosed(epsilonV, internalOhm, externalOhm);
  const voltageExternal = ohm.voltageExternal(currentA, externalOhm);
  const voltageInternal = ohm.voltageInternal(currentA, internalOhm);
  const powerW = ohm.powerExternal(currentA, externalOhm);
  const efficiency = epsilonV > 0 ? (100 * voltageExternal) / epsilonV : 0;
  const speed = chargeSpeed(currentA);
  const tMax = speed > 1e-6 ? LOOPS / speed : 0;

  const uiPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    const iMax = internalOhm > 0 ? epsilonV / internalOhm : Math.max(currentA * 2, 1);
    const span = Math.max(iMax * 1.05, 0.1);
    for (let i = 0; i <= 60; i++) {
      const I = (span * i) / 60;
      list.push({ t: I, y: Math.max(0, epsilonV - I * internalOhm) });
    }
    return list;
  }, [epsilonV, internalOhm, currentA]);

  const irPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 60; i++) {
      const R = 0.1 + ((100 - 0.1) * i) / 60;
      list.push({ t: R, y: ohm.currentClosed(epsilonV, internalOhm, R) });
    }
    return list;
  }, [epsilonV, internalOhm]);

  const explanation =
    extraExplanation ??
    localExplanation(epsilonV, internalOhm, externalOhm, currentA, voltageExternal, voltageInternal, powerW);
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

  const setEpsilonV = (value: number) => {
    setEpsilonState(clamp(value, 0.5, 50));
    resetMotion();
  };
  const setInternalOhm = (value: number) => {
    setInternalState(clamp(value, 0, 20));
    resetMotion();
  };
  const setExternalOhm = (value: number) => {
    setExternalState(clamp(value, 0.1, 100));
    resetMotion();
  };
  const setCurrentA = (value: number) => {
    const i = clamp(value, 1e-4, 100);
    const nextR = ohm.externalResistance(epsilonV, i, internalOhm);
    setExternalState(clamp(nextR, 0.1, 100));
    resetMotion();
  };

  const placeEpsilon = (value: number) => {
    if (playingRef.current) return;
    setEpsilonState(clamp(value, 0.5, 50));
    phaseRef.current = 0;
    setPhase(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const placeR = (value: number) => {
    if (playingRef.current) return;
    setExternalState(clamp(value, 0.1, 100));
    phaseRef.current = 0;
    setPhase(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setExternalState(23.5);
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
        `Объясни кратко по-русски закон Ома для замкнутой цепи. ` +
        `Сила тока в замкнутой цепи прямо пропорциональна ЭДС источника и обратно пропорциональна полному сопротивлению цепи. ` +
        `Формула I = ε/(r+R). Параметры: ЭДС ε = ${epsilonV.toFixed(1)} В, внутреннее сопротивление r = ${internalOhm.toFixed(2)} Ом, внешнее R = ${externalOhm.toFixed(1)} Ом. ` +
        `Сила тока I = ${currentA.toFixed(3)} А. Дай короткое объяснение.`;
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    epsilonV,
    internalOhm,
    externalOhm,
    setEpsilonV,
    setInternalOhm,
    setExternalOhm,
    setCurrentA,
    placeEpsilon,
    placeR,
    currentA,
    voltageExternal,
    voltageInternal,
    powerW,
    efficiency,
    uiPoints,
    irPoints,
    phase,
    currentTime,
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
