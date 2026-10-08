"""
Placeholder SFX for the Time Sorter desk scanner (44.1 kHz, 16-bit mono WAV, peak about -3 dBFS).

    python make_scanner_sfx.py

Writes next to this file:
    scanner_lid_open.wav   ~0.35 s  plastic hinge lift, soft spring creak, light click at the top
    scanner_lid_close.wav  ~0.30 s  firm plastic clack with a small rattle
    scanner_scan.wav       ~1.40 s  motor spin-up, carriage whirr out (rising), end click, return whirr, fan tail
and a waveform + spectrogram PNG for each (<name>_wave.png). Deterministic (fixed seeds), re-runnable.
"""
import os

import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
PEAK_DBFS = -3.0


def t_axis(dur):
    return np.arange(int(round(dur * SR))) / SR


def place(buf, snd, at):
    i = int(round(at * SR))
    n = min(len(snd), len(buf) - i)
    if n > 0:
        buf[i:i + n] += snd[:n]
    return buf


def bandpass(x, lo, hi, order=2):
    sos = signal.butter(order, [lo, hi], btype="band", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def lowpass(x, fc, order=2):
    return signal.sosfilt(signal.butter(order, fc, btype="low", fs=SR, output="sos"), x)


def highpass(x, fc, order=2):
    return signal.sosfilt(signal.butter(order, fc, btype="high", fs=SR, output="sos"), x)


def modes(dur, freqs, decays, amps, rng, jitter=0.0):
    """A struck plastic part: a few exponentially decaying partials with a soft attack."""
    t = t_axis(dur)
    out = np.zeros_like(t)
    for f, d, a in zip(freqs, decays, amps):
        f = f * (1 + jitter * rng.uniform(-1, 1))
        out += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 2 * np.pi)) * np.exp(-t / d)
    atk = np.minimum(1.0, t / 0.0006)
    return taper(out * atk)


def taper(x, ms=6.0):
    """Fade a snippet's last few ms so a cut-off decay never clicks."""
    n = min(len(x), int(ms * SR / 1000))
    x = x.copy()
    x[-n:] *= np.linspace(1, 0, n)
    return x


def click(rng, dur=0.04, bright=1.0, body=0.4, freqs=(2900, 4300, 6100), decay=0.006):
    t = t_axis(dur)
    tick = modes(dur, freqs, [decay, decay * 0.7, decay * 0.5], [1.0, 0.6 * bright, 0.35 * bright], rng, 0.03)
    noise = highpass(rng.standard_normal(len(t)), 2500) * np.exp(-t / 0.0015) * 0.5 * bright
    thump = body * np.sin(2 * np.pi * 190 * t) * np.exp(-t / 0.012)
    return taper(tick + noise + thump)


def finish(x, fade_in=0.002, fade_out=0.03):
    x = lowpass(x, 11000, 2)            # nothing harsh up top
    x = x - np.mean(x)
    n_in, n_out = int(fade_in * SR), int(fade_out * SR)
    x[:n_in] *= np.linspace(0, 1, n_in)
    x[-n_out:] *= np.linspace(1, 0, n_out) ** 2
    peak = np.max(np.abs(x))
    return x * (10 ** (PEAK_DBFS / 20) / peak)


