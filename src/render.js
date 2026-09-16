import { M4, norm3, lerp, rand, clamp, sub3, len3, TAU } from "./utils.js";
import { gl, U, MESH, W, H, FOG, draw, drawBox, resetDrawCache } from "./gl.js";
import { boxes } from "./world.js";
import { player, enemies, bolts, pickups, parts, beams, ETYPE, WEAPONS, burst } from "./entities.js";
import { G } from "./game-state.js";
/* =======================================================================
   10 · RENDER
   ======================================================================= */
const LIGHT = norm3([ .45, .82, .36 ]);
const VM_LIGHT = norm3([ .4, .55, .75 ]);

function render(){
  const aspect = H > 0 ? W / H : 1;
  const fov = (72 + (player.speed > 8 ? 4 : 0)) * Math.PI/180;
  const proj = M4.persp(fov, aspect, .05, 400);

  const bobY = Math.sin(player.bob) * .045;
  const bobX = Math.cos(player.bob*.5) * .035;
  const eye = [ player.p[0] + bobX*.25, player.p[1] + player.eye + bobY, player.p[2] ];
  const roll = Math.cos(player.bob*.5) * .006;
  const view = M4.mul(M4.mul(M4.mul(M4.rotZ(roll), M4.rotX(-player.pitch)), M4.rotY(-player.yaw)),
                      M4.trans(-eye[0], -eye[1], -eye[2]));

  gl.enable(gl.DEPTH_TEST);
  gl.enable(gl.CULL_FACE);
  gl.disable(gl.BLEND);
  gl.depthMask(true);
  gl.clearColor(FOG[0], FOG[1], FOG[2], 1);
  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

  gl.uniformMatrix4fv(U.uProj, false, proj);
  gl.uniformMatrix4fv(U.uView, false, view);
  gl.uniform3f(U.uCam, eye[0], eye[1], eye[2]);
  gl.uniform3f(U.uLight, LIGHT[0], LIGHT[1], LIGHT[2]);
  gl.uniform3f(U.uFog, FOG[0], FOG[1], FOG[2]);
  gl.uniform1f(U.uFogD, .0145);
  resetDrawCache();

  // piso
  draw(MESH.plane, M4.mul(M4.trans(0,0,0), M4.scale(420,1,420)), [.085,.11,.17], 0, 1, 1, true);

  // geometria da arena + faixa neon no topo
  for (const b of boxes){
    drawBox(b.x, b.y, b.z, b.sx, b.sy, b.sz, b.color, 0, 1);
    if (b.neon) drawBox(b.x, b.y + b.sy/2 + .045, b.z, b.sx*1.01, .09, b.sz*1.01, b.neon, 1, 1);
  }

  // coletáveis
  for (const pk of pickups){
    const col = pk.kind === "hp" ? [.25,1,.6] : [1,.69,.23];
    const y = pk.p[1] + Math.sin(pk.t*2.6)*.18;
    const blink = pk.life < 4 && Math.sin(pk.life*18) < 0;
    if (blink) continue;
    const m = M4.mul(M4.mul(M4.trans(pk.p[0], y, pk.p[2]), M4.rotY(pk.t*1.7)), M4.scale(.42,.42,.42));
    draw(MESH.box, m, col, .55, 1, 0, false);
    const m2 = M4.mul(M4.mul(M4.trans(pk.p[0], y, pk.p[2]), M4.rotY(-pk.t*2.3)), M4.scale(.62,.06,.62));
    draw(MESH.box, m2, col, 1, 1, 0, false);
  }

  // inimigos
  for (const e of enemies){
    const T = ETYPE[e.type];
    const s = .3 + .7 * Math.min(1, e.birth*1.4);
    let col = T.col;
    if (e.flash > 0) col = [ lerp(col[0],1,e.flash), lerp(col[1],1,e.flash), lerp(col[2],1,e.flash) ];
    const face = Math.atan2(e.vel[0], e.vel[2]);

    if (e.type === "drone"){
      const body = M4.mul(M4.mul(M4.mul(M4.trans(e.p[0],e.p[1],e.p[2]), M4.rotY(e.spin)),
                                 M4.rotZ(.22)), M4.scale(1.05*s, .5*s, 1.05*s));
      draw(MESH.box, body, col, e.flash*.8, 1, 0, false);
      const ring = M4.mul(M4.mul(M4.trans(e.p[0], e.p[1], e.p[2]), M4.rotY(-e.spin*1.7)),
                          M4.scale(1.7*s, .07*s, 1.7*s));
      draw(MESH.box, ring, T.core, 1, 1, 0, false);
    } else if (e.type === "runner"){
      const body = M4.mul(M4.mul(M4.mul(M4.trans(e.p[0],e.p[1],e.p[2]), M4.rotY(face)),
                                 M4.rotX(.3)), M4.scale(.52*s, .52*s, 1.2*s));
      draw(MESH.box, body, col, e.flash*.8, 1, 0, false);
      const fin = M4.mul(M4.mul(M4.trans(e.p[0], e.p[1]+.35*s, e.p[2]), M4.rotY(face)),
                         M4.scale(.12*s, .5*s, .7*s));
      draw(MESH.box, fin, T.core, .9, 1, 0, false);
      if (Math.random() < .5) burst([e.p[0], e.p[1]-.2, e.p[2]], 1, T.col, 1.2, .22, 0);
    } else {
      const body = M4.mul(M4.mul(M4.trans(e.p[0],e.p[1],e.p[2]), M4.rotY(e.spin*.5)),
                          M4.scale(1.9*s, 1.7*s, 1.9*s));
      draw(MESH.box, body, col, e.flash*.8, 1, 0, false);
      for (let k=0;k<4;k++){
        const a = e.spin + k*Math.PI/2;
        const m = M4.mul(M4.mul(M4.trans(e.p[0]+Math.cos(a)*1.25*s, e.p[1]+.9*s, e.p[2]+Math.sin(a)*1.25*s),
                                M4.rotY(a)), M4.scale(.3*s,.3*s,.3*s));
        draw(MESH.box, m, T.core, 1, 1, 0, false);
      }
    }
    // núcleo (ponto de dano dobrado)
    const pulse = 1 + Math.sin(G.time*7 + e.bob)*.07;
    const core = M4.mul(M4.trans(e.p[0], e.p[1], e.p[2]), M4.scale(T.r*.88*s*pulse, T.r*.88*s*pulse, T.r*.88*s*pulse));
    draw(MESH.sphere, core, T.core, 1, 1, 0, false);

    // barra de vida flutuante
    if (e.hp < e.maxHp){
      const wq = e.hp / e.maxHp;
      const yy = e.p[1] + T.r + .75;
      drawBox(e.p[0], yy, e.p[2], 1.5, .1, .04, [.05,.06,.09], 0, 1);
      drawBox(e.p[0] - (1.44*(1-wq))/2, yy, e.p[2], 1.44*wq, .07, .06,
              wq > .5 ? [.2,1,.65] : [1,.3,.35], 1, 1);
    }
  }

  // projéteis inimigos
  for (const b of bolts){
    draw(MESH.lowSph, M4.mul(M4.trans(b.p[0],b.p[1],b.p[2]), M4.scale(.34,.34,.34)), b.col, 1, 1, 0, true);
  }

  /* --- passes com transparência --- */
  gl.enable(gl.BLEND);
  gl.depthMask(false);

  // sombras de contato
  gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
  for (const e of enemies){
    const sc = ETYPE[e.type].r * 3.2 * clamp(1 - e.p[1]/9, .25, 1);
    draw(MESH.plane, M4.mul(M4.trans(e.p[0], .03, e.p[2]), M4.scale(sc,1,sc)), [0,0,0], 0, .32, 0, true);
  }
  for (const pk of pickups){
    draw(MESH.plane, M4.mul(M4.trans(pk.p[0], .03, pk.p[2]), M4.scale(1,1,1)), [0,0,0], 0, .25, 0, true);
  }

  // partículas e rastros (aditivo)
  gl.blendFunc(gl.SRC_ALPHA, gl.ONE);
  for (const p of parts){
    const a = clamp(p.life / p.max, 0, 1);
    const s = p.s * (0.4 + a);
    draw(MESH.box, M4.mul(M4.trans(p.p[0],p.p[1],p.p[2]), M4.scale(s,s,s)), p.col, 1, a, 0, true);
  }
  for (const b of beams){
    const d = sub3(b.b, b.a), L = len3(d);
    if (L < .01) continue;
    const mid = [ (b.a[0]+b.b[0])/2, (b.a[1]+b.b[1])/2, (b.a[2]+b.b[2])/2 ];
    const a = b.life / b.max;
    const m = M4.mul(M4.mul(M4.trans(mid[0],mid[1],mid[2]), M4.alignZ(d)), M4.scale(b.w, b.w, L));
    draw(MESH.box, m, b.col, 1, a, 0, true);
  }

  /* --- arma em primeira pessoa --- */
  gl.disable(gl.BLEND);
  gl.depthMask(true);
  gl.clear(gl.DEPTH_BUFFER_BIT);
  gl.uniformMatrix4fv(U.uProj, false, M4.persp(58*Math.PI/180, aspect, .01, 12));
  gl.uniformMatrix4fv(U.uView, false, M4.ident());
  gl.uniform3f(U.uCam, 0, 0, 0);
  gl.uniform3f(U.uLight, VM_LIGHT[0], VM_LIGHT[1], VM_LIGHT[2]);
  gl.uniform1f(U.uFogD, 0);
  resetDrawCache();
  drawViewModel();
  gl.uniform1f(U.uFogD, .0145);
}

