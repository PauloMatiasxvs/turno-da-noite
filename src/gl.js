/* =======================================================================
   1 · WEBGL — contexto, programa de shaders, malhas e desenho
   -----------------------------------------------------------------------
   Nada aqui depende do jogo: é a camada de renderização crua.
   ======================================================================= */
import { $, TAU, M4, IDENT3, normalMat3 } from "./utils.js";

export let canvas = null;   // <canvas> do jogo
export let gl     = null;   // contexto WebGL2
export let prog   = null;   // programa de shaders único
export const U    = {};     // localizações dos uniforms
export const MESH = {};     // malhas estáticas (cubo, esfera, plano)
export let W = 0, H = 0;    // tamanho do framebuffer em pixels reais

export const FOG = [0.035, 0.05, 0.09];

/* Sobe o contexto e tudo que depende dele.
   Retorna false quando o navegador não tem WebGL2 — quem chama decide o aviso. */
export function initGL(){
  canvas = document.getElementById("gl");
  gl = canvas.getContext("webgl2", { antialias:true, powerPreference:"high-performance" });
  if (!gl) return false;
  buildProgram();
  MESH.box    = boxGeo();
  MESH.sphere = sphereGeo(18, 12);
  MESH.lowSph = sphereGeo(8, 6);
  MESH.plane  = planeGeo();
  addEventListener("resize", resize);
  resize();
  return true;
}

const VS = `#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aNor;
uniform mat4 uProj, uView, uModel;
uniform mat3 uNorMat;
out vec3 vN;
out vec3 vW;
void main(){
  vec4 w = uModel * vec4(aPos, 1.0);
  vW = w.xyz;
  vN = uNorMat * aNor;
  gl_Position = uProj * uView * w;
}`;

const FS = `#version 300 es
precision highp float;
in vec3 vN;
in vec3 vW;
uniform vec3  uColor, uLight, uCam, uFog;
uniform float uEmissive, uAlpha, uGrid, uFogD;
out vec4 outColor;

void main(){
  vec3 N = normalize(vN);
  vec3 base = uColor;

  if (uGrid > 0.5){                                   // piso: grade neon procedural
    vec2 c1 = vW.xz * 0.25;
    vec2 g1 = abs(fract(c1 - 0.5) - 0.5) / fwidth(c1);
    float l1 = 1.0 - min(min(g1.x, g1.y), 1.0);
    vec2 c2 = vW.xz * 0.05;
    vec2 g2 = abs(fract(c2 - 0.5) - 0.5) / fwidth(c2);
    float l2 = 1.0 - min(min(g2.x, g2.y), 1.0);
    base = mix(base, vec3(0.05, 0.42, 0.58), l1 * 0.85);
    base = mix(base, vec3(0.25, 0.85, 1.00), l2 * 0.90);
  }

  float dif  = max(dot(N, uLight), 0.0);
  vec3  V    = normalize(uCam - vW);
  vec3  H    = normalize(uLight + V);
  float spec = pow(max(dot(N, H), 0.0), 26.0) * 0.30;
  float sky  = N.y * 0.5 + 0.5;
  vec3  amb  = mix(vec3(0.17,0.19,0.28), vec3(0.36,0.43,0.60), sky);

  // luz de preenchimento fria vinda do lado oposto: dá volume sem clarear demais
  float fill = max(dot(N, normalize(vec3(-uLight.x, 0.3, -uLight.z))), 0.0) * 0.40;
  // brilho de borda (fresnel) — é o que dá o contorno neon na geometria
  float rim  = pow(1.0 - max(dot(N, V), 0.0), 3.0) * 0.40;

  vec3 col = base * (amb + dif * vec3(1.0,0.94,0.84) * 1.05 + fill * vec3(0.32,0.55,1.0))
           + vec3(spec) + rim * vec3(0.16,0.48,0.72) * (1.0 - uEmissive);
  col = mix(col, base * 1.7 + 0.15, uEmissive);

  float d = length(uCam - vW);
  float f = 1.0 - exp(-pow(max(d - 6.0, 0.0) * uFogD, 2.0));
  col = mix(col, uFog, clamp(f, 0.0, 0.88));

  outColor = vec4(col, uAlpha);
}`;

function compile(type, src){
  const s = gl.createShader(type);
  gl.shaderSource(s, src);
  gl.compileShader(s);
  if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s));
  return s;
}