# --------------------------------------------------------------------------------- lid open
def lid_open():
    rng = np.random.default_rng(11)
    dur = 0.36
    t = t_axis(dur)
    out = np.zeros_like(t)

    # hinge friction: soft band-limited rub that swells and fades as the lid swings up
    rub_env = np.clip(t / 0.05, 0, 1) * np.clip((0.27 - t) / 0.08, 0, 1)
    rub = bandpass(rng.standard_normal(len(t)), 350, 1600) * rub_env * 0.18
    out += rub

    # spring creak: stick-slip micro clicks through two plastic/metal resonances, rate and pitch rising
    rate = np.interp(t, [0.0, 0.26], [55, 110])
    phase = np.cumsum(rate) / SR
    ticks = np.where(np.diff(np.floor(phase), prepend=0) > 0)[0]
    creak = np.zeros_like(t)
    for k in ticks:
        if t[k] > 0.26:
            break
        amp = 0.25 * (0.6 + 0.4 * rng.random()) * rub_env[k]
        g = modes(0.02, (1150 * (1 + 0.5 * t[k]), 2350 * (1 + 0.4 * t[k])), (0.004, 0.0025), (1.0, 0.5), rng, 0.02)
        place(creak, amp * g, t[k])
    out += creak

    # the lid tops out on its stop: light click with a little body
    place(out, 0.55 * click(rng, 0.05, bright=0.8, body=0.5), 0.272)
    # a tiny settle bounce
    place(out, 0.12 * click(rng, 0.03, bright=0.5, body=0.2, freqs=(2500, 3900, 5600)), 0.297)
    return finish(out, fade_out=0.02)


# --------------------------------------------------------------------------------- lid close
def lid_close():
    rng = np.random.default_rng(23)
    dur = 0.31
    t = t_axis(dur)
    out = np.zeros_like(t)

    # a short whoosh of air as the lid comes down
    w_env = np.clip(t / 0.03, 0, 1) * np.clip((0.045 - t) / 0.01, 0, 1)
    out += bandpass(rng.standard_normal(len(t)), 300, 1200) * w_env * 0.08

    hit = 0.045
    # firm clack: low body thump + hollow plastic shell modes + a short bright noise edge
    tt = t_axis(0.25)
    thump = (np.sin(2 * np.pi * 118 * tt) + 0.5 * np.sin(2 * np.pi * 236 * tt)) * np.exp(-tt / 0.035)
    shell = modes(0.25, (690, 1330, 2120, 3380, 4700), (0.045, 0.03, 0.02, 0.012, 0.008), (0.8, 0.7, 0.55, 0.4, 0.25), rng, 0.01)
    edge = highpass(rng.standard_normal(len(tt)), 1800) * np.exp(-tt / 0.004) * 0.9
    place(out, taper(0.9 * thump + 0.75 * shell + edge, 20), hit)

    # small rattle: the glass panel and the hinge pins settling
    for dt, a in ((0.031, 0.32), (0.052, 0.22), (0.071, 0.16), (0.094, 0.10), (0.118, 0.06)):
        r = modes(0.04, (rng.uniform(1700, 2400), rng.uniform(3000, 4200), rng.uniform(5000, 6200)),
                  (0.007, 0.005, 0.003), (1.0, 0.6, 0.3), rng)
        place(out, a * r, hit + dt + rng.uniform(-0.003, 0.003))
    return finish(out, fade_out=0.04)


# --------------------------------------------------------------------------------- scan
def motor_tone(f_inst, harmonics):
    phase = 2 * np.pi * np.cumsum(f_inst) / SR
    return sum(a * np.sin(k * phase) for k, a in harmonics)


