import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as forceCalculator from "../../physics/forceCalculator.ts";
import {
  metersFromNorm,
  motionCurves,
  normFromMeters,
  signedAccel,
  stepAlongPlane,
  willSlide,
} from "../../physics/inclinedPlaneMotion.ts";
import { requestExplain } from "../groq.ts";

const START_U = 0.5;

function localExplanation(
  massKg: number,
  angle: number,
  mu: number,
  lengthM: number,
  v0: number,
  s0: number,
  r: forceCalculator.ForceResult,
  a: number,
) {
  const tanA = Math.tan((angle * Math.PI) / 180);
  const rest = mu >= tanA ? "μ ≥ tg α → покой (пока v = 0)" : "μ < tg α → скольжение";
  return (
    `Груз m = ${massKg.toFixed(2)} кг на плоскости α = ${angle.toFixed(0)}°, μ = ${mu.toFixed(2)}, ` +
    `L = ${lengthM.toFixed(2)} м, s₀ = ${s0.toFixed(2)} м от основания, v₀ = ${v0.toFixed(2)} м/с вниз. ` +
    `F = mg = ${r.weightN.toFixed(1)} Н, N = mg cos α = ${r.normalN.toFixed(1)} Н, ` +
    `mg sin α = ${r.forceAlongPlaneN.toFixed(1)} Н, Fтр = μN = ${r.frictionN.toFixed(1)} Н. ` +
    `${rest}, a = g(sin α − μ cos α) = ${a.toFixed(2)} м/с².`
  );
}

