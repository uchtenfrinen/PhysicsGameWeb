import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as charge from "../../physics/chargeInField.ts";
import { requestExplain } from "../groq.ts";

function localExplanation(
  q: number,
  E: number,
  m: number,
  v0: number,
  angle: number,
  y0: number,
  a: number,
  aY: number,
  T: number,
  L: number,
  H: number,
) {
  const rad = (angle * Math.PI) / 180;
  const v0x = v0 * Math.cos(rad);
  const v0y = v0 * Math.sin(rad);
  return (
    `Заряд q = ${q.toExponential(2)} Кл в однородном поле E = ${E.toFixed(0)} В/м, m = ${m.toExponential(2)} кг, y₀ = ${y0.toFixed(1)} м. ` +
    `Поле вниз: a_y = −qE/m = ${aY.toFixed(2)} м/с², |a| = ${a.toFixed(2)} м/с². ` +
    `v₀ = ${v0.toFixed(1)} м/с, α = ${angle.toFixed(0)}°, v₀x = ${v0x.toFixed(2)}, v₀y = ${v0y.toFixed(2)}. ` +
    `x = v₀ cos α · t, y = y₀ + v₀ sin α · t + a_y t²/2. T = ${T.toFixed(2)} с, L = ${L.toFixed(2)} м, H = ${H.toFixed(2)} м.`
  );
}

export function useChargeField() {
  const [q, setQState] = useState(1e-6);
  const [eField, setEState] = useState(100);
  const [m, setMState] = useState(1e-6);
  const [v0, setV0State] = useState(20);
  const [angleDegrees, setAngleState] = useState(45);
  const [y0, setY0State] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const paramsRef = useRef({ q: 1e-6, eField: 100, m: 1e-6, v0: 20, angleDegrees: 45, y0: 0 });

  const accel = charge.accelerationMagnitude(q, eField, m);
  const aY = charge.accelerationY(q, eField, m);
  const timeOfFlight = useMemo(
    () => charge.timeOfFlight(v0, angleDegrees, q, eField, m, y0),
    [v0, angleDegrees, q, eField, m, y0],
  );
  const rangeM = useMemo(
    () => charge.range(v0, angleDegrees, q, eField, m, y0),
    [v0, angleDegrees, q, eField, m, y0],
  );
  const heightM = useMemo(
    () => charge.maxHeight(v0, angleDegrees, q, eField, m, y0),
    [v0, angleDegrees, q, eField, m, y0],
  );
  const trajectory = useMemo(
    () => charge.getTrajectoryPoints(v0, angleDegrees, q, eField, m, 80, y0),
    [v0, angleDegrees, q, eField, m, y0],
  );

  const heightPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    const T = Math.max(timeOfFlight, 0.1);
    for (let i = 0; i <= 60; i++) {
      const t = (T * i) / 60;
      list.push({ t, y: Math.max(0, charge.positionAt(v0, angleDegrees, q, eField, m, t, y0).y) });
    }
    return list;
  }, [v0, angleDegrees, q, eField, m, y0, timeOfFlight]);

  const speedPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    const T = Math.max(timeOfFlight, 0.1);
    for (let i = 0; i <= 60; i++) {
      const t = (T * i) / 60;
      list.push({ t, y: charge.speedAt(v0, angleDegrees, q, eField, m, t) });
    }
    return list;
  }, [v0, angleDegrees, q, eField, m, timeOfFlight]);

  const tEff = Math.min(Math.max(0, currentTime), Math.max(timeOfFlight, 0));
  const showIdle = !isPlaying && currentTime <= 1e-9;
  const pos = charge.positionAt(v0, angleDegrees, q, eField, m, tEff, y0);
  const vel = charge.velocityAt(v0, angleDegrees, q, eField, m, tEff);
  const currentSpeed = charge.speedAt(v0, angleDegrees, q, eField, m, tEff);
  const explanation =
    extraExplanation ??
    localExplanation(q, eField, m, v0, angleDegrees, y0, accel, aY, timeOfFlight, rangeM, heightM);
  const canPlay = timeOfFlight > 1e-6;
  const motionState = (() => {
    if (isPlaying) return "Полёт";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (timeOfFlight > 0 && currentTime >= timeOfFlight - 1e-6) return "На поверхности";
    return "Пауза";
  })();

  useEffect(() => {
    paramsRef.current = { q, eField, m, v0, angleDegrees, y0 };
  }, [q, eField, m, v0, angleDegrees, y0]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const p = paramsRef.current;
      const tMax = charge.timeOfFlight(p.v0, p.angleDegrees, p.q, p.eField, p.m, p.y0);
      if (tMax <= 0) {
        playingRef.current = false;
        setIsPlaying(false);
        return;
      }
      const next = timeRef.current + dt;
      if (next >= tMax) {
        timeRef.current = tMax;
        setCurrentTime(tMax);
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({
          vmax: charge.speedAt(p.v0, p.angleDegrees, p.q, p.eField, p.m, tMax),
          t: tMax,
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

  const resetMotion = () => {
    playingRef.current = false;
    setIsPlaying(false);
    timeRef.current = 0;
    setCurrentTime(0);
    setExtraExplanation(null);
    setRunSummary(null);
  };

  const setQ = (value: number) => {
    setQState(clamp(value, -1e-3, 1e-3));
    resetMotion();
  };
  const setEField = (value: number) => {
    setEState(clamp(value, 0.1, 1e6));
    resetMotion();
  };
  const setM = (value: number) => {
    setMState(clamp(value, 1e-9, 1));
    resetMotion();
  };
  const setV0 = (value: number) => {
    setV0State(clamp(value, 0.1, 500));
    resetMotion();
  };
  const setAngleDegrees = (value: number) => {
    setAngleState(clamp(value, 1, 89));
    resetMotion();
  };
  const setY0 = (value: number) => {
    setY0State(clamp(value, 0, 200));
    resetMotion();
  };

  const placeY0 = (h: number) => {
    if (playingRef.current) return;
    setY0State(clamp(h, 0, 200));
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setY0State(0);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (charge.timeOfFlight(v0, angleDegrees, q, eField, m, y0) <= 1e-6) return;
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
        `Объясни кратко по-русски движение заряда в однородном электрическом поле (аналог брошенного тела). ` +
        `Параметры: q = ${q.toExponential(2)} Кл, E = ${eField.toFixed(0)} В/м, m = ${m.toExponential(2)} кг, v₀ = ${v0.toFixed(1)} м/с, угол α = ${angleDegrees.toFixed(0)}°, y₀ = ${y0.toFixed(1)} м. ` +
        `Ускорение a = qE/m = ${accel.toFixed(2)} м/с². Время полёта T = ${timeOfFlight.toFixed(2)} с, дальность L = ${rangeM.toFixed(2)} м, макс. высота H = ${heightM.toFixed(2)} м. Дай короткое объяснение с формулами.`;
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    q,
    eField,
    m,
    v0,
    angleDegrees,
    y0,
    setQ,
    setEField,
    setM,
    setV0,
    setAngleDegrees,
    setY0,
    placeY0,
    accel,
    aY,
    timeOfFlight,
    rangeM,
    heightM,
    trajectory,
    heightPoints,
    speedPoints,
    currentTime,
    currentX: showIdle ? 0 : pos.x,
    currentY: showIdle ? y0 : Math.max(0, pos.y),
    currentSpeed,
    currentVx: vel.vx,
    currentVy: vel.vy,
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
