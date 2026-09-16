import { $, rand } from "./utils.js";
import { buildArena } from "./world.js";
import { player, WEAPONS, enemies, bolts, pickups, parts, beams } from "./entities.js";
import { SFX, audioInit } from "./audio.js";
import { lockPointer } from "./input.js";
/* =======================================================================
   5 · ESTADO DO JOGO
   ======================================================================= */
const G = {
  state:"menu", wave:0, score:0, kills:0, shots:0, hits:0,
  combo:1, comboT:0, queue:[], betweenT:0, time:0, best:0, hitMark:0, killMark:0
};
try { G.best = parseInt(localStorage.getItem("neonArenaBest") || "0", 10) || 0; } catch(e){}

function banner(text, subText, hold){
  const b = $("#banner"), s = $("#sub");
  b.textContent = text; b.classList.add("on");
  s.textContent = subText || ""; s.classList.toggle("on", !!subText);
  clearTimeout(banner._t);
  banner._t = setTimeout(() => { b.classList.remove("on"); s.classList.remove("on"); }, hold || 1600);
}
function feed(html){
  const d = document.createElement("div");
  d.innerHTML = html;
  $("#feed").appendChild(d);
  setTimeout(() => d.remove(), 2600);
}

function startWave(n){
  G.wave = n;
  G.queue = [];
  const count = Math.min(4 + Math.round(n * 1.7), 28);
  let t = 0;
  for (let i=0;i<count;i++){
    let type = "drone";
    const r = Math.random();
    if (n >= 2 && r < .38) type = "runner";
    if (n >= 3 && r > .88) type = "brute";
    if (n >= 7 && r > .78) type = "brute";
    G.queue.push({ type, t });
    t += rand(.2, .75);
  }
  banner("ONDA " + n, count + " HOSTIS DETECTADOS", 2000);
  SFX.wave();
}

function startGame(){
  buildArena();
  // esvazia no lugar: outros módulos guardam a referência destes arrays
  [enemies, bolts, pickups, parts, beams].forEach(a => { a.length = 0; });
  player.p = [0, 0, 20]; player.vy = 0; player.yaw = 0; player.pitch = 0;
  player.hp = player.maxHp; player.wep = 0; player.fireT = 0; player.reloadT = 0;
  player.kick = 0; player.hurtT = 0;
  WEAPONS.forEach(w => { w.ammo = w.mag; });
  WEAPONS[0].res = 160; WEAPONS[1].res = 36;
  G.score = 0; G.kills = 0; G.shots = 0; G.hits = 0; G.combo = 1; G.comboT = 0;
  G.betweenT = 2.4; G.wave = 0; G.state = "play";
  $("#feed").innerHTML = "";
  document.body.classList.add("playing");
  document.body.classList.remove("paused");
  banner("PREPARE-SE", "", 1800);
}

function gameOver(){
  G.state = "dead";
  SFX.dead();
  document.body.classList.remove("playing");
  if (G.score > G.best){
    G.best = G.score;
    try { localStorage.setItem("neonArenaBest", String(G.best)); } catch(e){}
  }
  if (document.pointerLockElement) document.exitPointerLock();
  const acc = G.shots ? Math.round(G.hits / G.shots * 100) : 0;
  $("#panelBody").innerHTML = `
    <h1>SISTEMA <span style="color:var(--red);text-shadow:0 0 30px rgba(255,59,82,.5)">OFFLINE</span></h1>
    <div class="tag">Você caiu na onda ${G.wave}</div>
    <div class="stats">
      <div><b>${G.score.toLocaleString("pt-BR")}</b><span>Pontos</span></div>
      <div><b>${G.wave}</b><span>Ondas</span></div>
      <div><b>${G.kills}</b><span>Abates</span></div>
      <div><b>${acc}%</b><span>Precisão</span></div>
    </div>
    <div class="tag" style="margin-bottom:22px">Recorde: ${G.best.toLocaleString("pt-BR")} pts</div>
    <button id="startBtn">Tentar de novo</button>`;
  bindStart();
}

function pauseGame(){
  if (G.state !== "play") return;
  G.state = "pause";
  document.body.classList.remove("playing");
  document.body.classList.add("paused");
  $("#panelBody").innerHTML = `
    <h1>EM <span>PAUSA</span></h1>
    <div class="tag">Onda ${G.wave} · ${G.score.toLocaleString("pt-BR")} pontos</div>
    <div class="keys">
      <div><kbd>W A S D</kbd> mover</div><div><kbd>Mouse</kbd> mirar</div>
      <div><kbd>Clique</kbd> atirar</div><div><kbd>Shift</kbd> correr</div>
      <div><kbd>Espaço</kbd> pular</div><div><kbd>R</kbd> recarregar</div>
      <div><kbd>1</kbd> <kbd>2</kbd> trocar arma</div><div><kbd>M</kbd> mudo</div>
    </div>
    <button id="startBtn">Continuar</button>`;
  bindStart();
}
function resumeGame(){
  G.state = "play";
  document.body.classList.add("playing");
  document.body.classList.remove("paused");
  lockPointer();
}

function bindStart(){
  const b = $("#startBtn");
  if (!b) return;
  b.onclick = () => {
    audioInit();
    if (G.state === "pause") resumeGame();
    else { startGame(); lockPointer(); }
  };
}
// bindStart() é chamado pelo main.js, depois que o WebGL sobe


export { G, banner, feed, startWave, startGame, gameOver, pauseGame, resumeGame, bindStart };
