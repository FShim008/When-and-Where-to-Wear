using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards the shoulder/arm discomfort instrument [PAPER2_STUDY_DESIGN: between-block discomfort
    /// ratings; the shoulder-discomfort stopping rule].
    ///
    /// This is not a secondary measure. E2 is hundreds of repeated one-arm reaches, so musculoskeletal
    /// fatigue is its foreseeable harm, and `max` is the value an operator stops a session on. A scoring
    /// bug here does not bias an estimate — it lets a session run past the point the rule exists to catch.
    /// </summary>
    public class ShoulderDiscomfortTests
    {
        private static Dictionary<string, float> Score(int shoulder, int upperarm, int forearm, int neck)
            => Questionnaire.ShoulderDiscomfort().Score(new Dictionary<string, int>
            {
                { "SHOULDER", shoulder }, { "UPPERARM", upperarm },
                { "FOREARM", forearm },   { "NECK", neck },
            });

        [Test]
        public void Max_is_the_worst_site_not_the_average()
        {
            // THE POINT OF THE INSTRUMENT. A severe shoulder beside three comfortable sites must not be
            // averaged away: mean(9,0,0,0) = 2.25 would sail past a threshold of 7 that max(9) trips.
            var m = Score(shoulder: 9, upperarm: 0, forearm: 0, neck: 0);

            Assert.That(m["max"], Is.EqualTo(9f),
                "Scored on the maximum. If this ever becomes a mean, the stopping rule stops working and " +
                "nothing in the data would show it.");
            Assert.That(m["max"], Is.GreaterThan(Questionnaire.DiscomfortStopThreshold));
        }

        [Test]
        public void Per_site_values_are_reported_so_a_rise_can_be_localised()
        {
            var m = Score(3, 5, 2, 1);
            Assert.That(m["shoulder"], Is.EqualTo(3f));
            Assert.That(m["upperarm"], Is.EqualTo(5f));
            Assert.That(m["forearm"], Is.EqualTo(2f));
            Assert.That(m["neck"], Is.EqualTo(1f));
            Assert.That(m["max"], Is.EqualTo(5f));
        }

        [Test]
        public void A_comfortable_participant_scores_zero_and_does_not_trip_the_rule()
        {
            var m = Score(0, 0, 0, 0);
            Assert.That(m["max"], Is.EqualTo(0f));
            Assert.That(m["max"], Is.LessThan(Questionnaire.DiscomfortStopThreshold));
        }

        [Test]
        public void The_threshold_sits_inside_the_scale()
        {
            // A threshold above the maximum response would be unreachable, and the rule would never fire.
            var q = Questionnaire.ShoulderDiscomfort();
            int top = q.Items.Max(i => i.Max);
            Assert.That(Questionnaire.DiscomfortStopThreshold, Is.GreaterThan(0f));
            Assert.That(Questionnaire.DiscomfortStopThreshold, Is.LessThanOrEqualTo(top),
                $"Threshold {Questionnaire.DiscomfortStopThreshold} must be reachable on a 0-{top} scale.");
        }

        [Test]
        public void Items_use_the_borg_cr10_range_and_no_item_is_reverse_keyed()
        {
            // Reverse-keying a discomfort item would invert the safety rule: a comfortable participant
            // would score high and trip the stop, and a suffering one would not.
            foreach (QuestionnaireItem it in Questionnaire.ShoulderDiscomfort().Items)
            {
                Assert.That(it.Min, Is.EqualTo(0), it.Id);
                Assert.That(it.Max, Is.EqualTo(10), it.Id);
                Assert.That(it.Reverse, Is.False,
                    $"{it.Id} must not be reverse-keyed — it would invert the stopping rule.");
            }
        }

        [Test]
        public void It_covers_the_sites_a_repeated_reach_loads()
        {
            var ids = Questionnaire.ShoulderDiscomfort().Items.Select(i => i.Id).ToList();
            Assert.That(ids, Is.EquivalentTo(new[] { "SHOULDER", "UPPERARM", "FOREARM", "NECK" }));
        }

        [Test]
        public void The_instrument_name_matches_what_the_runner_checks()
        {
            // E2SessionRunner applies the stopping rule by comparing Instrument == "DISCOMFORT".
            // A rename here without a matching change there would silently disable the rule.
            Assert.That(Questionnaire.ShoulderDiscomfort().Instrument, Is.EqualTo("DISCOMFORT"));
        }
    }
}
