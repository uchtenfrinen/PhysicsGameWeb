import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as projectile from "../../physics/projectileMotion.ts";
import { requestExplain } from "../groq.ts";

function localExplanation(
  v0: number,
  angle: number,
  y0: number,
  range: number,
  height: number,
  time: number,
) {
  const rad = (angle * Math.PI) / 180;
  const v0x = v0 * Math.cos(rad);
  const v0y = v0 * Math.sin(rad);
  const timeFormula = y0 <= 0 ? "T = 2v₀y/g" : "T = (v₀y + √(v₀y² + 2gy₀))/g";
  return (
    `Тело брошено под углом α = ${angle.toFixed(0)}° с v₀ = ${v0.toFixed(1)} м/с, y₀ = ${y0.toFixed(1)} м. ` +
    `v₀x = ${v0x.toFixed(2)} м/с, v₀y = ${v0y.toFixed(2)} м/с. ` +
    `x(t) = v₀x·t, y(t) = y₀ + v₀y·t − gt²/2. ` +
    `${timeFormula} = ${time.toFixed(2)} с, L = ${range.toFixed(2)} м, H = ${height.toFixed(2)} м.`
  );
}

export function useProjectile() {
  const [v0, setV0State] = useState(15);
  const [angleDegrees, setAngleState] = useState(45);
  const [y0, setY0State] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const paramsRef = useRef({ v0: 15, angleDegrees: 45, y0: 0 });

  const timeOfFlight = useMemo(
    () => projectile.timeOfFlight(v0, angleDegrees, y0),
    [v0, angleDegrees, y0],
  );
  const rangeM = useMemo(() => projectile.range(v0, angleDegrees, y0), [v0, angleDegrees, y0]);
  const heightM = useMemo(() => projectile.maxHeight(v0, angleDegrees, y0), [v0, angleDegrees, y0]);
  const trajectory = useMemo(
    () => projectile.getTrajectoryPoints(v0, angleDegrees, y0),
    [v0, angleDegrees, y0],
  );
  const heightPoints = useMemo(
    () => projectile.getHeightTimePoints(v0, angleDegrees, y0),
    [v0, angleDegrees, y0],
  );
  const speedPoints = useMemo(
    () => projectile.getSpeedTimePoints(v0, angleDegrees, y0),
    [v0, angleDegrees, y0],
  );

  const tEff = Math.min(Math.max(0, currentTime), timeOfFlight);
  const pos = projectile.positionAt(v0, angleDegrees, tEff, y0);
  const vel = projectile.velocityAt(v0, angleDegrees, tEff);
  const speed = projectile.speedAt(v0, angleDegrees, tEff);
  const impact = projectile.speedAt(v0, angleDegrees, timeOfFlight);
  const explanation = extraExplanation ?? localExplanation(v0, angleDegrees, y0, rangeM, heightM, timeOfFlight);
  const canPlay = timeOfFlight > 1e-6;
  const motionState = (() => {
    if (isPlaying) return "Полёт";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (timeOfFlight > 0 && currentTime >= timeOfFlight - 1e-6) return "На поверхности";
    return "Пауза";
  })();

  useEffect(() => {
    paramsRef.current = { v0, angleDegrees, y0 };
  }, [v0, angleDegrees, y0]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const p = paramsRef.current;
      const tMax = projectile.timeOfFlight(p.v0, p.angleDegrees, p.y0);
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
          vmax: projectile.speedAt(p.v0, p.angleDegrees, tMax),
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

  const setV0 = (value: number) => {
    setV0State(clamp(value, 0.1, 200));
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
    if (projectile.timeOfFlight(v0, angleDegrees, y0) <= 1e-6) return;
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
        `Объясни кратко по-русски движение тела, брошенного под углом к горизонту. ` +
        `Параметры: v₀ = ${v0.toFixed(1)} м/с, угол α = ${angleDegrees.toFixed(0)}°, начальная высота y₀ = ${y0.toFixed(1)} м. ` +
        `Рассчитанные результаты (используй в объяснении именно эти числа): дальность полёта L = ${rangeM.toFixed(2)} м, макс. высота H = ${heightM.toFixed(2)} м, время полёта T = ${timeOfFlight.toFixed(2)} с. ` +
        "Дай короткое объяснение с формулами, везде подставляя указанные L, H и T.";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    v0,
    angleDegrees,
    y0,
    setV0,
    setAngleDegrees,
    setY0,
    placeY0,
    timeOfFlight,
    rangeM,
    heightM,
    trajectory,
    heightPoints,
    speedPoints,
    currentTime,
    currentX: pos.x,
    currentY: Math.max(0, pos.y),
    currentSpeed: speed,
    currentVx: vel.vx,
    currentVy: vel.vy,
    impact,
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
