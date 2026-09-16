/* =======================================================================
   11 · PONTO DE ENTRADA — sobe o WebGL e roda o laço principal
   ======================================================================= */
import { $, clamp, lerp } from "./utils.js";
import { initGL, resize } from "./gl.js";
import { player, enemies } from "./entities.js";
import { G, bindStart } from "./game-state.js";
import { updatePlayer, updateEnemies, updateBolts, updateFx, updateWaves } from "./update.js";
import { updateHud, elFps } from "./hud.js";
import { render } from "./render.js";
import "./input.js";           // só registra os listeners

if (!initGL()){
  $("#panelBody").style.display = "none";
  $("#err").style.display = "block";
  $("#err").innerHTML = "<b>Seu navegador não tem WebGL2.</b><br>Abra no Chrome, Edge ou Firefox atualizado.";
} else {
  bindStart();
  requestAnimationFrame(frame);
}
let last = performance.now(), fpsAcc = 0, fpsN = 0;
function frame(now){
  requestAnimationFrame(frame);
  let dt = (now - last) / 1000;
  last = now;
  if (dt > .05) dt = .05;

  resize();

  if (G.state === "play"){
    G.time += dt;
    // suavização do "sway" da arma conforme o giro da mira
    player.sway[0] = lerp(player.sway[0], clamp((player.yaw - (player._pyaw || player.yaw)) * 7, -.06, .06), .25);
    player.sway[1] = lerp(player.sway[1], clamp((player.pitch - (player._ppitch || player.pitch)) * 7, -.06, .06), .25);
    player._pyaw = player.yaw; player._ppitch = player.pitch;

    updatePlayer(dt);
    updateEnemies(dt);
    updateBolts(dt);
    updateWaves(dt);
    updateFx(dt);
    updateHud(dt);
  } else {
    updateFx(dt * .3);
  }

  render();

  fpsAcc += dt; fpsN++;
  if (fpsAcc > .5){
    elFps.textContent = Math.round(fpsN / fpsAcc) + " FPS · " + enemies.length + " HOSTIS";
    fpsAcc = 0; fpsN = 0;
  }
}
