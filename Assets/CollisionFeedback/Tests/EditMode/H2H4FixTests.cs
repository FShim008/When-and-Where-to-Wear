using System;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards for the two confounds that made H2 and H4 uninterpretable
    /// [PAPER1_STUDY_DESIGN §2; docs/PAPER1_CODE_GAP.md #2, #3, #4].
    ///
    /// Both failures were silent. The vest fallback produced a working cue on the wrong device; the hazard
    /// glow produced a working display with the wrong information. Neither would have shown up in a dry run
    /// — only in a reviewer's question after collection. Hence tests rather than comments.
    /// </summary>
    public class H2H4FixTests
    {
        // ── H2: the warning cue must never come from the vest ────────────────────────────────────────

        [Test]
        public void Chest_warning_cue_is_a_torso_tactosy_not_the_vest()
        {
            TactorTarget t = BHapticsTactorMap.For(HapticSite.Chest);

            Assert.That(t.Device, Is.EqualTo(BHapticsDevice.TactosyTorso),
                "The generic condition's cue must come from a sternum Tactosy. Routing it to the vest " +
                "bundles 'localized vs generic' with 'different device', and H2 stops being an " +
                "anatomical-mapping hypothesis (§2, Option B).");
        }

        [Test]
        public void Every_warning_site_uses_the_same_device_family()
        {
            // This is the whole point of the 2026-09-07 decision: one device family, one driver, one
            // mounting style at every site, so the only thing differing between conditions is WHERE.
            var sites = new[] { HapticSite.Chest, HapticSite.LeftHand, HapticSite.RightHand,
                                HapticSite.LeftShin, HapticSite.RightShin };

            foreach (HapticSite s in sites)
            {
                TactorTarget t = BHapticsTactorMap.For(s);
                Assert.That(BHapticsTactorMap.CarriesWarningCue(t.Device), Is.True,
                    $"{s} routes to {t.Device}, which is task-feedback hardware, not a warning-cue device.");
            }
        }

        [Test]
        public void Motor_layout_is_identical_across_warning_sites()
        {
            // Different motor counts per site would be a salience difference masquerading as a site
            // difference — the same confound in a subtler form.
            int[] chest = BHapticsTactorMap.For(HapticSite.Chest).Motors;
            foreach (HapticSite s in new[] { HapticSite.LeftHand, HapticSite.RightHand,
                                             HapticSite.LeftShin, HapticSite.RightShin })
                Assert.That(BHapticsTactorMap.For(s).Motors, Is.EqualTo(chest),
                    $"{s} drives a different motor set from the torso site.");
        }

        [Test]
        public void The_vest_is_not_reachable_as_a_warning_cue_target()
        {
            Assert.That(BHapticsTactorMap.CarriesWarningCue(BHapticsDevice.VestFront), Is.False);
            Assert.That(BHapticsTactorMap.CarriesWarningCue(BHapticsDevice.VestBack), Is.False);
        }

        [Test]
        public void An_unmapped_site_throws_rather_than_falling_back_to_the_vest()
        {
            // The previous default branch returned VestFront. A site added later would have silently
            // acquired a vest cue and re-created the confound with no test failing.
            Assert.That(() => BHapticsTactorMap.For((HapticSite)99),
                        Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // ── H4: the visual cue must carry limb identity and nothing else ─────────────────────────────

        [Test]
        public void Visual_cue_duration_matches_the_haptic_pulse_train()
        {
            // H4 isolates modality, so the two cues must have the same onset and the same duration.
            LimbCueVisualParams p = LimbCueVisualParams.MatchedToThreePulse;
            Assert.That(p.TotalSeconds, Is.EqualTo(0.420f).Within(1e-5f),
                "3 pulses x 100 ms + 2 gaps x 60 ms = 420 ms. If the haptic waveform changed, this must too.");
        }

        [Test]
        public void Visual_cue_is_transient_not_a_persistent_display()
        {
            LimbCueVisualParams p = LimbCueVisualParams.MatchedToThreePulse;

            Assert.That(LimbCueVisual.Intensity(-0.01f, p), Is.Zero, "nothing before onset");
            Assert.That(LimbCueVisual.Intensity(p.TotalSeconds, p), Is.Zero, "nothing at the end");
            Assert.That(LimbCueVisual.Intensity(p.TotalSeconds + 1f, p), Is.Zero, "nothing a second later");
            Assert.That(LimbCueVisual.IsActive(p.TotalSeconds + 0.001f, p), Is.False);

            // A persistent display would let the participant read hazard state continuously, which the
            // haptic conditions never allow.
        }

        [Test]
        public void Visual_cue_delivers_exactly_three_pulses()
        {
            LimbCueVisualParams p = LimbCueVisualParams.MatchedToThreePulse;

            int transitions = 0;
            bool wasOn = false;
            for (float t = 0f; t < p.TotalSeconds; t += 0.001f)
            {
                bool on = LimbCueVisual.Intensity(t, p) > 0.5f;
                if (on && !wasOn) transitions++;
                wasOn = on;
            }
            Assert.That(transitions, Is.EqualTo(3), "the visual train must have the same pulse count as the haptic one");
        }

        [Test]
        public void Visual_cue_is_dark_during_the_gaps()
        {
            LimbCueVisualParams p = LimbCueVisualParams.MatchedToThreePulse;
            // First gap runs 100-160 ms; sample its middle.
            Assert.That(LimbCueVisual.Intensity(0.130f, p), Is.Zero);
            // Second gap runs 260-320 ms.
            Assert.That(LimbCueVisual.Intensity(0.290f, p), Is.Zero);
        }

        [Test]
        public void Visual_cue_reaches_full_intensity_inside_each_pulse()
        {
            LimbCueVisualParams p = LimbCueVisualParams.MatchedToThreePulse;
            Assert.That(LimbCueVisual.Intensity(0.050f, p), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(LimbCueVisual.Intensity(0.210f, p), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(LimbCueVisual.Intensity(0.370f, p), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Visual_cue_names_the_same_limb_the_haptic_cue_would()
        {
            // Identical routing is what makes H4 a modality comparison rather than an information one.
            foreach (Joint j in new[] { Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot })
                Assert.That(LimbCueVisual.SiteFor(j), Is.EqualTo(SiteRouting.For(j)));
        }

        // ── H4: the trigger fix, still guarded ───────────────────────────────────────────────────────

        [Test]
        public void Visual_shares_PBs_predictive_trigger()
        {
            Assert.That(ConditionManager.TriggerFor(Condition.Visual),
                        Is.EqualTo(ConditionManager.TriggerFor(Condition.PB)),
                        "H4 compares PB against Visual. A different trigger confounds modality with timing.");
            Assert.That(ConditionManager.TriggerFor(Condition.Visual), Is.EqualTo(TriggerKind.Predictive));
        }

        [Test]
        public void Visual_is_localized_like_PB()
        {
            // Information-matched to PB, not PG. An undifferentiated visual cue would confound modality
            // with information content, and a PB advantage over it would prove little (§2).
            Assert.That(ConditionManager.IsLocalized(Condition.Visual),
                        Is.EqualTo(ConditionManager.IsLocalized(Condition.PB)),
                        "Visual must be information-matched to PB, not PG.");
            Assert.That(ConditionManager.IsLocalized(Condition.Visual), Is.True);
            Assert.That(ConditionManager.IsLocalized(Condition.PG), Is.False,
                        "PG is the generic arm; if Visual matched PG, H4 would confound modality with " +
                        "information content and a PB advantage would prove little.");
        }
    }
}
