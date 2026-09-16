/* =======================================================================
   2 · ÁUDIO — tudo sintetizado, zero arquivos
   ======================================================================= */
let AC = null, master = null, muted = false, noiseBuf = null;
function audioInit(){
  if (AC){ if (AC.state === "suspended") AC.resume(); return; }
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return;
  AC = new Ctx();
  master = AC.createGain();
  master.gain.value = .5;
  master.connect(AC.destination);
  const n = AC.sampleRate * .5;
  noiseBuf = AC.createBuffer(1, n, AC.sampleRate);
  const d = noiseBuf.getChannelData(0);
  for (let i=0;i<n;i++) d[i] = Math.random()*2 - 1;
}
function tone(f0, f1, dur, type, vol, delay){
  if (!AC || muted) return;
  const t = AC.currentTime + (delay||0);
  const o = AC.createOscillator(), g = AC.createGain();
  o.type = type || "sine";
  o.frequency.setValueAtTime(f0, t);
  o.frequency.exponentialRampToValueAtTime(Math.max(f1,1), t + dur);
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + .006);
  g.gain.exponentialRampToValueAtTime(.0001, t + dur);
  o.connect(g); g.connect(master);
  o.start(t); o.stop(t + dur + .02);
}
function noise(dur, vol, f0, f1, q, delay){
  if (!AC || muted || !noiseBuf) return;
  const t = AC.currentTime + (delay||0);
  const s = AC.createBufferSource(); s.buffer = noiseBuf;
  const f = AC.createBiquadFilter(); f.type = "bandpass"; f.Q = q || 1;
  f.frequency.setValueAtTime(f0, t);
  f.frequency.exponentialRampToValueAtTime(Math.max(f1,20), t + dur);
  const g = AC.createGain();
  g.gain.setValueAtTime(vol, t);
  g.gain.exponentialRampToValueAtTime(.0001, t + dur);
  s.connect(f); f.connect(g); g.connect(master);
  s.start(t); s.stop(t + dur + .02);
}
const SFX = {
  rifle(){ noise(.09,.16,2600,700,1.4); tone(240,70,.09,"sawtooth",.1); },
  shotgun(){ noise(.26,.3,1700,180,.9); tone(150,40,.22,"square",.14); },
  dry(){ noise(.05,.1,4000,2500,6); },
  hit(){ tone(1500,900,.055,"square",.08); },
  crit(){ tone(2200,1300,.09,"square",.12); tone(1100,700,.09,"sine",.07,.02); },
  kill(){ noise(.3,.22,1400,120,.7); tone(420,60,.34,"sawtooth",.13); },
  hurt(){ tone(180,60,.24,"square",.2); noise(.2,.14,700,120,1); },
  pickup(){ tone(680,680,.08,"sine",.16); tone(1020,1020,.12,"sine",.14,.07); },
  reload(){ noise(.05,.16,1800,900,4); noise(.07,.18,1200,500,3,.22); },
  swap(){ noise(.04,.12,2200,1400,5); },
  wave(){ tone(330,330,.5,"sawtooth",.1); tone(440,440,.5,"sawtooth",.09,.08); tone(550,550,.6,"sawtooth",.09,.16); },
  spawn(){ tone(90,300,.3,"sine",.09); },
  dead(){ tone(300,40,1.2,"sawtooth",.2); noise(1.0,.18,900,60,.6); },
  bolt(){ tone(700,380,.16,"triangle",.07); }
};


/* Mudo é estado do módulo de áudio, não do input — quem aperta M só chama isto. */
export function toggleMute(){
  muted = !muted;
  if (master) master.gain.value = muted ? 0 : .5;
  return muted;
}

export { audioInit, SFX };
