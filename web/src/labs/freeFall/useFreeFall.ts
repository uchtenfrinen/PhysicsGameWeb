import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as freeFall from "../../physics/freeFall.ts";
import { requestExplain } from "../groq.ts";

function localExplanation(height0: number, massKg: number, v0: number, time: number, speed: number): string {
  return (
    `Тело массой m = ${massKg.toFixed(2)} кг, h₀ = ${height0.toFixed(1)} м, v₀ = ${v0.toFixed(2)} м/с вниз. ` +
    `g = ${freeFall.G.toFixed(2)} м/с². ` +
    `h(t) = h₀ − v₀t − gt²/2, v(t) = v₀ + gt. ` +
    `T = ${time.toFixed(2)} с, скорость удара ${speed.toFixed(2)} м/с.`
  );
}

export function useFreeFall() {
  const [height0, setHeight0State] = useState(20);
  const [massKg, setMassKgState] = useState(2);
  const [v0, setV0State] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const heightRef = useRef(20);
  const v0Ref = useRef(0);

  const timeOfFall = useMemo(() => freeFall.timeOfFall(height0, v0), [height0, v0]);
  const finalSpeed = useMemo(() => freeFall.finalSpeed(height0, v0), [height0, v0]);
  const peakH = useMemo(() => freeFall.peakHeight(height0, v0), [height0, v0]);
  const scaleH = Math.max(20, height0, peakH);
  const heightPoints = useMemo(() => freeFall.getHeightPoints(height0, 60, v0), [height0, v0]);
  const velocityPoints = useMemo(() => freeFall.getVelocityPoints(height0, 60, v0), [height0, v0]);
  const tEff = Math.min(Math.max(0, currentTime), Math.max(0, timeOfFall));
  const currentHeight = freeFall.heightAt(tEff, height0, v0);
  const currentSpeed = freeFall.velocityAt(tEff, v0);
  const explanation =
    extraExplanation ?? localExplanation(height0, massKg, v0, timeOfFall, finalSpeed);
  const canPlay = timeOfFall > 1e-6;

  const motionState = (() => {
    if (isPlaying) return currentSpeed < -1e-3 ? "Вверх" : "Движение вниз";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (timeOfFall > 0 && currentTime >= timeOfFall - 1e-6) return "На поверхности";
    return "Пауза";
  })();

  useEffect(() => {
    heightRef.current = height0;
    v0Ref.current = v0;
  }, [height0, v0]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, (now - last) / 1000);
      last = now;
      const h0 = heightRef.current;
      const vStart = v0Ref.current;
      const tFall = freeFall.timeOfFall(h0, vStart);
      const next = timeRef.current + dt;
      const h = freeFall.heightAt(next, h0, vStart);
      if (tFall <= 0 || next >= tFall || h <= 0) {
        timeRef.current = Math.min(next, Math.max(tFall, 0));
        setCurrentTime(timeRef.current);
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({
          vmax: freeFall.finalSpeed(h0, vStart),
          t: timeRef.current,
        });
        return;
      }
      timeRef.current = next;
      setCurrentTime(next);
      frame = window.requestAnimationFrame(tick);
    };
    frame = window.requestAnimationFrame(tick);
    return () => window.cancelAnimationFrame(frame);
  }, [isPlaying]);

  const stopMotion = (clearSummary = true) => {
    playingRef.current = false;
    setIsPlaying(false);
    timeRef.current = 0;
    setCurrentTime(0);
    setExtraExplanation(null);
    if (clearSummary) setRunSummary(null);
  };

  const setHeight0 = (value: number) => {
    const next = clamp(value, 0.1, 1000);
    heightRef.current = next;
    setHeight0State(next);
    stopMotion();
  };

  const setMassKg = (value: number) => {
    setMassKgState(clamp(value, 0.01, 1_000_000));
    setExtraExplanation(null);
  };

  const setV0 = (value: number) => {
    const next = clamp(value, -20, 20);
    v0Ref.current = next;
    setV0State(next);
    stopMotion();
  };

  const placeHeight = (h: number) => {
    if (playingRef.current) return;
    const next = clamp(h, 0.1, 1000);
    heightRef.current = next;
    setHeight0State(next);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    const resetH = 20;
    heightRef.current = resetH;
    setHeight0State(resetH);
    stopMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (freeFall.timeOfFall(height0, v0) <= 1e-6) return;
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
        `Объясни кратко по-русски свободное падение. ` +
        `Начальная высота h₀ = ${height0.toFixed(1)} м, масса m = ${massKg.toFixed(2)} кг (T и v от m не зависят), ` +
        `начальная скорость вниз v₀ = ${v0.toFixed(2)} м/с. ` +
        `Результаты: T = ${timeOfFall.toFixed(2)} с, скорость приземления v = ${finalSpeed.toFixed(2)} м/с. ` +
        "Формулы: h(t) = h₀ − v₀·t − g·t²/2, v(t) = v₀ + g·t, T = (−v₀ + √(v₀² + 2g·h₀))/g. Используй в объяснении именно эти численные значения.";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    height0,
    massKg,
    v0,
    setHeight0,
    setMassKg,
    setV0,
    placeHeight,
    timeOfFall,
    finalSpeed,
    scaleH,
    currentTime,
    currentHeight,
    currentSpeed,
    isPlaying,
    canPlay,
    runSummary,
    motionState,
    heightPoints,
    velocityPoints,
    explanation,
    busy,
    build,
    togglePlay,
    explainGroq,
  };
}
