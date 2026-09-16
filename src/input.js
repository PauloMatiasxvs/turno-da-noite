import { $, clamp } from "./utils.js";
import { canvas } from "./gl.js";
import { player } from "./entities.js";
import { G, pauseGame, resumeGame, startGame, feed } from "./game-state.js";
import { startReload, swapTo } from "./combat.js";
import { audioInit, toggleMute } from "./audio.js";
/* =======================================================================
   6 · ENTRADA
   ======================================================================= */
const keys = {};
let mouseDown = false, noLock = false, lastMX = 0, lastMY = 0, haveLast = false;
const SENS = .0022;

addEventListener("keydown", e => {
  if (e.code === "Escape") return;
  if (["Space","KeyW","KeyA","KeyS","KeyD","Tab","KeyR"].indexOf(e.code) >= 0) e.preventDefault();
  if (keys[e.code]) return;
  keys[e.code] = true;
  if (G.state !== "play") return;
  if (e.code === "KeyR") startReload();
  if (e.code === "Digit1") swapTo(0);
  if (e.code === "Digit2") swapTo(1);
  if (e.code === "KeyM"){
    feed(toggleMute() ? "<b>SOM</b> desligado" : "<b>SOM</b> ligado");
  }
});
addEventListener("keyup", e => { keys[e.code] = false; });
addEventListener("blur", () => { for (const k in keys) keys[k] = false; mouseDown = false; });

canvas.addEventListener("mousedown", e => {
  if (G.state === "play" && e.button === 0){ mouseDown = true; e.preventDefault(); }
});
addEventListener("mouseup", e => { if (e.button === 0) mouseDown = false; });
addEventListener("contextmenu", e => { if (G.state === "play") e.preventDefault(); });

// Sem captura de ponteiro (ex.: rodando dentro de um iframe) o mouse esbarra na
// borda da janela. Nesse caso a posição do cursor vira *velocidade* de giro,
// como um analógico: afastou do centro, a câmera gira para aquele lado.
let aimX = 0, aimY = 0;
addEventListener("mousemove", e => {
  if (G.state !== "play") return;
  if (document.pointerLockElement === canvas){
    player.yaw   -= e.movementX * SENS;
    player.pitch -= e.movementY * SENS;
    player.pitch  = clamp(player.pitch, -1.45, 1.45);
  } else if (noLock){
    // janela minimizada ou ainda sem layout reporta 0 — sem o guarda isso vira NaN
    const vw = innerWidth || 1, vh = innerHeight || 1;
    aimX = clamp((e.clientX / vw) * 2 - 1, -1, 1);
    aimY = clamp((e.clientY / vh) * 2 - 1, -1, 1);
    if (!haveLast){ haveLast = true; document.body.classList.add("nolock"); $("#nolockHint").classList.add("on"); }
  }
});
function aimDrift(dt){
  if (!noLock || document.pointerLockElement) return;
  const dz = .14;
  const ax = Math.abs(aimX) > dz ? Math.sign(aimX) * (Math.abs(aimX)-dz) / (1-dz) : 0;
  const ay = Math.abs(aimY) > dz ? Math.sign(aimY) * (Math.abs(aimY)-dz) / (1-dz) : 0;
  player.yaw   -= ax * ax * Math.sign(ax) * 3.4 * dt;
  player.pitch -= ay * ay * Math.sign(ay) * 2.2 * dt;
  player.pitch  = clamp(player.pitch, -1.45, 1.45);
}

function lockPointer(){
  haveLast = false;
  if (!canvas.requestPointerLock){ noLock = true; return; }
  try {
    const req = canvas.requestPointerLock();
    if (req && req.catch) req.catch(() => { noLock = true; });
  } catch(e){ noLock = true; }
}
document.addEventListener("pointerlockerror", () => { noLock = true; });
document.addEventListener("pointerlockchange", () => {
  if (!document.pointerLockElement && G.state === "play" && !noLock) pauseGame();
});


export { keys, mouseDown, noLock, aimDrift, lockPointer };
