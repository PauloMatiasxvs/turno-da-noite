import { clamp, lerp, rand, norm3, sub3, add3, mul3, len3 } from "./utils.js";
import { keys, mouseDown, aimDrift } from "./input.js";
import { player, WEAPONS, enemies, bolts, pickups, parts, beams, ETYPE, burst, spawnEnemy } from "./entities.js";
import { ARENA, boxes, groundAt, pushOut, blocked } from "./world.js";
import { tryFire, startReload, finishReload, hurtPlayer, addScore } from "./combat.js";
import { G, banner, feed, startWave } from "./game-state.js";
import { SFX } from "./audio.js";
/* =======================================================================
   8 · ATUALIZAÇÃO
   ======================================================================= */
function updatePlayer(dt){
  aimDrift(dt);
  const sprint = keys.ShiftLeft || keys.ShiftRight;
  const spd = sprint ? 10.6 : 7.1;
  const f = [ -Math.sin(player.yaw), 0, -Math.cos(player.yaw) ];
  const r = [  Math.cos(player.yaw), 0, -Math.sin(player.yaw) ];
  let mx = 0, mz = 0;
  if (keys.KeyW) { mx += f[0]; mz += f[2]; }
  if (keys.KeyS) { mx -= f[0]; mz -= f[2]; }
  if (keys.KeyD) { mx += r[0]; mz += r[2]; }
  if (keys.KeyA) { mx -= r[0]; mz -= r[2]; }
  const ml = Math.hypot(mx, mz);
  if (ml > 0){ mx = mx/ml*spd; mz = mz/ml*spd; }
  player.speed = ml > 0 ? spd : 0;

  // vertical
  player.vy -= 23 * dt;
  player.p[1] += player.vy * dt;
  const g = groundAt(player.p, player.r);
  if (player.p[1] <= g){
    player.p[1] = g;
    if (player.vy < -11) burst([player.p[0], g+.1, player.p[2]], 6, [.4,.7,1], 3, .25, 1);
    player.vy = 0;
    player.onGround = true;
  } else player.onGround = false;

  if (keys.Space && player.onGround){
    player.vy = 8.1;
    player.onGround = false;
  }

  // horizontal + colisão
  player.p[0] += mx * dt;
  player.p[2] += mz * dt;
  pushOut(player.p, player.r, player.p[1], 1.7);
  player.p[0] = clamp(player.p[0], -ARENA+.6, ARENA-.6);
  player.p[2] = clamp(player.p[2], -ARENA+.6, ARENA-.6);

  // balanço de passos
  if (ml > 0 && player.onGround) player.bob += dt * (sprint ? 13 : 9.5);
  else player.bob = lerp(player.bob, Math.round(player.bob/Math.PI)*Math.PI, dt*6);

  // armas
  player.fireT = Math.max(0, player.fireT - dt);
  player.kick  = lerp(player.kick, 0, dt*11);
  if (player.reloadT > 0){
    player.reloadT -= dt;
    if (player.reloadT <= 0){ player.reloadT = 0; finishReload(); }
  }
  const w = WEAPONS[player.wep];
  if (mouseDown && (w.auto || !player._held)) tryFire();
  player._held = mouseDown;
  if (w.ammo === 0 && w.res > 0 && player.reloadT === 0) startReload();

  // pickups
  for (let i = pickups.length-1; i>=0; i--){
    const pk = pickups[i];
    pk.t += dt; pk.life -= dt;
    if (pk.life <= 0){ pickups.splice(i,1); continue; }
    if (Math.hypot(pk.p[0]-player.p[0], pk.p[2]-player.p[2]) < 1.5 &&
        Math.abs(pk.p[1]-player.p[1]-.8) < 2.2){
      if (pk.kind === "hp"){
        player.hp = Math.min(player.maxHp, player.hp + 32);
        feed("<b>+32</b> integridade");
      } else {
        WEAPONS[0].res = Math.min(WEAPONS[0].maxRes, WEAPONS[0].res + 60);
        WEAPONS[1].res = Math.min(WEAPONS[1].maxRes, WEAPONS[1].res + 10);
        feed("<b>MUNIÇÃO</b> reabastecida");
      }
      SFX.pickup();
      burst(pk.p, 12, pk.kind === "hp" ? [.3,1,.62] : [1,.69,.23], 5, .4, 0);
      pickups.splice(i,1);
    }
  }
}