function buildProgram(){
  prog = gl.createProgram();
  gl.attachShader(prog, compile(gl.VERTEX_SHADER, VS));
  gl.attachShader(prog, compile(gl.FRAGMENT_SHADER, FS));
  gl.linkProgram(prog);
  if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(prog));
  gl.useProgram(prog);
  ["uProj","uView","uModel","uNorMat","uColor","uLight","uCam","uFog","uEmissive","uAlpha","uGrid","uFogD"]
    .forEach(n => U[n] = gl.getUniformLocation(prog, n));
}
function makeMesh(pos, nor, idx){
  const vao = gl.createVertexArray();
  gl.bindVertexArray(vao);
  const pb = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, pb);
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(pos), gl.STATIC_DRAW);
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 0, 0);
  const nb = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, nb);
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(nor), gl.STATIC_DRAW);
  gl.enableVertexAttribArray(1);
  gl.vertexAttribPointer(1, 3, gl.FLOAT, false, 0, 0);
  const ib = gl.createBuffer();
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, ib);
  gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(idx), gl.STATIC_DRAW);
  gl.bindVertexArray(null);
  return { vao, count: idx.length };
}

function boxGeo(){
  const p=[], n=[], i=[];
  const F = [
    { n:[0,0,1],  v:[[-.5,-.5,.5],[.5,-.5,.5],[.5,.5,.5],[-.5,.5,.5]] },
    { n:[0,0,-1], v:[[.5,-.5,-.5],[-.5,-.5,-.5],[-.5,.5,-.5],[.5,.5,-.5]] },
    { n:[1,0,0],  v:[[.5,-.5,.5],[.5,-.5,-.5],[.5,.5,-.5],[.5,.5,.5]] },
    { n:[-1,0,0], v:[[-.5,-.5,-.5],[-.5,-.5,.5],[-.5,.5,.5],[-.5,.5,-.5]] },
    { n:[0,1,0],  v:[[-.5,.5,.5],[.5,.5,.5],[.5,.5,-.5],[-.5,.5,-.5]] },
    { n:[0,-1,0], v:[[-.5,-.5,-.5],[.5,-.5,-.5],[.5,-.5,.5],[-.5,-.5,.5]] }
  ];
  F.forEach((f,k) => {
    f.v.forEach(v => { p.push(v[0],v[1],v[2]); n.push(f.n[0],f.n[1],f.n[2]); });
    const b = k*4;
    i.push(b,b+1,b+2, b,b+2,b+3);
  });
  return makeMesh(p,n,i);
}

function sphereGeo(sg, rg){
  const p=[], n=[], i=[];
  for (let y=0; y<=rg; y++){
    const phi = (y/rg) * Math.PI;
    for (let x=0; x<=sg; x++){
      const th = (x/sg) * TAU;
      const nx = Math.sin(phi)*Math.cos(th), ny = Math.cos(phi), nz = Math.sin(phi)*Math.sin(th);
      n.push(nx,ny,nz);
      p.push(nx*.5, ny*.5, nz*.5);
    }
  }
  for (let y=0; y<rg; y++) for (let x=0; x<sg; x++){
    const a = y*(sg+1)+x, b = a+sg+1;
    i.push(a, a+1, b, a+1, b+1, b);
  }
  return makeMesh(p,n,i);
}

function planeGeo(){
  return makeMesh(
    [-.5,0,.5, .5,0,.5, .5,0,-.5, -.5,0,-.5],
    [0,1,0, 0,1,0, 0,1,0, 0,1,0],
    [0,1,2, 0,2,3]
  );
}

/* --- estado de desenho --- */
let emis = -1, alph = -1, grid = -1;
function draw(mesh, model, color, emissive, alpha, isGrid, fastNormals){
  gl.uniformMatrix4fv(U.uModel, false, model);
  gl.uniformMatrix3fv(U.uNorMat, false, fastNormals ? IDENT3 : normalMat3(model));
  gl.uniform3f(U.uColor, color[0], color[1], color[2]);
  const e = emissive || 0, a = alpha === undefined ? 1 : alpha, g = isGrid ? 1 : 0;
  if (e !== emis){ gl.uniform1f(U.uEmissive, e); emis = e; }
  if (a !== alph){ gl.uniform1f(U.uAlpha, a);   alph = a; }
  if (g !== grid){ gl.uniform1f(U.uGrid, g);    grid = g; }
  gl.bindVertexArray(mesh.vao);
  gl.drawElements(gl.TRIANGLES, mesh.count, gl.UNSIGNED_SHORT, 0);
}
// atalho: caixa posicionada por centro + tamanho
function drawBox(x,y,z, sx,sy,sz, color, emissive, alpha){
  draw(MESH.box, M4.mul(M4.trans(x,y,z), M4.scale(sx,sy,sz)), color, emissive, alpha, 0, true);
}

function resize(){
  const dpr = Math.min(devicePixelRatio || 1, 2);
  W = Math.floor(innerWidth * dpr);
  H = Math.floor(innerHeight * dpr);
  if (canvas.width !== W || canvas.height !== H){
    canvas.width = W; canvas.height = H;
    gl.viewport(0, 0, W, H);
  }
}

/* O draw() só manda um uniform para a GPU quando o valor muda; o render zera
   esse cache no começo de cada quadro. */
export function resetDrawCache(){ emis = alph = grid = -1; }

export { draw, drawBox, resize };
