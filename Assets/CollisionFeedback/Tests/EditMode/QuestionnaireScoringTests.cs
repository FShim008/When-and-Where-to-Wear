using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class QuestionnaireScoringTests
    {
        private static Dictionary<string, int> Fill(Questionnaire q, int value)
        {
            var r = new Dictionary<string, int>();
            foreach (var it in q.Items) r[it.Id] = value;
            return r;
        }

        [Test]
        public void Instruments_have_the_expected_shape()
        {
            Assert.That(Questionnaire.Ipq().Instrument, Is.EqualTo("IPQ"));
            Assert.That(Questionnaire.Ipq().Items.Count, Is.EqualTo(14));
            Assert.That(Questionnaire.NasaTlx().Instrument, Is.EqualTo("NASA_TLX"));
            Assert.That(Questionnaire.NasaTlx().Items.Count, Is.EqualTo(6));
            Assert.That(Questionnaire.Ssq().Instrument, Is.EqualTo("SSQ"));
            Assert.That(Questionnaire.Ssq().Items.Count, Is.EqualTo(16));
        }

        [Test]
        public void AllAnswered_requires_every_item()
        {
            var q = Questionnaire.NasaTlx();
            var partial = new Dictionary<string, int> { { "MENTAL", 5 } };
            Assert.That(q.AllAnswered(partial), Is.False);
            Assert.That(q.AllAnswered(Fill(q, 5)), Is.True);
        }

        [Test]
        public void Ipq_midpoint_is_symmetric_under_reverse_keys()
        {
            var q = Questionnaire.Ipq();
            // Every item at the 0..6 midpoint (3): reverse-correction of 3 is still 3, so presence == 3.
            Assert.That(q.Score(Fill(q, 3))["presence"], Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void Ipq_all_max_accounts_for_four_reverse_items()
        {
            var q = Questionnaire.Ipq();
            // 10 normal items corrected to 6, 4 reverse items (SP2,SP3,INV1,INV3) corrected to 0 → 60/14.
            Assert.That(q.Score(Fill(q, 6))["presence"], Is.EqualTo(60f / 14f).Within(1e-3f));
        }

        [Test]
        public void NasaTlx_overall_is_mean_on_0_100_scale()
        {
            var q = Questionnaire.NasaTlx();
            Assert.That(q.Score(Fill(q, 10))["overall"], Is.EqualTo(50f).Within(1e-3f)); // 10/20 → 50/100

            var one = Fill(q, 0); one["MENTAL"] = 20;                                      // one dim maxed
            Assert.That(q.Score(one)["overall"], Is.EqualTo(100f / 6f).Within(1e-3f));
            Assert.That(q.Score(one)["mental"], Is.EqualTo(100f).Within(1e-3f));
        }

        [Test]
        public void Ssq_zero_is_zero_everywhere()
        {
            var q = Questionnaire.Ssq();
            var m = q.Score(Fill(q, 0));
            Assert.That(m["total"], Is.EqualTo(0f));
            Assert.That(m["nausea"], Is.EqualTo(0f));
            Assert.That(m["oculomotor"], Is.EqualTo(0f));
            Assert.That(m["disorientation"], Is.EqualTo(0f));
        }

        [Test]
        public void Ssq_single_symptom_hits_the_right_subscales()
        {
            var q = Questionnaire.Ssq();
            var r = new Dictionary<string, int>();
            foreach (var it in q.Items) r[it.Id] = 0;
            r["NAUSEA"] = 3; // NAUSEA belongs to both Nausea and Disorientation subscales
            var m = q.Score(r);
            Assert.That(m["nausea"], Is.EqualTo(3 * 9.54f).Within(1e-2f));
            Assert.That(m["disorientation"], Is.EqualTo(3 * 13.92f).Within(1e-2f));
            Assert.That(m["oculomotor"], Is.EqualTo(0f));
            Assert.That(m["total"], Is.EqualTo((3 + 0 + 3) * 3.74f).Within(1e-2f));
        }

        [Test]
        public void Ssq_all_severe_matches_kennedy_weighting()
        {
            var q = Questionnaire.Ssq();
            // each subscale raw sum = 7 items × 3 = 21.
            var m = q.Score(Fill(q, 3));
            Assert.That(m["nausea"], Is.EqualTo(21 * 9.54f).Within(1e-2f));
            Assert.That(m["oculomotor"], Is.EqualTo(21 * 7.58f).Within(1e-2f));
            Assert.That(m["disorientation"], Is.EqualTo(21 * 13.92f).Within(1e-2f));
            Assert.That(m["total"], Is.EqualTo((21 + 21 + 21) * 3.74f).Within(1e-2f));
        }
    }
}
