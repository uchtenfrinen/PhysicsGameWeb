import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as gas from "../../physics/idealGas.ts";
import { requestExplain } from "../groq.ts";

const PERIOD = 4;

function localExplanation(n: number, T: number, V: number, P: number, vMin: number, vMax: number) {
  return (
    `PV = νRT, R = ${gas.R.toFixed(3)} Дж/(моль·К). ` +
    `ν = ${n.toFixed(2)} моль, T = ${T.toFixed(0)} К, V = ${V.toFixed(4)} м³. ` +
    `P = νRT/V = ${P.toFixed(0)} Па. ` +
    `Изотерма (T = const): PV = const, поршень: V от ${vMin.toFixed(4)} до ${vMax.toFixed(4)} м³.`
  );
}

function volumeAt(t: number, vMin: number, vMax: number, period: number) {
  const mid = (vMin + vMax) / 2;
  const amp = (vMax - vMin) / 2;
  if (period <= 0) return mid;
  return clamp(mid + amp * Math.sin((t / period) * Math.PI * 2), vMin, vMax);
}

function volumeRateAt(t: number, vMin: number, vMax: number, period: number) {
  const amp = (vMax - vMin) / 2;
  if (period <= 0) return 0;
  const omega = (Math.PI * 2) / period;
  return amp * omega * Math.cos((t / period) * Math.PI * 2);
}

export function useIdealGas() {
  const [n, setNState] = useState(1);
  const [temperature, setTState] = useState(300);
  const [volume, setVState] = useState(0.025);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const paramsRef = useRef({ n: 1, temperature: 300, volume: 0.025 });

  const vMin = volume * 0.4;
  const vMax = volume * 1.6;
  const pressure0 = gas.pressure(n, temperature, volume);
  const tEff = isPlaying || currentTime > 1e-9 ? currentTime : 0;
  const currentV = volumeAt(tEff, vMin, vMax, PERIOD);
  const currentP = gas.pressure(n, temperature, currentV);
  const currentVDot = isPlaying ? volumeRateAt(tEff, vMin, vMax, PERIOD) : 0;
  const isoPoints = useMemo(
    () => gas.isothermPoints(n, temperature, vMin, vMax).map((p) => ({ t: p.v, y: p.p })),
    [n, temperature, vMin, vMax],
  );
  const vTimePoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 60; i++) {
      const t = (PERIOD * i) / 60;
      list.push({ t, y: volumeAt(t, vMin, vMax, PERIOD) });
    }
    return list;
  }, [vMin, vMax]);
  const explanation =
    extraExplanation ?? localExplanation(n, temperature, volume, pressure0, vMin, vMax);
  const canPlay = vMax - vMin > 1e-9;
  const motionState = (() => {
    if (isPlaying) return "Изотерма";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (currentTime >= PERIOD - 1e-6) return "Конец пробега";
    return "Пауза";
  })();

  useEffect(() => {
    paramsRef.current = { n, temperature, volume };
  }, [n, temperature, volume]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const next = timeRef.current + dt;
      if (next >= PERIOD) {
        timeRef.current = PERIOD;
        setCurrentTime(PERIOD);
        playingRef.current = false;
        setIsPlaying(false);
        const p = paramsRef.current;
        const lo = p.volume * 0.4;
        const hi = p.volume * 1.6;
        const amp = (hi - lo) / 2;
        setRunSummary({ vmax: amp * ((Math.PI * 2) / PERIOD), t: PERIOD });
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

  const setN = (value: number) => {
    setNState(clamp(value, 0.01, 1000));
    resetMotion();
  };
  const setTemperature = (value: number) => {
    setTState(clamp(value, 1, 10000));
    resetMotion();
  };
  const setVolume = (value: number) => {
    setVState(clamp(value, 1e-6, 100));
    resetMotion();
  };

  const placeVolume = (value: number) => {
    if (playingRef.current) return;
    setVState(clamp(value, 1e-6, 100));
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setVState(0.025);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (vMax - vMin <= 1e-9) return;
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
        `Объясни кратко по-русски уравнение состояния идеального газа (Менделеева — Клапейрона). ` +
        `Параметры: ν = ${n.toFixed(2)} моль, T = ${temperature.toFixed(0)} К, V = ${volume.toFixed(4)} м³. Давление P = ${pressure0.toFixed(0)} Па. ` +
        "Упомяни PV = νRT и изотермический процесс (Бойль — Мариотт). Дай короткое объяснение.";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    n,
    temperature,
    volume,
    setN,
    setTemperature,
    setVolume,
    placeVolume,
    pressure0,
    currentV,
    currentP,
    currentVDot,
    vMin,
    vMax,
    isoPoints,
    vTimePoints,
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
