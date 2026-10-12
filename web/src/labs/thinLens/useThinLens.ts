import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as lens from "../../physics/thinLens.ts";
import { requestExplain } from "../groq.ts";

const RAY_RATE = 0.42;

function localExplanation(d: number, f: number, dP: number, mag: number, hImg: number, real: boolean) {
  if (!Number.isFinite(dP)) {
    return "При d = f изображение уходит в бесконечность (лучи после линзы параллельны).";
  }
  const dist = dP > 0
    ? `Изображение справа от линзы на расстоянии d' = ${dP.toFixed(1)} см (действительное).`
    : `Изображение слева от линзы на расстоянии |d'| = ${Math.abs(dP).toFixed(1)} см (мнимое).`;
  return (
    `Формула линзы: 1/f = 1/d + 1/d' ⇒ d' = f·d/(d−f). ` +
    `d = ${d.toFixed(1)} см, f = ${f.toFixed(1)} см → d' = ${dP.toFixed(1)} см. ${dist} ` +
    `Г = −d'/d = ${mag.toFixed(2)}. h' = |Г|·h = ${hImg.toFixed(2)} см. ` +
    `${real ? "Действительное, перевёрнутое." : "Мнимое, прямое."}`
  );
}

export function useThinLens() {
  const [d, setDState] = useState(30);
  const [f, setFState] = useState(20);
  const [objectHeight, setHState] = useState(10);
  const [progress, setProgress] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const progressRef = useRef(0);
  const timeRef = useRef(0);
  const pathRef = useRef(0);

  const dPrime = lens.imageDistance(d, f);
  const mag = lens.magnification(d, f);
  const imageH = lens.imageHeight(objectHeight, d, f);
  const real = lens.isRealImage(d, f);
  const character = !Number.isFinite(dPrime)
    ? "На экране в бесконечности (d = f)"
    : real
      ? "Действительное, перевёрнутое"
      : "Мнимое, прямое";
  const tMax = 1 / RAY_RATE;
  const pathLen =
    Number.isFinite(dPrime)
      ? Math.hypot(d, objectHeight) + Math.hypot(Math.abs(dPrime), imageH)
      : 0;

  const dpPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 80; i++) {
      const x = 0.2 + ((80 - 0.2) * i) / 80;
      const y = lens.imageDistance(x, f);
      if (Number.isFinite(y) && Math.abs(y) < 180) list.push({ t: x, y });
    }
    return list;
  }, [f]);

  const magPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 80; i++) {
      const x = 0.2 + ((80 - 0.2) * i) / 80;
      const y = lens.magnification(x, f);
      if (Number.isFinite(y) && Math.abs(y) < 8) list.push({ t: x, y });
    }
    return list;
  }, [f]);

  const explanation =
    extraExplanation ?? localExplanation(d, f, dPrime, mag, imageH, real);
  const canPlay = Number.isFinite(dPrime);
  const motionState = (() => {
    if (isPlaying) return "Ход луча";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (currentTime >= tMax - 1e-6) return "Конец пробега";
    return "Пауза";
  })();

  pathRef.current = pathLen;

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
        setRunSummary({ vmax: pathRef.current * RAY_RATE, t: tMax });
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

  const setD = (value: number) => {
    setDState(clamp(value, 0.1, 200));
    resetMotion();
  };
  const setF = (value: number) => {
    setFState(clamp(value, 0.1, 200));
    resetMotion();
  };
  const setObjectHeight = (value: number) => {
    setHState(clamp(value, 0.1, 50));
    resetMotion();
  };
  const setDPrime = (value: number) => {
    if (Math.abs(value - f) < 1e-6) return;
    const nextD = (value * f) / (value - f);
    if (!(nextD > 0) || !Number.isFinite(nextD)) return;
    setDState(clamp(nextD, 0.1, 200));
    resetMotion();
  };

  const placeObject = (nextD: number, nextH: number) => {
    if (playingRef.current) return;
    setDState(clamp(nextD, 0.1, 200));
    setHState(clamp(nextH, 0.1, 50));
    progressRef.current = 0;
    setProgress(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const placeF = (nextF: number) => {
    if (playingRef.current) return;
    setFState(clamp(nextF, 0.1, 200));
    progressRef.current = 0;
    setProgress(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setDState(30);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (!Number.isFinite(dPrime)) return;
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
        `Объясни кратко по-русски тонкую собирающую линзу. Формула линзы 1/f = 1/d + 1/d'. ` +
        `Дано: расстояние от предмета до линзы d = ${d.toFixed(1)} см, фокус f = ${f.toFixed(1)} см. ` +
        `Расстояние до изображения d' = ${Number.isFinite(dPrime) ? dPrime.toFixed(1) : "∞"} см, увеличение Г = ${mag.toFixed(2)}. ` +
        `Опиши построение хода трёх лучей и характер изображения.`;
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    d,
    f,
    objectHeight,
    setD,
    setF,
    setObjectHeight,
    setDPrime,
    placeObject,
    placeF,
    dPrime,
    mag,
    imageH,
    real,
    character,
    dpPoints,
    magPoints,
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
