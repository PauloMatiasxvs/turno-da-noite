/* =======================================================================
   0 · UTILITÁRIOS
   ======================================================================= */
const $  = s => document.querySelector(s);
const TAU = Math.PI * 2;
const clamp = (v,a,b) => v < a ? a : v > b ? b : v;
const lerp  = (a,b,t) => a + (b - a) * t;
const rand  = (a,b) => a + Math.random() * (b - a);
const randi = (a,b) => Math.floor(rand(a,b+1));
const pick  = a => a[Math.floor(Math.random()*a.length)];

const sub3 = (a,b) => [a[0]-b[0], a[1]-b[1], a[2]-b[2]];
const add3 = (a,b) => [a[0]+b[0], a[1]+b[1], a[2]+b[2]];
const mul3 = (a,s) => [a[0]*s, a[1]*s, a[2]*s];
const dot3 = (a,b) => a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
const len3 = a => Math.hypot(a[0],a[1],a[2]);
const cross3 = (a,b) => [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]];
function norm3(a){ const l = len3(a) || 1; return [a[0]/l, a[1]/l, a[2]/l]; }

/* --- matrizes 4x4, column-major (formato que o OpenGL espera) --- */
const M4 = {
  ident(){ const o = new Float32Array(16); o[0]=o[5]=o[10]=o[15]=1; return o; },
  mul(a,b){                                  // a * b
    const o = new Float32Array(16);
    for (let c=0;c<4;c++) for (let r=0;r<4;r++){
      let s = 0;
      for (let k=0;k<4;k++) s += a[k*4+r] * b[c*4+k];
      o[c*4+r] = s;
    }
    return o;
  },
  persp(fovy, asp, n, f){
    const t = 1 / Math.tan(fovy/2), o = new Float32Array(16);
    o[0] = t/asp; o[5] = t; o[10] = (f+n)/(n-f); o[11] = -1; o[14] = 2*f*n/(n-f);
    return o;
  },
  trans(x,y,z){ const o = M4.ident(); o[12]=x; o[13]=y; o[14]=z; return o; },
  scale(x,y,z){ const o = M4.ident(); o[0]=x; o[5]=y; o[10]=z; return o; },
  rotX(a){ const c=Math.cos(a), s=Math.sin(a), o=M4.ident(); o[5]=c;o[6]=s;o[9]=-s;o[10]=c; return o; },
  rotY(a){ const c=Math.cos(a), s=Math.sin(a), o=M4.ident(); o[0]=c;o[2]=-s;o[8]=s;o[10]=c; return o; },
  rotZ(a){ const c=Math.cos(a), s=Math.sin(a), o=M4.ident(); o[0]=c;o[1]=s;o[4]=-s;o[5]=c; return o; },
  // rotação que alinha o eixo +Z ao vetor dir (usado nos rastros de tiro)
  alignZ(dir){
    const z = norm3(dir);
    const up = Math.abs(z[1]) > .995 ? [1,0,0] : [0,1,0];
    const x = norm3(cross3(up, z));
    const y = cross3(z, x);
    const o = M4.ident();
    o[0]=x[0];o[1]=x[1];o[2]=x[2];
    o[4]=y[0];o[5]=y[1];o[6]=y[2];
    o[8]=z[0];o[9]=z[1];o[10]=z[2];
    return o;
  }
};
const IDENT3 = new Float32Array([1,0,0, 0,1,0, 0,0,1]);
// inversa transposta do bloco 3x3 — corrige normais sob escala não uniforme
function normalMat3(m){
  const a00=m[0],a01=m[1],a02=m[2], a10=m[4],a11=m[5],a12=m[6], a20=m[8],a21=m[9],a22=m[10];
  const b01 = a22*a11 - a12*a21, b11 = -a22*a10 + a12*a20, b21 = a21*a10 - a11*a20;
  let det = a00*b01 + a01*b11 + a02*b21;
  if (!det) return IDENT3;
  det = 1/det;
  const o = new Float32Array(9);
  o[0]=b01*det;                    o[1]=(-a22*a01+a02*a21)*det;  o[2]=(a12*a01-a02*a11)*det;
  o[3]=b11*det;                    o[4]=(a22*a00-a02*a20)*det;   o[5]=(-a12*a00+a02*a10)*det;
  o[6]=b21*det;                    o[7]=(-a21*a00+a01*a20)*det;  o[8]=(a11*a00-a01*a10)*det;
  return o;
}


export {
  $, TAU, clamp, lerp, rand, randi, pick,
  sub3, add3, mul3, dot3, len3, cross3, norm3,
  M4, IDENT3, normalMat3
};
