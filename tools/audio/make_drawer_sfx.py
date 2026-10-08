"""Procedural placeholder sounds for the heavy brass stamp drawer (Track BR).

Saleh (2026-10-08): "a heavy brass drawer that makes the sound a typewriter
makes when the carriage returns, that heavy metal carry". He sources the real
sounds himself (SOUND_LIST.md: drawer_open, drawer_close, "typewriter carriage
return, heavy brass"); until then these play.

    python tools/audio/make_drawer_sfx.py <repo root> [<waveform png folder>]

Writes Assets/Art/Office/Sounds/drawer_open.wav and drawer_close.wav (48 kHz,
16-bit mono, peak about -3 dBFS, starting within 1 ms) and, with a folder,
their waveform PNGs.

The timing follows the drawer's default knobs (MotionKnobs; keep in step):
- drawer_open plays as the carry sets off. Ratchet clicks come one per tooth of
  the travel, so they speed up with the carry: tooth k of N clicks at
  T * (k/N) ** (1/power), with T = drawerCarrySeconds (0.42) and power =
  drawerCarryPower (2.4). They ride over a filtered metallic rasp that swells
  with the speed. The heavy clunk lands at T, the hard stop. Then the
  mechanism's finer ratchet rasps on through the cradles' raise, from
  drawerRaiseFrom * T to about T + 0.3 s.
- drawer_close plays as the shove sets off (the cradles have folded). A shorter
  reverse rasp follows, its clicks one per tooth along DrawerSequence.ShoveCurve
  (u + kick*u*(1-u), kick = drawerShoveKick 0.7) over drawerShoveSeconds
  (0.28), so they slow down. A deep thud lands as it seats.
"""
import os
import sys
import wave

import numpy as np

RATE = 48000
CARRY_SECONDS, CARRY_POWER, RAISE_FROM, RAISE_STAGGER = 0.42, 2.4, 0.8, 0.14
SHOVE_SECONDS, SHOVE_KICK = 0.28, 0.7
rng = np.random.default_rng(2150)


def band(x, lo, hi):
    """Band-limits x to lo..hi Hz (FFT mask with soft edges)."""
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1.0 / RATE)
    mask = 1.0 / (1.0 + (lo / np.maximum(f, 1.0)) ** 4) / (1.0 + (f / hi) ** 4)
    return np.fft.irfft(spec * mask, len(x))


def env(n, attack, decay):
    t = np.arange(n) / RATE
    a = np.clip(t / max(attack, 1e-4), 0.0, 1.0)
    return a * np.exp(-t / decay)


def place(buf, at, sound, gain=1.0):
    i = int(at * RATE)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(sound))
    buf[i:j] += gain * sound[: j - i]


def tooth(pitch=1.0, weight=1.0):
    """One ratchet tooth: a sharp metal tick with a short brassy ring (pitch scales the ring)."""
    n = int(0.045 * RATE)
    t = np.arange(n) / RATE
    tick = band(rng.standard_normal(n), 2200 * pitch, 7500) * env(n, 0.0004, 0.004)
    ring = (np.sin(2 * np.pi * 2350 * pitch * t) + 0.6 * np.sin(2 * np.pi * 3720 * pitch * t + 1.0)) * env(n, 0.0005, 0.014)
    body = np.sin(2 * np.pi * 620 * pitch * t) * env(n, 0.0008, 0.01) * weight
    s = 0.9 * tick / (np.abs(tick).max() + 1e-9) + 0.35 * ring + 0.3 * body
    return s


def clunk(low=72.0, length=0.42, metal=1.0):
    """The heavy stop: a low body thump, a dull metal impact and a little brass ring."""
    n = int(length * RATE)
    t = np.arange(n) / RATE
    thump = (np.sin(2 * np.pi * low * t) + 0.6 * np.sin(2 * np.pi * low * 1.55 * t + 0.4)) * env(n, 0.002, length * 0.35)
    knock = band(rng.standard_normal(n), 120, 1800) * env(n, 0.0005, 0.03)
    knock /= np.abs(knock).max() + 1e-9
    ring = (np.sin(2 * np.pi * 880 * t) + 0.5 * np.sin(2 * np.pi * 1410 * t)) * env(n, 0.001, 0.16) * metal
    return 1.0 * thump + 0.7 * knock + 0.12 * ring


def rasp(n, lo=1400, hi=5200):
    r = band(rng.standard_normal(n), lo, hi)
    return r / (np.abs(r).max() + 1e-9)


