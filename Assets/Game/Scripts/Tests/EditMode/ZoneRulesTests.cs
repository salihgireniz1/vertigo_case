using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class ZoneRulesTests
    {
        static readonly ProgressionRules Rules = new(safeZoneInterval: 5, superZoneInterval: 30, growthPerZone: 0.25f);

        [TestCase(1, WheelTier.Bronze)]
        [TestCase(4, WheelTier.Bronze)]
        [TestCase(5, WheelTier.Silver)]
        [TestCase(10, WheelTier.Silver)]
        [TestCase(25, WheelTier.Silver)]
        [TestCase(30, WheelTier.Golden)]
        [TestCase(60, WheelTier.Golden)]
        [TestCase(31, WheelTier.Bronze)]
        public void GetTier_FollowsSafeAndSuperIntervals(int zone, WheelTier expected)
        {
            Assert.AreEqual(expected, ZoneRules.GetTier(zone, Rules));
        }

        [TestCase(1, false)]
        [TestCase(5, true)]
        [TestCase(30, true)]
        [TestCase(7, false)]
        public void IsSafe_TrueOnlyForSilverAndGolden(int zone, bool expected)
        {
            Assert.AreEqual(expected, ZoneRules.IsSafe(zone, Rules));
        }

        [TestCase(100, 1, 100)]
        [TestCase(100, 2, 125)]
        [TestCase(100, 5, 200)]
        [TestCase(7, 3, 11)] // 7 * 1.5 = 10.5, rounds away from zero
        public void ScaleAmount_GrowsLinearlyPerZone(int baseAmount, int zone, int expected)
        {
            Assert.AreEqual(expected, ZoneRules.ScaleAmount(baseAmount, zone, Rules));
        }

        [TestCase(1, 5)]
        [TestCase(5, 5)]
        [TestCase(6, 10)]
        [TestCase(11, 15)]
        public void NextSafeZone_IsFirstSilverZoneAtOrAfter(int zone, int expected)
        {
            Assert.AreEqual(expected, ZoneRules.NextSafeZone(zone, Rules));
        }

        [TestCase(1, 30)]
        [TestCase(30, 30)]
        [TestCase(31, 60)]
        public void NextSuperZone_IsFirstGoldenZoneAtOrAfter(int zone, int expected)
        {
            Assert.AreEqual(expected, ZoneRules.NextSuperZone(zone, Rules));
        }
    }
}
