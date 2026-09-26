namespace CollisionFeedback.Core
{
    /// <summary>
    /// Owned bHaptics devices.
    ///
    /// **Five Tactosy units carry the warning cue** — sternum, both forearms, both feet — so the manipulated
    /// factor uses one device family, one driver and one mounting style at every site
    /// [PAPER1_STUDY_DESIGN §2, DECISION 2026-09-07].
    ///
    /// The TactSuit vest is retained **for projectile-hit task feedback only** and never delivers a warning
    /// cue. See <see cref="BHapticsTactorMap"/> for why that separation is load-bearing.
    /// </summary>
    public enum BHapticsDevice
    {
        /// <summary>Warning cue, generic (torso) condition. Tactosy at the sternum — NOT the vest.</summary>
        TactosyTorso,
        HandLeft,
        HandRight,
        FootLeft,
        FootRight,

        /// <summary>
        /// Task feedback only: projectile hits. **Never a warning cue.** Identical in every condition and
        /// carrying no hazard information, it is permitted under the §10 task-versus-hazard constraint.
        /// </summary>
        VestFront,
        /// <summary>Task feedback only. See <see cref="VestFront"/>.</summary>
        VestBack,
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
    /// Hardware-free and testable; the Runtime BHapticsSink turns a <see cref="TactorTarget"/> into the
    /// actual SDK call.
    ///
    /// ── WHY THE CHEST SITE IS A TACTOSY, NOT THE VEST — THIS IS H2 ─────────────────────────────────────
    /// H2 contrasts **somatotopic limb cueing against an undifferentiated torso alert**. If the torso cue
    /// came from the vest while the limb cues came from Tactosy units, then "localized versus generic" would
    /// be bundled with "different actuator, different driver, different mounting, different salience", and
    /// **H2 could not be interpreted as an anatomical-mapping effect at all** — only as a device × site
    /// effect, which would force the weaker construct name through the title, hypotheses and abstract
    /// [PAPER1_STUDY_DESIGN §2, Option B].
    ///
    /// The design closed this on 2026-09-07 by adopting Option C: a Tactosy at the sternum. This map is
    /// where that decision either holds or silently reverts, so the vest is deliberately **unreachable**
    /// from <see cref="For"/> — including from the default branch, which previously fell back to the vest
    /// and would have re-created the confound for any site added later.
    ///
    /// ⚠ Motor indices are sensible defaults. Verify against the SDK layout for your exact units before
    /// collection; a wrong index is a cue on the wrong part of the body, which is H2's whole subject.
    /// </summary>
    public static class BHapticsTactorMap
    {
        private static readonly int[] TactosyMotors = { 0, 1, 2 };

        public static TactorTarget For(HapticSite site, float intensity = 1f) => site switch
        {
            // Generic condition. Sternum Tactosy — matched by construction to the limb units.
            HapticSite.Chest     => new TactorTarget(BHapticsDevice.TactosyTorso, TactosyMotors, intensity),
            HapticSite.LeftHand  => new TactorTarget(BHapticsDevice.HandLeft,     TactosyMotors, intensity),
            HapticSite.RightHand => new TactorTarget(BHapticsDevice.HandRight,    TactosyMotors, intensity),
            HapticSite.LeftShin  => new TactorTarget(BHapticsDevice.FootLeft,     TactosyMotors, intensity),
            HapticSite.RightShin => new TactorTarget(BHapticsDevice.FootRight,    TactosyMotors, intensity),

            // No vest fallback. An unmapped site is a bug to surface, not to route to the torso: falling
            // back to the vest would put a warning cue on the task-feedback device and reopen the confound.
            _ => throw new System.ArgumentOutOfRangeException(
                     nameof(site), site,
                     "No warning-cue tactor mapped for this site. Add a Tactosy mapping — do not route " +
                     "warning cues to the vest (PAPER1_STUDY_DESIGN §2, H2 device confound)."),
        };

        /// <summary>
        /// True when the device is allowed to deliver a **warning cue**. The vest is not.
        ///
        /// Exposed so the Runtime sink and any future routing can assert it rather than trusting that this
        /// file never changes — the H2 confound is one careless default branch away from returning.
        /// </summary>
        public static bool CarriesWarningCue(BHapticsDevice device) =>
            device != BHapticsDevice.VestFront && device != BHapticsDevice.VestBack;
    }
}
