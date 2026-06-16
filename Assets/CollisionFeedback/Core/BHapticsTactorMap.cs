namespace CollisionFeedback.Core
{
    /// <summary>Owned bHaptics devices: TactSuit X40 (front/back) + 2 Tactosy for Hands + 2 Tactosy for Feet.</summary>
    public enum BHapticsDevice
    {
        VestFront,
        VestBack,
        HandLeft,
        HandRight,
        FootLeft,
        FootRight,
    }

    /// <summary>Where a cue plays: a device, the motor indices on it, and an intensity (per-site gain).</summary>
    public readonly struct TactorTarget
    {
        public readonly BHapticsDevice Device;
        public readonly int[] Motors;
        public readonly float Intensity; // 0..1

        public TactorTarget(BHapticsDevice device, int[] motors, float intensity)
        {
            Device = device;
            Motors = motors;
            Intensity = intensity;
        }
    }

    /// <summary>
    /// Pure mapping from a logical <see cref="HapticSite"/> to bHaptics device + motor indices [3F.2].
    /// Hardware-free + testable; the Runtime BHapticsSink turns a <see cref="TactorTarget"/> into the
    /// actual SDK call. NOTE: motor indices below are sensible defaults — verify them against the bHaptics
    /// SDK device layouts / motor counts for your exact units (X40 front = 20 motors; Tactosy varies).
    /// </summary>
    public static class BHapticsTactorMap
    {
        public static TactorTarget For(HapticSite site, float intensity = 1f) => site switch
        {
            HapticSite.Chest     => new TactorTarget(BHapticsDevice.VestFront, new[] { 6, 7, 8, 11, 12, 13 }, intensity),
            HapticSite.LeftHand  => new TactorTarget(BHapticsDevice.HandLeft,  new[] { 0, 1, 2 }, intensity),
            HapticSite.RightHand => new TactorTarget(BHapticsDevice.HandRight, new[] { 0, 1, 2 }, intensity),
            HapticSite.LeftShin  => new TactorTarget(BHapticsDevice.FootLeft,  new[] { 0, 1, 2 }, intensity),
            HapticSite.RightShin => new TactorTarget(BHapticsDevice.FootRight, new[] { 0, 1, 2 }, intensity),
            _                    => new TactorTarget(BHapticsDevice.VestFront, new[] { 6, 7, 8, 11, 12, 13 }, intensity),
        };
    }
}