export function useInclinedPlane() {
  const [massKg, setMassKgState] = useState(2);
  const [angleDegrees, setAngleState] = useState(30);
  const [frictionMu, setMuState] = useState(0.5);
  const [lengthM, setLengthState] = useState(2);
  const [v0, setV0State] = useState(0);
  const [alongM, setAlongM] = useState(() => metersFromNorm(START_U, 2));
  const [currentTime, setCurrentTime] = useState(0);
  const [speed, setSpeed] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const playingRef = useRef(false);
  const timeRef = useRef(0);
  const alongRef = useRef(alongM);
  const speedRef = useRef(0);
  const runS0Ref = useRef(alongM);
  const [runS0, setRunS0] = useState(alongM);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const vmaxRef = useRef(0);
  const paramsRef = useRef({ massKg, angleDegrees, frictionMu, lengthM, v0 });

  const forces = useMemo(
    () => forceCalculator.compute(massKg, angleDegrees, frictionMu),
    [massKg, angleDegrees, frictionMu],
  );
  const aSlide = useMemo(
    () => signedAccel(angleDegrees, frictionMu),
    [angleDegrees, frictionMu],
  );
  const canMove = willSlide(v0, angleDegrees, frictionMu);
  const aShow = canMove ? aSlide : 0;
  const s0Chart = isPlaying || currentTime > 1e-9 ? runS0 : alongM;
  const curves = useMemo(
    () => motionCurves(s0Chart, v0, lengthM, angleDegrees, frictionMu),
    [s0Chart, v0, lengthM, angleDegrees, frictionMu],
  );
  const explanation =
    extraExplanation ??
    localExplanation(massKg, angleDegrees, frictionMu, lengthM, v0, alongM, forces, aSlide);
  const timeFor1M = aShow > 0.001 ? Math.sqrt(2 / aShow) : null;
  const position = normFromMeters(alongM, lengthM);
  const motionState = (() => {
    if (isPlaying) return speed < -1e-3 ? "Вверх по плоскости" : "Скользит вниз";
    if (forces.isAtRest && Math.abs(v0) <= 1e-9) return "Покой";
    if (alongM <= 1e-6) return "У основания";
    if (currentTime > 1e-9) return "Пауза";
    return "Ожидание запуска";
  })();

  useEffect(() => {
    paramsRef.current = { massKg, angleDegrees, frictionMu, lengthM, v0 };
  }, [massKg, angleDegrees, frictionMu, lengthM, v0]);

  useEffect(() => {
    alongRef.current = alongM;
  }, [alongM]);

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, (now - last) / 1000);
      last = now;
      const p = paramsRef.current;
      const rest = forceCalculator.compute(p.massKg, p.angleDegrees, p.frictionMu).isAtRest;
      const a = signedAccel(p.angleDegrees, p.frictionMu);
      const vPrev = speedRef.current;
      vmaxRef.current = Math.max(vmaxRef.current, Math.abs(vPrev), Math.abs(vPrev + a * dt));
      const next = stepAlongPlane(alongRef.current, vPrev, dt, p.lengthM, a, rest);
      timeRef.current += dt;
      alongRef.current = next.s;
      speedRef.current = next.v;
      setCurrentTime(timeRef.current);
      setAlongM(next.s);
      setSpeed(next.v);
      if (next.stopped) {
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({ vmax: vmaxRef.current, t: timeRef.current });
        return;
      }
      frame = window.requestAnimationFrame(tick);
    };
    frame = window.requestAnimationFrame(tick);
    return () => window.cancelAnimationFrame(frame);
  }, [isPlaying]);

  const stopMotion = () => {
    playingRef.current = false;
    setIsPlaying(false);
    timeRef.current = 0;
    setCurrentTime(0);
    speedRef.current = 0;
    setSpeed(0);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const setMassKg = (value: number) => {
    setMassKgState(clamp(value, 0.5, 50));
    stopMotion();
  };
  const setAngleDegrees = (value: number) => {
    setAngleState(clamp(value, 10, 60));
    stopMotion();
  };
  const setFrictionMu = (value: number) => {
    setMuState(clamp(value, 0, 1));
    stopMotion();
  };
  const setLengthM = (value: number) => {
    const next = clamp(value, 0.5, 20);
    const u = normFromMeters(alongRef.current, lengthM);
    const s = metersFromNorm(u, next);
    alongRef.current = s;
    runS0Ref.current = s;
    setLengthState(next);
    setAlongM(s);
    setRunS0(s);
    stopMotion();
  };
  const setV0 = (value: number) => {
    setV0State(clamp(value, -10, 10));
    stopMotion();
  };

  const setS0 = (value: number) => {
    const s = clamp(value, 0, paramsRef.current.lengthM);
    alongRef.current = s;
    runS0Ref.current = s;
    setAlongM(s);
    setRunS0(s);
    stopMotion();
  };

  const placeU = (u: number) => {
    if (playingRef.current) return;
    const s = metersFromNorm(u, paramsRef.current.lengthM);
    alongRef.current = s;
    runS0Ref.current = s;
    timeRef.current = 0;
    speedRef.current = 0;
    setAlongM(s);
    setRunS0(s);
    setCurrentTime(0);
    setSpeed(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    const s = metersFromNorm(START_U, lengthM);
    alongRef.current = s;
    runS0Ref.current = s;
    setAlongM(s);
    setRunS0(s);
    stopMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (!willSlide(v0, angleDegrees, frictionMu)) return;
    if (alongRef.current <= 1e-9 && v0 >= -1e-9) return;
    runS0Ref.current = alongRef.current;
    setRunS0(alongRef.current);
    timeRef.current = 0;
    setCurrentTime(0);
    speedRef.current = v0;
    setSpeed(v0);
    vmaxRef.current = Math.abs(v0);
    setRunSummary(null);
    playingRef.current = true;
    setIsPlaying(true);
  };

  const explainGroq = async () => {
    setBusy(true);
    setExtraExplanation("Запрос к нейросети…");
    try {
      const prompt =
        `Объясни кратко и по-русски задачу о грузе на наклонной плоскости. Параметры: масса ${massKg} кг, угол ${angleDegrees}°, коэффициент трения μ = ${frictionMu}, ` +
        `длина плоскости L = ${lengthM.toFixed(2)} м, начальная координата от основания s₀ = ${alongM.toFixed(2)} м, начальная скорость вниз v₀ = ${v0.toFixed(2)} м/с. ` +
        `Рассчитанные силы: F = ${forces.weightN.toFixed(1)} Н, N = ${forces.normalN.toFixed(1)} Н, F вдоль плоскости = ${forces.forceAlongPlaneN.toFixed(1)} Н, F трения = ${forces.frictionN.toFixed(1)} Н. ` +
        `Ускорение: a = g(sin α − μ cos α) = ${aShow.toFixed(2)} м/с². Состояние: ${canMove ? "движение" : "покой"}. ` +
        "Дай короткое структурированное объяснение с формулами и выводом.";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    massKg,
    angleDegrees,
    frictionMu,
    lengthM,
    v0,
    setMassKg,
    setAngleDegrees,
    setFrictionMu,
    setLengthM,
    setV0,
    setS0,
    placeU,
    position,
    alongM,
    runS0,
    currentTime,
    speed,
    forces,
    curves,
    pathM: s0Chart - alongM,
    accelMps2: aShow,
    timeFor1M,
    canPlay: canMove && !(alongM <= 1e-9 && v0 >= -1e-9),
    isPlaying,
    runSummary,
    motionState,
    explanation,
    busy,
    build,
    togglePlay,
    explainGroq,
  };
}
