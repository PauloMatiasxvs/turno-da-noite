import { $, clamp } from "./utils.js";
import { G } from "./game-state.js";
import { player, WEAPONS } from "./entities.js";
/* =======================================================================
   9 · HUD
   ======================================================================= */
const elWave = $("#waveNum"), elScore = $("#score"), elHp = $("#hpFill"),
      elAmmo = $("#ammo"), elWep = $("#wep"), elReload = $("#reload"),
      elCombo = $("#combo"), elCross = $("#cross"), elVig = $("#vig"),
      elLow = $("#lowhp"), elArrow = $("#arrowWrap"), elFps = $("#fps");

function updateHud(dt){
  const w = WEAPONS[player.wep];
  elWave.textContent = G.wave;
  elScore.textContent = G.score.toLocaleString("pt-BR");
  const hpPct = clamp(player.hp / player.maxHp, 0, 1);
  elHp.style.width = (hpPct*100) + "%";
  elHp.classList.toggle("low", hpPct < .35);
  elAmmo.innerHTML = w.ammo + "<small>/" + w.res + "</small>";
  elAmmo.classList.toggle("empty", w.ammo === 0);
  elWep.textContent = w.name;
  elReload.textContent = player.reloadT > 0 ? "RECARREGANDO" : (w.ammo === 0 ? "APERTE R" : "");

  G.comboT = Math.max(0, G.comboT - dt);
  if (G.comboT === 0) G.combo = 1;
  elCombo.textContent = "COMBO x" + G.combo;
  elCombo.classList.toggle("on", G.combo > 1);

  // mira: abre com o movimento e com a dispersão da arma
  const spread = 5 + w.spread*190 + player.kick*2.2 + (player.speed > 8 ? 4 : player.speed > 0 ? 2 : 0);
  elCross.style.setProperty("--sp", spread.toFixed(1) + "px");
  G.hitMark = Math.max(0, G.hitMark - dt);
  G.killMark = Math.max(0, G.killMark - dt);
  elCross.classList.toggle("hit", G.hitMark > 0 && G.killMark === 0);
  elCross.classList.toggle("kill", G.killMark > 0);

  player.hurtT = Math.max(0, player.hurtT - dt);
  elVig.style.opacity = (player.hurtT / .45 * .9).toFixed(2);
  elArrow.style.opacity = player.hurtT > 0 ? "1" : "0";
  elArrow.style.transform = "rotate(" + player.hurtDir + "rad)";
  elLow.style.opacity = hpPct < .3 ? "1" : "0";
}


export { updateHud, elFps };
