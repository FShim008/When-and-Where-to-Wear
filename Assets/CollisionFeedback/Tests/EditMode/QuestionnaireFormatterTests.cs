using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class QuestionnaireFormatterTests
    {
        [Test]
        public void Rows_emit_one_tidy_line_per_measure_with_invariant_decimals()
        {
            var measures = new Dictionary<string, float> { { "presence", 4.5f }, { "spatial", 3.0f } };
            var rows = new List<string>(QuestionnaireFormatter.Rows(7, 2, "PB", "IPQ", measures));

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows, Has.Some.StartsWith("7,2,PB,IPQ,measure,presence,4.5"));
            Assert.That(rows, Has.Some.StartsWith("7,2,PB,IPQ,measure,spatial,3"));
            Assert.That(QuestionnaireFormatter.Header(),
                Does.StartWith("participant,block,condition,instrument,kind,measure,value"));
        }

        [Test]
        public void NaN_or_Inf_is_written_as_NA()
        {
            var measures = new Dictionary<string, float> { { "x", float.NaN } };
            var rows = new List<string>(QuestionnaireFormatter.Rows(1, -1, "None", "SSQ", measures));
            Assert.That(rows[0], Does.EndWith(",NA"));
            Assert.That(rows[0], Does.StartWith("1,-1,None,SSQ,measure,x,"));
        }

        // ── Item-level export [§12] ──────────────────────────────────────────────────────────────

        [Test]
        public void Item_rows_are_emitted_and_marked_as_items()
        {
            var responses = new Dictionary<string, int> { { "ipq_g1", 5 }, { "ipq_sp2", 2 } };
            var rows = new List<string>(QuestionnaireFormatter.ItemRows(7, 2, "PB", "IPQ", responses));

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows, Has.Some.EqualTo("7,2,PB,IPQ,item,ipq_g1,5"));
            Assert.That(rows, Has.Some.EqualTo("7,2,PB,IPQ,item,ipq_sp2,2"));
        }

        [Test]
        public void Items_and_measures_are_distinguishable_even_when_named_the_same()
        {
            // The whole reason `kind` exists rather than relying on the name. An instrument whose item id
            // happens to match a subscale name would otherwise pool raw responses into the scored value,
            // silently and with no way to notice afterwards.
            var responses = new Dictionary<string, int> { { "total", 3 } };
            var measures = new Dictionary<string, float> { { "total", 42f } };

            var item = new List<string>(QuestionnaireFormatter.ItemRows(1, 0, "PB", "SSQ", responses))[0];
            var scored = new List<string>(QuestionnaireFormatter.Rows(1, 0, "PB", "SSQ", measures))[0];

            Assert.That(item, Does.Contain($",{QuestionnaireFormatter.KindItem},total,"));
            Assert.That(scored, Does.Contain($",{QuestionnaireFormatter.KindMeasure},total,"));
            Assert.That(item, Is.Not.EqualTo(scored));
        }

        [Test]
        public void Item_values_are_written_as_integers_not_floats()
        {
            // A raw Likert response is an integer. Writing "5" rather than "5.0" keeps the column readable
            // and stops a reader inferring a precision the instrument does not have.
            var rows = new List<string>(
                QuestionnaireFormatter.ItemRows(1, 0, "PB", "IPQ", new Dictionary<string, int> { { "q", 5 } }));
            Assert.That(rows[0], Does.EndWith(",5"));
        }

        [Test]
        public void Every_row_has_the_same_column_count_as_the_header()
        {
            int expected = QuestionnaireFormatter.Header().Split(',').Length;

            var scored = new List<string>(QuestionnaireFormatter.Rows(
                1, 0, "PB", "IPQ", new Dictionary<string, float> { { "presence", 4f } }));
            var items = new List<string>(QuestionnaireFormatter.ItemRows(
                1, 0, "PB", "IPQ", new Dictionary<string, int> { { "ipq_g1", 5 } }));

            foreach (string r in scored) Assert.That(r.Split(',').Length, Is.EqualTo(expected), r);
            foreach (string r in items) Assert.That(r.Split(',').Length, Is.EqualTo(expected), r);
        }

        [Test]
        public void Commas_in_labels_are_escaped_in_both_row_kinds()
        {
            var scored = new List<string>(QuestionnaireFormatter.Rows(
                1, 0, "PB", "CUE", new Dictionary<string, float> { { "helpful, overall", 4f } }));
            var items = new List<string>(QuestionnaireFormatter.ItemRows(
                1, 0, "PB", "CUE", new Dictionary<string, int> { { "helpful, overall", 4 } }));

            Assert.That(scored[0], Does.Contain("\"helpful, overall\""));
            Assert.That(items[0], Does.Contain("\"helpful, overall\""));
        }
    }
}
