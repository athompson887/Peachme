// Checked against something outside the code: the order the states joined the
// Union (US National Archives / Senate records), not against StageTable itself.

using NUnit.Framework;
using PeachMe.SimCore;

namespace PeachMe.Tests
{
    public class StageTableTests
    {
        [Test]
        public void FiftyStatesThenWashingtonDc()
        {
            Assert.AreEqual(51, StageTable.Count);
            Assert.AreEqual("DC", StageTable.Stages[50].Code);
        }

        [Test]
        public void AdmissionOrderMatchesTheRecord()
        {
            // Delaware ratified first (7 Dec 1787); Pennsylvania and New Jersey followed that month.
            Assert.AreEqual("DE", StageTable.Stages[0].Code);
            Assert.AreEqual("PA", StageTable.Stages[1].Code);
            Assert.AreEqual("NJ", StageTable.Stages[2].Code);
            // Rhode Island was the last of the thirteen (29 May 1790).
            Assert.AreEqual("RI", StageTable.Stages[12].Code);
            // Texas 28th (1845), California 31st (1850), Oklahoma 46th (1907).
            Assert.AreEqual("TX", StageTable.Stages[27].Code);
            Assert.AreEqual("CA", StageTable.Stages[30].Code);
            Assert.AreEqual("OK", StageTable.Stages[45].Code);
            // Alaska 49th, Hawaii 50th (1959).
            Assert.AreEqual("AK", StageTable.Stages[48].Code);
            Assert.AreEqual("HI", StageTable.Stages[49].Code);
        }

        [Test]
        public void EveryCodeIsUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var s in StageTable.Stages)
            {
                Assert.IsTrue(seen.Add(s.Code), "duplicate stage code " + s.Code);
            }
        }

        [Test]
        public void ChaptersOfSixStartWhereTheyShould()
        {
            Assert.AreEqual(0, StageTable.ChapterStart(5, 6));
            Assert.AreEqual(6, StageTable.ChapterStart(6, 6));
            Assert.AreEqual(48, StageTable.ChapterStart(50, 6));
            Assert.AreEqual(8, StageTable.ChapterOf(50, 6));
        }
    }
}
