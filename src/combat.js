import { clamp, norm3, cross3, add3, mul3, sub3, len3, dot3 } from "./utils.js";
import { player, WEAPONS, enemies, ETYPE, burst, beam, dropPickup } from "./entities.js";
import { boxes, rayAABB, raySphere } from "./world.js";
import { G, feed, gameOver } from "./game-state.js";
import { SFX } from "./audio.js";
/* =======================================================================
   7 · COMBATE
   ======================================================================= */
function camDir(){
  const cy = Math.cos(player.pitch), sy = Math.sin(player.pitch);
  return [ -Math.sin(player.yaw)*cy, sy, -Math.cos(player.yaw)*cy ];
}
function eyePos(){ return [ player.p[0], player.p[1] + player.eye, player.p[2] ]; }

function swapTo(i){
  if (i === player.wep || player.reloadT > 0) return;
  player.wep = i;
  player.fireT = Math.max(player.fireT, .25);
  SFX.swap();
}
function startReload(){
  const w = WEAPONS[player.wep];
  if (player.reloadT > 0 || w.ammo >= w.mag || w.res <= 0) return;
  player.reloadT = w.reload;
  SFX.reload();
}
function finishReload(){
  const w = WEAPONS[player.wep];
  const need = Math.min(w.mag - w.ammo, w.res);
  w.ammo += need; w.res -= need;
}

function addScore(n){
  G.score += Math.round(n * G.combo);
}

function tryFire(){
  const w = WEAPONS[player.wep];
  if (player.fireT > 0 || player.reloadT > 0) return;
  if (w.ammo <= 0){
    player.fireT = .3;
    SFX.dry();
    if (w.res > 0) startReload();
    return;
  }
  w.ammo--;
  player.fireT = w.rate;
  player.kick = Math.min(player.kick + w.kick, 6);
  player.pitch = clamp(player.pitch + w.kick * .0055, -1.45, 1.45);
  SFX[w.sfx]();
  G.shots++;

  const o = eyePos(), d = camDir();
  const right = norm3(cross3(d, [0,1,0]));
  const up = cross3(right, d);
  let anyHit = false;

  for (let i=0;i<w.pellets;i++){
    const sx = (Math.random()+Math.random()-1) * w.spread;
    const sy = (Math.random()+Math.random()-1) * w.spread;
    const dir = norm3([
      d[0] + right[0]*sx + up[0]*sy,
      d[1] + right[1]*sx + up[1]*sy,
      d[2] + right[2]*sx + up[2]*sy
    ]);

    let best = w.range, target = null;
    for (const b of boxes){
      const t = rayAABB(o, dir, b.min, b.max);
      if (t < best){ best = t; target = null; }
    }
    for (const e of enemies){
      if (e.dead || e.birth < .25) continue;
      const t = raySphere(o, dir, e.p, e.r);
      if (t < best){ best = t; target = e; }
    }

    const hp = [ o[0]+dir[0]*best, o[1]+dir[1]*best, o[2]+dir[2]*best ];
    beam(add3(o, mul3(dir, .6)), hp, target ? [1,.85,.4] : [.55,.8,1], target ? .035 : .018);

    if (target){
      // acerto no núcleo (centro da esfera) causa dano dobrado
      const dc = len3(sub3(hp, target.p));
      const crit = dc < target.r * .5;
      damageEnemy(target, w.dmg * (crit ? 2 : 1), dir, crit);
      anyHit = true;
      burst(hp, crit ? 9 : 5, crit ? [1,.9,.45] : ETYPE[target.type].col, 5, .3, 0);
    } else if (best < w.range){
      burst(hp, 4, [.6,.8,1], 3.5, .26, 1);
    }
  }
  if (anyHit) G.hits++;
}

function damageEnemy(e, dmg, dir, crit){
  if (e.dead) return;
  e.hp -= dmg;
  e.flash = 1;
  e.vel[0] += dir[0] * (crit ? 5 : 2.6) / (e.r*2);
  e.vel[2] += dir[2] * (crit ? 5 : 2.6) / (e.r*2);
  if (crit) SFX.crit(); else SFX.hit();
  G.hitMark = .12;
  if (e.hp <= 0) killEnemy(e);
}

function killEnemy(e){
  e.dead = true;
  const T = ETYPE[e.type];
  G.kills++;
  addScore(T.score);                       // pontua com o combo atual...
  G.comboT = 3.2;
  G.combo = Math.min(G.combo + 1, 8);      // ...e só depois sobe o multiplicador
  SFX.kill();
  G.killMark = .2;
  burst(e.p, 26, T.col, 9, .7, 1);
  burst(e.p, 10, T.core, 12, .5, 0);
  dropPickup(e.p);
  if (G.combo >= 3) feed(`<b>x${G.combo}</b> ${e.type.toUpperCase()}`);
}

function hurtPlayer(dmg, from){
  if (G.state !== "play") return;
  player.hp -= dmg;
  player.hurtT = .45;
  const d = norm3([ from[0]-player.p[0], 0, from[2]-player.p[2] ]);
  const f = [ -Math.sin(player.yaw), 0, -Math.cos(player.yaw) ];
  const r = [  Math.cos(player.yaw), 0, -Math.sin(player.yaw) ];
  player.hurtDir = Math.atan2(dot3(d, r), dot3(d, f));
  G.combo = 1; G.comboT = 0;
  SFX.hurt();
  if (player.hp <= 0){ player.hp = 0; gameOver(); }
}


export { camDir, eyePos, swapTo, startReload, finishReload, addScore, tryFire, damageEnemy, killEnemy, hurtPlayer };
