import { clamp, sub3, mul3, len3 } from "./utils.js";
/* =======================================================================
   3 · MUNDO — arena, paredes, coberturas
   ======================================================================= */
const ARENA = 30;                       // meia-largura da arena
const boxes = [];
function addBox(x,y,z, sx,sy,sz, color, neon){
  boxes.push({
    x,y,z, sx,sy,sz, color, neon,
    min:[x-sx/2, y-sy/2, z-sz/2],
    max:[x+sx/2, y+sy/2, z+sz/2]
  });
}
const WALL  = [0.17, 0.21, 0.31];
const CRATE = [0.26, 0.30, 0.40];
const PILL  = [0.21, 0.25, 0.36];

function buildArena(){
  boxes.length = 0;
  const h = 9, t = 2;
  addBox(0, h/2, -ARENA-t/2, ARENA*2+t*2, h, t, WALL, [0.2,.85,1]);
  addBox(0, h/2,  ARENA+t/2, ARENA*2+t*2, h, t, WALL, [0.2,.85,1]);
  addBox(-ARENA-t/2, h/2, 0, t, h, ARENA*2+t*2, WALL, [0.2,.85,1]);
  addBox( ARENA+t/2, h/2, 0, t, h, ARENA*2+t*2, WALL, [0.2,.85,1]);

  // pilares
  [[-16,-16],[16,-16],[-16,16],[16,16]].forEach(p =>
    addBox(p[0], 4, p[1], 3.4, 8, 3.4, PILL, [1,.18,.53]));
  [[0,-22],[0,22],[-22,0],[22,0]].forEach(p =>
    addBox(p[0], 3, p[1], 2.2, 6, 2.2, PILL, [1,.69,.23]));

  // coberturas baixas
  addBox(0, 1.4, -9, 13, 2.8, 1.6, CRATE, [.2,.85,1]);
  addBox(0, 1.4,  9, 13, 2.8, 1.6, CRATE, [.2,.85,1]);
  addBox(-9, 1.4, 0, 1.6, 2.8, 13, CRATE, [.2,.85,1]);
  addBox( 9, 1.4, 0, 1.6, 2.8, 13, CRATE, [.2,.85,1]);

  // engradados (dá para subir)
  const crates = [[-24,-6],[-21,-9],[24,7],[21,10],[-6,24],[7,-24],[13,-14],[-13,14],[25,-20],[-25,20]];
  crates.forEach((c,k) => {
    addBox(c[0], 1.1, c[1], 2.2, 2.2, 2.2, CRATE, [1,.69,.23]);
    if (k % 3 === 0) addBox(c[0]+.3, 3.2, c[1]+.3, 1.8, 1.8, 1.8, CRATE, [1,.18,.53]);
  });

  // plataforma central
  addBox(0, .55, 0, 8, 1.1, 8, PILL, [.2,.85,1]);
}
buildArena();

function insideAnyBox(x, z, pad){
  for (const b of boxes){
    if (x > b.min[0]-pad && x < b.max[0]+pad && z > b.min[2]-pad && z < b.max[2]+pad) return true;
  }
  return false;
}

/* --- interseções --- */
function rayAABB(o, d, mn, mx){
  let t1 = -Infinity, t2 = Infinity;
  for (let i=0;i<3;i++){
    if (Math.abs(d[i]) < 1e-8){ if (o[i] < mn[i] || o[i] > mx[i]) return Infinity; }
    else {
      let a = (mn[i]-o[i]) / d[i], b = (mx[i]-o[i]) / d[i];
      if (a > b){ const s = a; a = b; b = s; }
      if (a > t1) t1 = a;
      if (b < t2) t2 = b;
    }
  }
  if (t2 < Math.max(t1, 0)) return Infinity;
  return t1 > 0 ? t1 : (t2 > 0 ? 0 : Infinity);
}
function raySphere(o, d, c, r){
  const ox = o[0]-c[0], oy = o[1]-c[1], oz = o[2]-c[2];
  const b = ox*d[0] + oy*d[1] + oz*d[2];
  const cc = ox*ox + oy*oy + oz*oz - r*r;
  const disc = b*b - cc;
  if (disc < 0) return Infinity;
  const s = Math.sqrt(disc);
  let t = -b - s;
  if (t < 0) t = -b + s;
  return t < 0 ? Infinity : t;
}
function blocked(a, b){                        // linha de visão obstruída?
  const d = sub3(b, a), dist = len3(d);
  if (dist < .001) return false;
  const dir = mul3(d, 1/dist);
  for (const bx of boxes) if (rayAABB(a, dir, bx.min, bx.max) < dist) return true;
  return false;
}
// empurra um círculo (x,z) para fora das caixas sólidas na altura feetY
function pushOut(p, r, feetY, headH){
  for (const b of boxes){
    if (b.max[1] <= feetY + .4) continue;
    if (b.min[1] >= feetY + headH) continue;
    const cx = clamp(p[0], b.min[0], b.max[0]);
    const cz = clamp(p[2], b.min[2], b.max[2]);
    const dx = p[0]-cx, dz = p[2]-cz;
    const d2 = dx*dx + dz*dz;
    if (d2 >= r*r) continue;
    const d = Math.sqrt(d2);
    if (d < 1e-5){
      const px = Math.min(p[0]-b.min[0], b.max[0]-p[0]);
      const pz = Math.min(p[2]-b.min[2], b.max[2]-p[2]);
      if (px < pz) p[0] += (p[0] < (b.min[0]+b.max[0])/2 ? -(px+r) : (px+r));
      else         p[2] += (p[2] < (b.min[2]+b.max[2])/2 ? -(pz+r) : (pz+r));
    } else {
      const s = (r-d)/d;
      p[0] += dx*s; p[2] += dz*s;
    }
  }
}
function groundAt(p, r){
  let g = 0;
  for (const b of boxes){
    if (b.max[1] > p[1] + .4 || b.max[1] <= g) continue;
    const cx = clamp(p[0], b.min[0], b.max[0]);
    const cz = clamp(p[2], b.min[2], b.max[2]);
    const dx = p[0]-cx, dz = p[2]-cz;
    if (dx*dx + dz*dz < r*r) g = b.max[1];
  }
  return g;
}


export { ARENA, boxes, addBox, buildArena, insideAnyBox, rayAABB, raySphere, blocked, pushOut, groundAt };
