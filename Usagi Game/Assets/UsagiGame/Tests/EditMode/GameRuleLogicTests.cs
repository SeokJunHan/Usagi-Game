using NUnit.Framework;

namespace UsagiGame.Tests
{
    public sealed class GameRuleLogicTests
    {
        [Test]
        public void PairKey_IsOrderIndependent()
        {
            Assert.That(GameRuleLogic.PairKey(3, 8), Is.EqualTo(GameRuleLogic.PairKey(8, 3)));
        }

        [Test]
        public void Merge_RequiresSameStageAndOlderPhysicsStep()
        {
            Assert.That(GameRuleLogic.CanMerge(2, 2, 3, 3, 4), Is.True);
            Assert.That(GameRuleLogic.CanMerge(2, 3, 3, 3, 4), Is.False);
            Assert.That(GameRuleLogic.CanMerge(2, 2, 4, 3, 4), Is.False);
        }

        [Test]
        public void OverflowTime_ResetsBelowDangerLine()
        {
            float elapsed = GameRuleLogic.UpdateOverflowTime(true, .6f, .02f);
            Assert.That(elapsed, Is.EqualTo(.62f).Within(.0001f));
            Assert.That(GameRuleLogic.UpdateOverflowTime(false, elapsed, .02f), Is.Zero);
        }
    }
}