function updateEnemies(dt){
  const ppos = [ player.p[0], player.p[1]+1.0, player.p[2] ];
  for (let i = enemies.length-1; i>=0; i--){
    const e = enemies[i];
    if (e.dead){ enemies.splice(i,1); continue; }
    const T = ETYPE[e.type];
    e.birth = Math.min(1, e.birth + dt*1.8);
    e.flash = Math.max(0, e.flash - dt*4);
    e.spin += dt * (e.type === "runner" ? 7 : 2.2);
    e.bob  += dt * 2.4;
    e.hitT = Math.max(0, e.hitT - dt);

    const toP = [ ppos[0]-e.p[0], 0, ppos[2]-e.p[2] ];
    const dist = Math.hypot(toP[0], toP[2]) || .001;
    const dir = [ toP[0]/dist, 0, toP[2]/dist ];
    const side = [ -dir[2], 0, dir[0] ];

    let wish = [0,0,0];
    if (T.ranged){
      const want = 13;
      const f = dist > want + 3 ? 1 : dist < want - 4 ? -1 : 0;
      wish = [ dir[0]*f + side[0]*e.strafe*.85, 0, dir[2]*f + side[2]*e.strafe*.85 ];
      e.fireT -= dt;
      if (e.birth >= 1 && e.fireT <= 0 && dist < 34 && !blocked(e.p, ppos)){
        e.fireT = T.fire * rand(.8, 1.3);
        const bd = norm3(sub3([ppos[0], ppos[1]+rand(-.2,.2), ppos[2]], e.p));
        bolts.push({ p:[e.p[0],e.p[1],e.p[2]], v: mul3(bd, 17), life: 3.2, dmg: T.bolt, col: T.core });
        SFX.bolt();
      }
    } else {
      wish = [ dir[0], 0, dir[2] ];
      if (dist < e.r + .95){
        if (e.hitT <= 0){
          e.hitT = T.hitRate;
          hurtPlayer(T.melee, e.p);
          burst([ (e.p[0]+ppos[0])/2, ppos[1], (e.p[2]+ppos[2])/2 ], 10, T.col, 6, .3, 0);
        }
        wish = [0,0,0];
      }
    }
    if (Math.random() < dt * .5) e.strafe *= -1;

    e.vel[0] = lerp(e.vel[0], wish[0]*e.spd, dt*3.5);
    e.vel[2] = lerp(e.vel[2], wish[2]*e.spd, dt*3.5);
    e.p[0] += e.vel[0]*dt;
    e.p[2] += e.vel[2]*dt;
    e.p[1] = T.hover + Math.sin(e.bob)*.22;

    pushOut(e.p, e.r, e.p[1]-T.hover*.5, .1);
    e.p[0] = clamp(e.p[0], -ARENA+e.r, ARENA-e.r);
    e.p[2] = clamp(e.p[2], -ARENA+e.r, ARENA-e.r);
  }
}

function updateBolts(dt){
  const pc = [ player.p[0], player.p[1]+1.0, player.p[2] ];
  for (let i = bolts.length-1; i>=0; i--){
    const b = bolts[i];
    b.life -= dt;
    const np = add3(b.p, mul3(b.v, dt));
    let hit = false;
    if (len3(sub3(np, pc)) < .85){ hurtPlayer(b.dmg, b.p); hit = true; }
    if (!hit){
      for (const bx of boxes){
        if (np[0] > bx.min[0] && np[0] < bx.max[0] && np[1] > bx.min[1] &&
            np[1] < bx.max[1] && np[2] > bx.min[2] && np[2] < bx.max[2]){ hit = true; break; }
      }
    }
    b.p = np;
    if (hit || b.life <= 0 || Math.abs(b.p[0]) > ARENA+2 || Math.abs(b.p[2]) > ARENA+2 || b.p[1] < 0){
      burst(b.p, 7, b.col, 4, .25, 0);
      bolts.splice(i,1);
    }
  }
}

function updateFx(dt){
  for (let i = parts.length-1; i>=0; i--){
    const p = parts[i];
    p.life -= dt;
    if (p.life <= 0){ parts.splice(i,1); continue; }
    if (p.g) p.v[1] -= 16*dt;
    p.p[0] += p.v[0]*dt; p.p[1] += p.v[1]*dt; p.p[2] += p.v[2]*dt;
    if (p.p[1] < .06 && p.g){ p.p[1] = .06; p.v[1] *= -.34; p.v[0] *= .7; p.v[2] *= .7; }
  }
  for (let i = beams.length-1; i>=0; i--){
    beams[i].life -= dt;
    if (beams[i].life <= 0) beams.splice(i,1);
  }
}

function updateWaves(dt){
  if (G.queue.length === 0 && enemies.length === 0){
    if (G.betweenT > 0){
      const before = Math.ceil(G.betweenT);
      G.betweenT -= dt;
      const now = Math.ceil(G.betweenT);
      if (G.wave > 0 && now !== before && now > 0) banner("ONDA LIMPA", "PRÓXIMA EM " + now, 1200);
      if (G.betweenT <= 0){
        if (G.wave > 0){
          addScore(250 + G.wave*120);
          WEAPONS[0].res = Math.min(WEAPONS[0].maxRes, WEAPONS[0].res + 50);
          WEAPONS[1].res = Math.min(WEAPONS[1].maxRes, WEAPONS[1].res + 8);
          player.hp = Math.min(player.maxHp, player.hp + 14);
        }
        startWave(G.wave + 1);
      }
    }
  } else if (G.queue.length){
    for (let i = G.queue.length-1; i>=0; i--){
      G.queue[i].t -= dt;
      if (G.queue[i].t <= 0){
        spawnEnemy(G.queue[i].type, G.wave);
        G.queue.splice(i,1);
      }
    }
    if (G.queue.length === 0) G.betweenT = 4.0;
  }
}


export { updatePlayer, updateEnemies, updateBolts, updateFx, updateWaves };
