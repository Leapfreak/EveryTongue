/* mic-worklet.js — AudioWorkletProcessor for the web-mic broadcast.
   Downsamples the browser's native capture rate (44.1/48kHz) to 16kHz mono
   int16 and posts 1600-sample (100ms) frames — the exact frame the live-server
   engine queue expects, so the server forwards bytes untouched.
   NOTE: this file runs in AudioWorkletGlobalScope (modern JS is fine here);
   the ES5 rule applies to app.js, not worklet modules. */

class MicDownsampler extends AudioWorkletProcessor {
  constructor() {
    super();
    this._outRate = 16000;
    this._frameOut = 1600;                    /* 100ms at 16kHz */
    this._buf = new Float32Array(0);
    /* For 48000 (ratio 3) and 44100 (ratio 2.75625) one output frame consumes
       an INTEGER number of input samples (4800 / 4410), so there is no
       cumulative drift from the per-frame floor(). */
    this._ratio = sampleRate / this._outRate;
  }

  process(inputs) {
    const ch = inputs[0] && inputs[0][0];
    if (!ch || ch.length === 0) return true;

    const merged = new Float32Array(this._buf.length + ch.length);
    merged.set(this._buf);
    merged.set(ch, this._buf.length);
    this._buf = merged;

    const need = this._frameOut * this._ratio;
    while (this._buf.length >= need + 1) {
      const out = new Int16Array(this._frameOut);
      for (let i = 0; i < this._frameOut; i++) {
        const p = i * this._ratio;
        const i0 = Math.floor(p);
        const frac = p - i0;
        let s = this._buf[i0] * (1 - frac) + this._buf[i0 + 1] * frac;
        if (s > 1) s = 1; else if (s < -1) s = -1;
        out[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
      }
      this._buf = this._buf.slice(Math.floor(need));
      this.port.postMessage(out.buffer, [out.buffer]);
    }
    return true;
  }
}

registerProcessor('mic-downsampler', MicDownsampler);