function drawViewModel(){
  const w = WEAPONS[player.wep];
  const k = player.kick;
  const sway = [ lerp(0, -player.sway[0], 1), lerp(0, -player.sway[1], 1) ];
  const bx = Math.cos(player.bob) * .012;
  const by = Math.abs(Math.sin(player.bob)) * .014;
  const base = M4.mul(
    M4.mul(M4.trans(.30 + sway[0] + bx, -.30 + by - k*.012, -.62 + k*.035),
           M4.rotY(-.09 + sway[0]*.6)),
    M4.rotX(-.03 + k*.03 + sway[1]*.5)
  );
  const part = (x,y,z, sx,sy,sz, col, em) =>
    draw(MESH.box, M4.mul(M4.mul(base, M4.trans(x,y,z)), M4.scale(sx,sy,sz)), col, em||0, 1, 0, false);

  if (player.wep === 0){
    part(0, 0, 0,        .085,.115,.46,  [.13,.15,.21]);      // corpo
    part(0, .035, -.33,  .05,.05,.30,    [.09,.10,.15]);      // cano
    part(0, .085, .02,   .05,.045,.30,   [.18,.21,.29]);      // trilho
    part(0, .095, -.14,  .022,.03,.03,   w.col, 1);           // mira
    part(-.005,-.13,.10, .07,.16,.10,    [.11,.13,.18]);      // punho
    part(0,-.10,-.06,    .06,.15,.12,    [.16,.19,.26]);      // carregador
  } else {
    part(0, 0, .02,      .10,.13,.5,     [.19,.14,.10]);
    part(-.03,.03,-.36,  .055,.055,.34,  [.10,.11,.15]);
    part(.03,.03,-.36,   .055,.055,.34,  [.10,.11,.15]);
    part(0, .095, -.05,  .03,.03,.24,    w.col, 1);
    part(-.005,-.14,.14, .075,.17,.11,   [.14,.10,.07]);
    part(0,-.08,-.10,    .055,.05,.22,   [.22,.16,.10]);
  }

  // clarão do disparo
  if (player.fireT > w.rate - .045 && w.ammo >= 0 && player.reloadT === 0){
    const s = rand(.10, .2);
    const z = player.wep === 0 ? -.52 : -.55;
    draw(MESH.box, M4.mul(M4.mul(M4.mul(base, M4.trans(0, .035, z)), M4.rotZ(rand(0,TAU))),
                          M4.scale(s, s, s*1.5)), [1,.85,.45], 1, 1, 0, true);
  }
}


export { render };
