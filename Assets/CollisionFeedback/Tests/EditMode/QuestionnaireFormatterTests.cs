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
            Assert.That(rows, Has.Some.StartsWith("7,2,PB,IPQ,presence,4.5"));
            Assert.That(rows, Has.Some.StartsWith("7,2,PB,IPQ,spatial,3"));
            Assert.That(QuestionnaireFormatter.Header(), Does.StartWith("participant,block,condition,instrument,measure,value"));
        }

        [Test]
        public void NaN_or_Inf_is_written_as_NA()
        {
            var measures = new Dictionary<string, float> { { "x", float.NaN } };
            var rows = new List<string>(QuestionnaireFormatter.Rows(1, -1, "None", "SSQ", measures));
            Assert.That(rows[0], Does.EndWith(",NA"));
            Assert.That(rows[0], Does.StartWith("1,-1,None,SSQ,x,"));
        }
    }
}