def drawer_open():
    total = CARRY_SECONDS + 0.62
    buf = np.zeros(int(total * RATE))
    teeth = 24
    # The catch letting go as the heave starts (the sound begins at once), then a tooth per notch of the travel.
    place(buf, 0.0, tooth(0.85, 1.5), 0.5)
    place(buf, 0.0, clunk(130.0, 0.12, 0.2), 0.25)
    for k in range(1, teeth + 1):
        at = CARRY_SECONDS * (k / teeth) ** (1.0 / CARRY_POWER)
        speed = (k / teeth) ** ((CARRY_POWER - 1) / CARRY_POWER)  # the carry's speed there, 0..1
        place(buf, at - 0.004, tooth(1.0 + 0.06 * speed, 1.0), 0.32 + 0.4 * speed)
    # The rasp under the carry swells with its speed and cuts at the stop.
    n = int(CARRY_SECONDS * RATE)
    t = np.arange(n) / RATE
    speed = (t / CARRY_SECONDS) ** (CARRY_POWER - 1)
    place(buf, 0.0, rasp(n) * speed * 0.16)
    place(buf, CARRY_SECONDS, clunk(), 1.0)
    # The mechanism: finer, quicker teeth through the cradles' raise (DENIED, then APPROVED).
    start = RAISE_FROM * CARRY_SECONDS
    for lane in range(2):
        s = start + lane * RAISE_STAGGER
        for k in range(9):
            place(buf, s + k * 0.021, tooth(1.35, 0.4), 0.16 + 0.03 * k)
    tail = int(0.36 * RATE)
    place(buf, start, rasp(tail, 2500, 7000) * env(tail, 0.03, 0.12) * 0.07)
    return buf


def drawer_close():
    total = SHOVE_SECONDS + 0.5
    buf = np.zeros(int(total * RATE))
    teeth = 12
    us = np.linspace(0, 1, 4001)
    p = us + SHOVE_KICK * us * (1 - us)
    for k in range(1, teeth):
        u = us[np.searchsorted(p, k / teeth)]
        place(buf, u * SHOVE_SECONDS - 0.003, tooth(0.92, 1.0), 0.42 - 0.018 * k)
    n = int(SHOVE_SECONDS * RATE)
    t = np.arange(n) / RATE
    place(buf, 0.0, rasp(n, 1100, 4200) * (1.0 + SHOVE_KICK * (1 - 2 * t / SHOVE_SECONDS)) / (1 + SHOVE_KICK) * 0.15)
    place(buf, SHOVE_SECONDS, clunk(52.0, 0.48, 0.5), 1.1)
    return buf


def write(path, x):
    peak = np.abs(x).max()
    x = x / peak * (10 ** (-3 / 20))
    fade = int(0.01 * RATE)
    x[-fade:] *= np.linspace(1, 0, fade)
    data = (x * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    return x


def waveform(path, x, title, marks):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    t = np.arange(len(x)) / RATE
    fig, ax = plt.subplots(figsize=(10, 2.6), dpi=110)
    ax.plot(t, x, lw=0.4, color="#8A2F3B")
    for at, label in marks:
        ax.axvline(at, color="#4A5872", lw=0.8, ls="--")
        ax.text(at, 0.92, " " + label, color="#4A5872", fontsize=8, va="top")
    ax.set_xlim(0, t[-1])
    ax.set_ylim(-1, 1)
    ax.set_xlabel("seconds")
    ax.set_title(title, fontsize=10)
    fig.tight_layout()
    fig.savefig(path)
    plt.close(fig)


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
    pngs = sys.argv[2] if len(sys.argv) > 2 else None
    out = os.path.join(root, "Assets", "Art", "Office", "Sounds")
    os.makedirs(out, exist_ok=True)
    o = write(os.path.join(out, "drawer_open.wav"), drawer_open())
    c = write(os.path.join(out, "drawer_close.wav"), drawer_close())
    print("wrote", out, f"open {len(o) / RATE:.2f} s, close {len(c) / RATE:.2f} s")
    if pngs:
        os.makedirs(pngs, exist_ok=True)
        waveform(os.path.join(pngs, "drawer_open_waveform.png"), o, "drawer_open: the carry's ratchet speeding up, the clunk at the stop, the cradles' raise",
                 [(0.0, "carry sets off"), (CARRY_SECONDS, "stop (clunk)"), (RAISE_FROM * CARRY_SECONDS, "raise")])
        waveform(os.path.join(pngs, "drawer_close_waveform.png"), c, "drawer_close: the shove's reverse rasp slowing, the thud as it seats",
                 [(0.0, "shove"), (SHOVE_SECONDS, "seated (thud)")])


main()
