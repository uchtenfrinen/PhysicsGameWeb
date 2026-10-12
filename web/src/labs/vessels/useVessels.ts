import { useEffect, useMemo, useRef, useState } from "react";
import { clamp } from "../../physics/clamp.ts";
import * as vessels from "../../physics/communicatingVessels.ts";
import { requestExplain } from "../groq.ts";

const FILL_RATE = 0.7;

function localExplanation(rhoLight: number, rhoHeavy: number, hHeavy: number, hLight: number, p: number) {
  return (
    `Сообщающиеся сосуды, две жидкости. ρ₁·g·h₁ = ρ₂·g·h₂ ⇒ h₁/h₂ = ρ₂/ρ₁. ` +
    `ρмасла = ${rhoLight.toFixed(0)} кг/м³, ρводы = ${rhoHeavy.toFixed(0)} кг/м³, hводы = ${hHeavy.toFixed(3)} м. ` +
    `hмасла = hводы·(ρводы/ρмасла) = ${hLight.toFixed(3)} м. ` +
    `p = ρводы·g·hводы = ${p.toFixed(1)} Па.`
  );
}

export function useVessels() {
  const [rhoLight, setRhoLightState] = useState(800);
  const [rhoHeavy, setRhoHeavyState] = useState(1000);
  const [heightHeavy, setHeightHeavyState] = useState(0.1);
  const [shown, setShown] = useState(0);
  const [currentTime, setCurrentTime] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [extraExplanation, setExtraExplanation] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [runSummary, setRunSummary] = useState<{ vmax: number; t: number } | null>(null);
  const playingRef = useRef(false);
  const shownRef = useRef(0);
  const timeRef = useRef(0);
  const vmaxRef = useRef(0);
  const heightLightRef = useRef(0);
  const heightHeavyRef = useRef(0.1);

  const heightLight = useMemo(
    () => vessels.heightLightFromHeavy(rhoLight, rhoHeavy, heightHeavy),
    [rhoLight, rhoHeavy, heightHeavy],
  );
  const pressure = useMemo(
    () => vessels.pressureAtInterface(rhoHeavy, heightHeavy),
    [rhoHeavy, heightHeavy],
  );
  const rhoPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 80; i++) {
      const rho = 200 + ((1200 - 200) * i) / 80;
      list.push({ t: rho, y: vessels.heightLightFromHeavy(rho, rhoHeavy, heightHeavy) });
    }
    return list;
  }, [rhoHeavy, heightHeavy]);
  const pPoints = useMemo(() => {
    const list: { t: number; y: number }[] = [];
    for (let i = 0; i <= 60; i++) {
      const h = (0.25 * i) / 60;
      list.push({ t: h, y: vessels.pressureAtInterface(rhoHeavy, h) });
    }
    return list;
  }, [rhoHeavy]);

  const preview = !isPlaying && shown <= 1e-6;
  const hL = heightLight * (preview ? 1 : shown);
  const hH = heightHeavy * (preview ? 1 : shown);
  const explanation = extraExplanation ?? localExplanation(rhoLight, rhoHeavy, heightHeavy, heightLight, pressure);
  const canPlay = heightHeavy >= 1e-4 && heightLight > 1e-9;
  const currentVLight = isPlaying ? FILL_RATE * heightLight : 0;
  const currentVHeavy = isPlaying ? FILL_RATE * heightHeavy : 0;
  const motionState = (() => {
    if (isPlaying) return "Установление";
    if (currentTime <= 1e-9) return "Ожидание запуска";
    if (shown >= 1 - 1e-6) return "Равновесие";
    return "Пауза";
  })();

  heightLightRef.current = heightLight;
  heightHeavyRef.current = heightHeavy;

  useEffect(() => {
    if (!isPlaying) return;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
      last = now;
      const speed = FILL_RATE * Math.max(heightLightRef.current, heightHeavyRef.current);
      vmaxRef.current = Math.max(vmaxRef.current, speed);
      timeRef.current += dt;
      setCurrentTime(timeRef.current);
      const next = Math.min(1, shownRef.current + dt * FILL_RATE);
      shownRef.current = next;
      setShown(next);
      if (next >= 1) {
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

  const resetMotion = () => {
    playingRef.current = false;
    setIsPlaying(false);
    shownRef.current = 0;
    setShown(0);
    timeRef.current = 0;
    setCurrentTime(0);
    vmaxRef.current = 0;
    setExtraExplanation(null);
    setRunSummary(null);
  };

  const setRhoLight = (value: number) => {
    setRhoLightState(clamp(value, 1, 20000));
    resetMotion();
  };
  const setRhoHeavy = (value: number) => {
    setRhoHeavyState(clamp(value, 1, 20000));
    resetMotion();
  };
  const setHeightHeavy = (value: number) => {
    setHeightHeavyState(clamp(value, 1e-4, 10));
    resetMotion();
  };

  const placeHeavy = (h: number) => {
    if (playingRef.current) return;
    setHeightHeavyState(clamp(h, 1e-4, 10));
    shownRef.current = 0;
    setShown(0);
    timeRef.current = 0;
    setCurrentTime(0);
    setIsPlaying(false);
    setRunSummary(null);
    setExtraExplanation(null);
  };

  const placeLight = (h: number) => {
    if (playingRef.current) return;
    if (rhoHeavy <= 0) return;
    placeHeavy((h * rhoLight) / rhoHeavy);
  };

  const build = () => {
    setHeightHeavyState(0.1);
    resetMotion();
  };

  const togglePlay = () => {
    if (playingRef.current) {
      playingRef.current = false;
      setIsPlaying(false);
      setRunSummary(null);
      return;
    }
    if (heightHeavy < 1e-4) return;
    shownRef.current = 0;
    setShown(0);
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
        `Объясни кратко по-русски сообщающиеся сосуды с двумя жидкостями. ρ_лёгкая = ${rhoLight.toFixed(0)} кг/м³, ρ_тяжёлая = ${rhoHeavy.toFixed(0)} кг/м³, высота столба тяжёлой h = ${heightHeavy.toFixed(3)} м. Высота лёгкой h_л = ${heightLight.toFixed(3)} м, давление на границе p = ${pressure.toFixed(1)} Па. Дай короткое объяснение.`;
      setExtraExplanation(await requestExplain(prompt));
    } catch (error) {
      setExtraExplanation("Ошибка: " + (error instanceof Error ? error.message : String(error)));
    } finally {
      setBusy(false);
    }
  };

  return {
    rhoLight,
    rhoHeavy,
    heightHeavy,
    heightLight,
    pressure,
    hL,
    hH,
    currentVLight,
    currentVHeavy,
    setRhoLight,
    setRhoHeavy,
    setHeightHeavy,
    placeHeavy,
    placeLight,
    rhoPoints,
    pPoints,
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
