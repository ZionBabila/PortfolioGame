"""Synthesizes the fortune machine's sound effects (no samples, no licences).
Run: python -I slot_sfx.py <out_dir>   (writes 16-bit mono 44.1 kHz WAVs)
"""
import sys, os, wave
import numpy as np

SR = 44100
rng = np.random.default_rng(26)

def t(sec): return np.arange(int(SR * sec)) / SR
def env(n, attack=0.002, decay=0.1):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    return a * np.exp(-x / decay)
def tone(freq, sec, decay, harmonics=((1, 1.0),), attack=0.002):
    x = t(sec); s = sum(a * np.sin(2 * np.pi * freq * h * x) for h, a in harmonics)
    return s * env(len(x), attack, decay)
def noise(sec, decay, attack=0.0005, hp=True):
    n = rng.uniform(-1, 1, int(SR * sec))
    if hp: n = np.diff(n, prepend=0)          # brighter, more "metal"
    return n * env(len(n), attack, decay)
def mix(*xs):
    out = np.zeros(max(len(x) for x in xs))
    for x in xs: out[:len(x)] += x
    return out
def place(dst, src, at):
    i = int(at * SR); dst[i:i + len(src)] += src[:len(dst) - i]
def write(path, x, gain=0.8):
    x = x / (np.max(np.abs(x)) + 1e-9) * gain
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())

def tick():   # a ratchet tooth passing the pawl
    x = mix(noise(0.03, 0.004) * 0.6, tone(2400, 0.03, 0.006) * 0.5)
    return x
def pull():   # lever: a run of ratchet clicks speeding up, then the mechanism lands
    x = np.zeros(int(SR * 0.55))
    at = 0.0
    for i in range(7):
        place(x, tick() * (0.6 + 0.06 * i), at); at += 0.06 - i * 0.006
    place(x, mix(tone(70, 0.3, 0.09, ((1, 1.0), (2, 0.4))), noise(0.08, 0.02, hp=False) * 0.6), at + 0.02)
    return x
def stop():   # reel catches: dull clunk
    return mix(tone(95, 0.25, 0.07, ((1, 1.0), (2.1, 0.35), (3.3, 0.12))), noise(0.05, 0.008) * 0.5)
def win():    # bell arpeggio + coin shimmer
    x = np.zeros(int(SR * 1.6))
    bell = ((1, 1.0), (2.76, 0.35), (5.4, 0.12))
    for i, f in enumerate((1046.5, 1318.5, 1568.0, 2093.0)):
        place(x, tone(f, 1.0, 0.35, bell), i * 0.11)
    for i in range(14):
        place(x, tone(3000 + rng.uniform(0, 2500), 0.12, 0.03) * 0.25, 0.45 + i * 0.05)
    return x
def empty():  # nothing left: a dull buzz
    x = t(0.32); sq = np.sign(np.sin(2 * np.pi * 110 * x)) * 0.5 + np.sin(2 * np.pi * 55 * x)
    return sq * env(len(x), 0.01, 0.12)
def catch():  # caught Zion: two bright notes up + sparkle
    x = np.zeros(int(SR * 0.7))
    place(x, tone(784, 0.4, 0.12, ((1, 1.0), (2, 0.3))), 0.0)
    place(x, tone(1175, 0.5, 0.18, ((1, 1.0), (2, 0.3))), 0.09)
    for i in range(6): place(x, tone(2600 + i * 300, 0.1, 0.03) * 0.3, 0.18 + i * 0.04)
    return x

if __name__ == "__main__":
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    for name, fn, gain in [("Slot_Pull", pull, 0.8), ("Slot_Tick", tick, 0.6), ("Slot_Stop", stop, 0.85),
                           ("Slot_Win", win, 0.8), ("Slot_Empty", empty, 0.6), ("Slot_Catch", catch, 0.75)]:
        write(os.path.join(out, name + ".wav"), fn(), gain); print(name)
