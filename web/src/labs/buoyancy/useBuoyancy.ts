import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as buoyancy from "../../physics/buoyancy.ts";
import { requestExplain } from "../groq.ts";

const FRAC_RATE = 0.55;

function bodyHeightM(volume: number) {
  return Math.cbrt(Math.max(volume, 1e-6));
}

function localExplanation(
  rhoBody: number,
  rhoFluid: number,
  volume: number,
  weightN: number,
  fa: number,
  frac: number,
  eta0: number,
  status: string,
) {
  const vSub = volume * frac;
  return (
    `Закон Архимеда: F_A = ρж·g·Vпогр. ` +
    `ρт = ${rhoBody.toFixed(0)} кг/м³, ρж = ${rhoFluid.toFixed(0)} кг/м³, V = ${volume.toFixed(4)} м³, η₀ = ${eta0.toFixed(2)}. ` +
    `P = ρт·g·V = ${weightN.toFixed(2)} Н, F_A = ${fa.toFixed(2)} Н (Vпогр = ${vSub.toFixed(4)} м³). ` +
    `Доля погружения Vпогр/V = ρт/ρж = ${(frac * 100).toFixed(1)}%. ${status}.`
  );
}

function fractionPoints(rhoFluid: number) {
  const list: { t: number; y: number }[] = [];
  const max = Math.max(rhoFluid * 1.4, 200);
  for (let i = 0; i < 60; i++) {
    const rho = 1 + ((max - 1) * i) / 59;
    list.push({ t: rho, y: buoyancy.submergedFraction(rho, rhoFluid) });
  }
  return list;
}

function forcePoints(rhoFluid: number, volume: number) {
  const list: { t: number; y: number }[] = [];
  const max = Math.max(rhoFluid * 1.4, 200);
  for (let i = 0; i < 60; i++) {
    const rho = 1 + ((max - 1) * i) / 59;
    const frac = buoyancy.submergedFraction(rho, rhoFluid);
    list.push({ t: rho, y: buoyancy.archimedesForce(rhoFluid, volume * frac) });
  }
  return list;
}

export function useBuoyancy() {
  const [rhoBody, setRhoBodyState] = useState(600);
  const [rhoFluid, setRhoFluidState] = useState(1000);
  const [volume, setVolumeState] = useState(0.001);
  const [eta0, setEta0State] = useState(0);
  const [currentFrac, setCurrentFrac] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const fracRef = useRef(0);
  const timeRef = useRef(0);
  const vmaxRef = useRef(0);
  const volumeRef = useRef(0.001);
  const targetRef = useRef(0);

  const frac = useMemo(() => buoyancy.submergedFraction(rhoBody, rhoFluid), [rhoBody, rhoFluid]);
  const vSub = volume * frac;
  const fa = buoyancy.archimedesForce(rhoFluid, vSub);
  const showFrac = isPlaying || currentTime > 1e-9 ? currentFrac : eta0;
  const faNow = buoyancy.archimedesForce(rhoFluid, volume * Math.min(1, Math.max(0, showFrac)));
  const weightN = buoyancy.weight(rhoBody, volume);
  const sinking = buoyancy.isSinking(rhoBody, rhoFluid);
  const floating = buoyancy.isFloating(rhoBody, rhoFluid);
  const status = sinking ? "тело тонет (ρт > ρж)" : floating ? "куб плавает (ρт < ρж)" : "безразличное равновесие";
  const etaPoints = useMemo(() => fractionPoints(rhoFluid), [rhoFluid]);
  const faPoints = useMemo(() => forcePoints(rhoFluid, volume), [rhoFluid, volume]);
  const explanation = extraExplanation ?? localExplanation(rhoBody, rhoFluid, volume, weightN, fa, frac, eta0, status);
  const canPlay = Math.abs(eta0 - frac) >= 0.002;
  const bodyH = bodyHeightM(volume);
  const currentV = (() => {
    if (!isPlaying) return 0;
    const diff = frac - showFrac;
    if (Math.abs(diff) < 0.002) return 0;
    return Math.sign(diff) * FRAC_RATE * bodyH;
  })();
  const motionState = (() => {
    if (isPlaying) return "К равновесию";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (Math.abs(currentFrac - frac) < 0.002) return status;
    return "Пауза";
  })();

  targetRef.current = frac;
  volumeRef.current = volume;

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const target = targetRef.current;
      const H = bodyHeightM(volumeRef.current);
      const diff = target - fracRef.current;
      const speed = FRAC_RATE * H;
      vmaxRef.current = Math.max(vmaxRef.current, speed);
      timeRef.current += dt;
      setCurrentTime(timeRef.current);
      if (Math.abs(diff) < 0.002) {
        fracRef.current = target;
        setCurrentFrac(target);
        playingRef.current = false;
        setIsPlaying(false);
        setRunSummary({ vmax: vmaxRef.current, t: timeRef.current });
        return;
      }
      fracRef.current += Math.sign(diff) * Math.min(FRAC_RATE * dt, Math.abs(diff));
      setCurrentFrac(fracRef.current);
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
    vmaxRef.current = 0;
    setExtraExplanation(null);
    setRunSummary(null);
  };

  const setRhoBody = (value: number) => {
    setRhoBodyState(clamp(value, 1, 20000));
    resetMotion();
  };
  const setRhoFluid = (value: number) => {
    setRhoFluidState(clamp(value, 1, 20000));
    resetMotion();
  };
  const setVolume = (value: number) => {
    const next = clamp(value, 1e-6, 100);
    volumeRef.current = next;
    setVolumeState(next);
    resetMotion();
  };
  const setEta0 = (value: number) => {
    const next = clamp(value, 0, 1);
    setEta0State(next);
    fracRef.current = next;
    setCurrentFrac(next);
    resetMotion();
  };

  const placeFrac = (value: number) => {
    if (playingRef.current) return;
    const next = clamp(value, 0, 1);
    setEta0State(next);
    fracRef.current = next;
    setCurrentFrac(next);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const build = () => {
    setEta0State(0);
    fracRef.current = 0;
    setCurrentFrac(0);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (Math.abs(eta0 - frac) < 0.002) return;
    fracRef.current = eta0;
    setCurrentFrac(eta0);
    timeRef.current = 0;
    setCurrentTime(0);
    vmaxRef.current = 0;
    setRunSummary(null);
    playingRef.current = true;
    setIsPlaying(true);
  };

  const explainGroq = async () => {
    setBusy(true);
    setExtraExplanation("Запрос к нейросети…");
    try {
      const prompt =
        `Объясни кратко по-русски закон Архимеда и условия плавания. ` +
        `Параметры: плотность тела ρ_т = ${rhoBody.toFixed(0)} кг/м³, плотность жидкости ρ_ж = ${rhoFluid.toFixed(0)} кг/м³, объём тела V = ${volume.toFixed(4)} м³, начальная доля погружения η₀ = ${eta0.toFixed(2)}. ` +
        `Вес P = ${weightN.toFixed(2)} Н, сила Архимеда F_A = ${fa.toFixed(2)} Н, доля погружения = ${(frac * 100).toFixed(1)}%. ${status}. ` +
        "Дай короткое объяснение с формулами.";
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    rhoBody,
    rhoFluid,
    volume,
    eta0,
    setRhoBody,
    setRhoFluid,
    setVolume,
    setEta0,
    placeFrac,
    frac,
    currentFrac,
    showFrac,
    currentTime,
    currentV,
    fa,
    faNow,
    weightN,
    status,
    etaPoints,
    faPoints,
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
