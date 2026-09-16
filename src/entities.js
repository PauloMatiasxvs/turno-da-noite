import { rand, norm3, TAU } from "./utils.js";
import { SFX } from "./audio.js";
import { ARENA, insideAnyBox } from "./world.js";
/* =======================================================================
   4 · ENTIDADES
   ======================================================================= */
const WEAPONS = [
  { name:"Rifle",   dmg:15, rate:.105, mag:32, ammo:32, res:160, maxRes:220, reload:1.4,
    spread:.014, pellets:1, auto:true,  range:120, kick:.9,  sfx:"rifle",   col:[.2,.85,1] },
  { name:"Shotgun", dmg:11, rate:.72,  mag:6,  ammo:6,  res:36,  maxRes:60,  reload:2.1,
    spread:.075, pellets:9, auto:false, range:45,  kick:3.4, sfx:"shotgun", col:[1,.69,.23] }
];

const player = {
  p:[0,0,20], vy:0, yaw:0, pitch:0,
  hp:100, maxHp:100, r:.42, eye:1.68, onGround:true,
  wep:0, fireT:0, reloadT:0, bob:0, kick:0, sway:[0,0], hurtT:0, hurtDir:0, speed:0
};

const ETYPE = {
  drone: { hp:38,  spd:4.4, r:.72, col:[.18,.86,1],  core:[.55,1,1],   score:100, hover:1.7, ranged:true,  fire:2.2,  bolt:8  },
  runner:{ hp:22,  spd:9.2, r:.55, col:[1,.18,.53],  core:[1,.7,.9],   score:140, hover:1.15, melee:14,    hitRate:.8 },
  brute: { hp:180, spd:2.9, r:1.25,col:[1,.55,.14],  core:[1,.9,.5],   score:340, hover:1.9, melee:26,     hitRate:1.2 }
};

let enemies = [], bolts = [], pickups = [], parts = [], beams = [];

function spawnEnemy(type, wave){
  const T = ETYPE[type];
  let x, z, tries = 0;
  do {
    const a = rand(0, TAU), d = rand(18, ARENA-4);
    x = Math.cos(a)*d; z = Math.sin(a)*d;
    tries++;
  } while (tries < 40 && (insideAnyBox(x, z, 1.4) || Math.hypot(x-player.p[0], z-player.p[2]) < 14));

  const scale = 1 + (wave-1) * .11;
  enemies.push({
    type, p:[x, T.hover, z], vel:[0,0,0],
    hp: T.hp*scale, maxHp: T.hp*scale, r: T.r,
    spd: T.spd * Math.min(1 + (wave-1)*.025, 1.5),
    spin: rand(0,TAU), strafe: Math.random() < .5 ? 1 : -1,
    fireT: rand(.6, 2.2), hitT: 0, flash: 0, birth: 0, dead: false, bob: rand(0,TAU)
  });
  SFX.spawn();
  burst([x, T.hover, z], 14, T.col, 7, .5, 0);
}

function burst(p, n, col, spd, life, grav){
  for (let i=0;i<n;i++){
    if (parts.length > 320) break;
    const d = norm3([rand(-1,1), rand(-1,1), rand(-1,1)]);
    parts.push({
      p:[p[0],p[1],p[2]],
      v:[d[0]*rand(.3,1)*spd, d[1]*rand(.3,1)*spd + (grav?2:0), d[2]*rand(.3,1)*spd],
      life: life*rand(.6,1.2), max: life, s: rand(.05,.16), col, g: grav
    });
  }
}
function beam(a, b, col, w){ beams.push({ a, b, col, w: w||.02, life: .055, max: .055 }); }

function dropPickup(p){
  const r = Math.random();
  let kind = null;
  if (player.hp < 55 && r < .42) kind = "hp";
  else if (r < .16) kind = "hp";
  else if (r < .48) kind = "ammo";
  if (!kind) return;
  pickups.push({ p:[p[0], .8, p[2]], kind, t: rand(0,TAU), life: 22 });
}


export { WEAPONS, ETYPE, player, enemies, bolts, pickups, parts, beams, spawnEnemy, burst, beam, dropPickup };