def scan():
    rng = np.random.default_rng(7)
    dur = 1.42
    t = t_axis(dur)
    out = np.zeros_like(t)

    def env(points):
        xs, ys = zip(*points)
        return np.interp(t, xs, ys)

    # 1. motor spin-up hum (0 - 0.2 s): low hum climbing, with a soft relay tick at the start
    place(out, 0.22 * click(rng, 0.03, bright=0.4, body=0.6, freqs=(1800, 2900, 4100)), 0.004)
    f_hum = env([(0, 55), (0.18, 112), (1.2, 112), (1.42, 70)])
    hum = motor_tone(f_hum, [(1, 1.0), (2, 0.5), (3, 0.25), (4, 0.12)])
    out += 0.16 * hum * env([(0, 0), (0.15, 1), (1.12, 1), (1.40, 0)])

    # 2. carriage out (0.17 - 0.80 s): stepper whine rising in pitch + gear/belt whirr
    f_out = env([(0.17, 330), (0.30, 470), (0.78, 520), (0.80, 520)])
    whine = motor_tone(f_out, [(1, 1.0), (2, 0.45), (3, 0.2), (5, 0.08)])
    steps = 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * np.cumsum(f_out / 2) / SR))   # step-pulse grit
    grit = bandpass(rng.standard_normal(len(t)), 900, 3200) * (0.35 + 0.65 * lowpass(steps, 900))
    e_out = env([(0.17, 0), (0.24, 1), (0.74, 1.05), (0.79, 0.2), (0.80, 0)])
    wob = 1 + 0.06 * np.sin(2 * np.pi * 7.5 * t)
    out += e_out * wob * (0.20 * whine + 0.10 * grit)

    # 3. end-of-travel click (bright) at 0.79 s
    place(out, 0.75 * click(rng, 0.05, bright=1.0, body=0.35, freqs=(3300, 4900, 6500), decay=0.007), 0.792)

    # 4. carriage return (0.84 - 1.14 s): quicker, a touch higher, pitch falling as it lands
    f_ret = env([(0.84, 600), (0.95, 640), (1.12, 560), (1.14, 520)])
    whine_r = motor_tone(f_ret, [(1, 1.0), (2, 0.4), (3, 0.2), (5, 0.06)])
    grit_r = bandpass(rng.standard_normal(len(t)), 1000, 3400)
    e_ret = env([(0.84, 0), (0.88, 1), (1.10, 0.9), (1.145, 0)])
    out += e_ret * (0.18 * whine_r + 0.09 * grit_r)
    place(out, 0.28 * click(rng, 0.04, bright=0.5, body=0.6, freqs=(2100, 3300, 4800)), 1.145)   # home stop

    # 5. cooling fan: low airy noise under everything, left to tail out
    fan = lowpass(bandpass(rng.standard_normal(len(t)), 120, 2200), 1500)
    fan_tone = motor_tone(np.full(len(t), 180.0), [(1, 1.0), (2, 0.3)])
    e_fan = env([(0, 0), (0.25, 0.7), (1.12, 0.8), (1.18, 1.0), (1.42, 0)])
    out += e_fan * (0.09 * fan + 0.03 * fan_tone)
    return finish(out, fade_out=0.08)


def save(name, x):
    path = os.path.join(HERE, name + ".wav")
    wavfile.write(path, SR, np.int16(np.clip(x, -1, 1) * 32767))
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        fig, (a1, a2) = plt.subplots(2, 1, figsize=(9, 4.6), sharex=True, gridspec_kw=dict(height_ratios=[1, 1.3]))
        tt = np.arange(len(x)) / SR
        a1.plot(tt, x, lw=0.5, color="#2B6F6A")
        a1.axhline(10 ** (PEAK_DBFS / 20), color="#C0563A", lw=0.6, ls="--")
        a1.axhline(-10 ** (PEAK_DBFS / 20), color="#C0563A", lw=0.6, ls="--")
        a1.set_ylim(-1, 1)
        a1.set_title(f"{name}.wav  -  {len(x) / SR:.2f} s, peak {20 * np.log10(np.max(np.abs(x))):.1f} dBFS", fontsize=10)
        a2.specgram(x, NFFT=1024, Fs=SR, noverlap=896, cmap="magma")
        a2.set_ylim(0, 8000)
        a2.set_ylabel("Hz")
        a2.set_xlabel("s")
        fig.tight_layout()
        fig.savefig(os.path.join(HERE, name + "_wave.png"), dpi=110)
        plt.close(fig)
    except ImportError:
        pass
    print(f"{path}: {len(x) / SR:.3f} s, peak {20 * np.log10(np.max(np.abs(x))):.2f} dBFS")


if __name__ == "__main__":
    save("scanner_lid_open", lid_open())
    save("scanner_lid_close", lid_close())
    save("scanner_scan", scan())
