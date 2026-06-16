#!/usr/bin/env python3
"""M4 bench smoke test: stream a fake moving skeleton to TrackingBench over UDP.

Verifies the whole receive -> parse -> score -> render path with NO hardware.
Usage:  python m4_test_stream.py [host] [port]        (default 127.0.0.1 9000)

Then press Play on the Bench_M4 scene: you should see 6 spheres (a little stick
figure), the RIGHT HAND waving, ~30 Hz on the HUD, sub-mm jitter, everything green.
Ctrl+C to stop.
"""
import socket, sys, time, math, random

host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
port = int(sys.argv[2]) if len(sys.argv) > 2 else 9000
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Joint order MUST match CollisionFeedback.Core.Joint:
# Head, Chest, LeftHand, RightHand, LeftFoot, RightFoot
base = [
    (0.00, 1.70, 0.00),   # Head
    (0.00, 1.30, 0.00),   # Chest
    (-0.20, 1.00, 0.00),  # LeftHand
    (0.20, 1.00, 0.00),   # RightHand
    (-0.15, 0.05, 0.00),  # LeftFoot
    (0.15, 0.05, 0.00),   # RightFoot
]

t0 = time.time()
print(f"Streaming a fake skeleton to {host}:{port} at 30 Hz. Ctrl+C to stop.")
try:
    while True:
        t = time.time() - t0
        pts = list(base)
        pts[3] = (0.20 + 0.30 * math.sin(2 * t), 1.00 + 0.20 * math.sin(3 * t), 0.00)  # wave RightHand
        vals = [f"{t:.5f}"]
        for (x, y, z) in pts:
            # +/- 0.5 mm noise on every joint so nothing reads as "frozen" (mimics real tracking)
            vals += [f"{x + random.uniform(-5e-4, 5e-4):.5f}",
                     f"{y + random.uniform(-5e-4, 5e-4):.5f}",
                     f"{z + random.uniform(-5e-4, 5e-4):.5f}"]
        sock.sendto((",".join(vals) + "\n").encode("ascii"), (host, port))
        time.sleep(1 / 30)
except KeyboardInterrupt:
    print("\nstopped.")
