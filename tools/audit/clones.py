"""Duplicate code blocks by token-window hashing (CPD-style).

Token stream per file = the lexer's tokens (comments/whitespace already
gone) minus `using` directives and declaration attribute sections. Two modes:
  exact       tokens compared verbatim;
  normalized  identifiers -> ID, numbers -> NUM, strings -> STR, chars -> CHR
              (keywords and punctuation kept), so renamed copies match.

Every window of W consecutive tokens is hashed (blake2b over the token ids;
deterministic). For every pair of equal windows whose preceding windows are
NOT equal (a pair start), the match is extended token by token to its maximal
length -> a clone pair. Pairs overlapping themselves inside one file are
dropped. A same-file pair whose whole stretch repeats with a shorter period
(another copy of the window lies between the two and the shorter shift
matches all the way) is a periodic run - typically a data table of
same-shaped rows - and is reported as a periodic region instead of a clone.
Pairs with the identical token sequence are merged into a clone group (all
its locations). A 3-way clone whose copies share different
lengths shows up as several groups (one per distinct maximal sequence).

Duplicated lines = code lines covered by the tokens of any clone location,
each line counted once per file.
"""
from __future__ import annotations

import hashlib
from array import array
from collections import defaultdict

NORMALIZE = {"id": "ID", "number": "NUM", "string": "STR", "char": "CHR"}


def token_stream(fp, mode: str):
    """(keys, spans) for a FileParse: keys = comparable token strings,
    spans = (line, end_line) of each kept token."""
    excluded = set()
    for lo, hi in list(fp.usings) + list(fp.attr_ranges):
        excluded.update(range(lo, hi))
    keys, spans = [], []
    for i, t in enumerate(fp.tokens):
        if i in excluded:
            continue
        if mode == "normalized":
            keys.append(NORMALIZE.get(t.kind, t.text))
        else:
            keys.append(t.kind + ":" + t.text)
        spans.append((t.line, t.end_line))
    return keys, spans


def find_clones(streams: dict, W: int):
    """streams: {path: (keys, spans)}. Returns a list of groups:
    {"tokens": n, "locations": [(path, start_tok, end_tok_excl, line, end_line)]}."""
    vocab = {}
    paths = sorted(streams)
    ids, hashes = {}, {}
    occ = defaultdict(list)
    for path in paths:
        keys, _ = streams[path]
        arr = array("I", (vocab.setdefault(k, len(vocab)) for k in keys))
        ids[path] = arr
        b = arr.tobytes()
        hs = []
        for p in range(len(arr) - W + 1):
            h = hashlib.blake2b(b[4 * p:4 * (p + W)], digest_size=10).digest()
            hs.append(h)
            occ[h].append((path, p))
        hashes[path] = hs

    groups = {}
    periodic = []
    for path in paths:
        hs = hashes[path]
        for p, h in enumerate(hs):
            L = occ[h]
            if len(L) < 2 or L[0] != (path, p):
                continue   # handle each hash once, from its first occurrence
            for i in range(len(L)):
                fa, pa = L[i]
                for j in range(i + 1, len(L)):
                    fb, pb = L[j]
                    if pa > 0 and pb > 0 and hashes[fa][pa - 1] == hashes[fb][pb - 1]:
                        continue   # not a start: the match extends to the left
                    ia, ib = ids[fa], ids[fb]
                    n = W
                    while pa + n < len(ia) and pb + n < len(ib) and ia[pa + n] == ib[pb + n]:
                        n += 1
                    if fa == fb and pb < pa + n:
                        continue   # overlaps itself
                    if fa == fb and j > i + 1 and L[i + 1][0] == fa:
                        # Another copy of the window lies between the two: if the
                        # whole stretch repeats with that shorter period it is a
                        # periodic run (a data table), not a copied block.
                        q = L[i + 1][1] - pa
                        m = W
                        while pa + q + m < len(ia) and ia[pa + m] == ia[pa + q + m]:
                            m += 1
                        if pa + q + m >= pb + n:
                            periodic.append((fa, pa, pb + n))
                            continue
                    key = (n, hashlib.blake2b(ia[pa:pa + n].tobytes(), digest_size=16).digest())
                    g = groups.setdefault(key, set())
                    g.add((fa, pa))
                    g.add((fb, pb))
    out = []
    for (n, _), locs in groups.items():
        kept = []
        last = {}
        for f, p in sorted(locs):
            if f in last and p < last[f] + n:
                continue   # overlaps a kept copy in the same file
            kept.append((f, p))
            last[f] = p
        if len(kept) < 2:
            continue
        locations = []
        for f, p in kept:
            spans = streams[f][1]
            locations.append((f, p, p + n, spans[p][0], spans[p + n - 1][1]))
        out.append({"tokens": n, "locations": locations})
    out.sort(key=lambda g: (-g["tokens"], -len(g["locations"]), g["locations"][0][0], g["locations"][0][1]))
    return out, _merge_regions(periodic, streams)


def _merge_regions(regions, streams):
    """Merges overlapping (path, lo, hi) token ranges -> [(path, line, end_line, tokens)]."""
    by_file = defaultdict(list)
    for f, lo, hi in regions:
        by_file[f].append((lo, hi))
    out = []
    for f in sorted(by_file):
        cur = None
        for lo, hi in sorted(by_file[f]):
            if cur and lo <= cur[1]:
                cur[1] = max(cur[1], hi)
            else:
                if cur:
                    out.append((f, cur[0], cur[1]))
                cur = [lo, hi]
        out.append((f, cur[0], cur[1]))
    spans = {f: streams[f][1] for f in by_file}
    return [(f, spans[f][lo][0], spans[f][hi - 1][1], hi - lo) for f, lo, hi in out]


def duplicated_lines(groups, streams) -> dict:
    """{path: sorted list of code lines covered by clone tokens}."""
    cover = defaultdict(set)
    for g in groups:
        for f, lo, hi, _, _ in g["locations"]:
            spans = streams[f][1]
            for a, b in spans[lo:hi]:
                cover[f].update(range(a, b + 1))
    return {f: sorted(v) for f, v in sorted(cover.items())}


def run(files: dict, paths, mode: str, W: int):
    """files: {path: FileParse}. Returns (groups, periodic_regions, streams)."""
    streams = {p: token_stream(files[p], mode) for p in sorted(paths)}
    groups, periodic = find_clones(streams, W)
    return groups, periodic, streams
