using NUnit.Framework;
using UI;
using UnityEngine;

namespace Tests.EditMode
{
    public class ScaleToFitParentTests
    {
        static readonly Vector2 Design = new Vector2(984f, 1290f);

        [Test]
        public void ComputeScale_AvailableLargerThanDesign_ReturnsOne()
        {
            Assert.AreEqual(1f, ScaleToFitParent.ComputeScale(new Vector2(1080f, 1752f), Design));
        }

        [Test]
        public void ComputeScale_NarrowerThanDesign_ScalesByWidth()
        {
            Assert.AreEqual(979f / 984f, ScaleToFitParent.ComputeScale(new Vector2(979f, 1830f), Design), 1e-5f);
        }

        [Test]
        public void ComputeScale_ShorterThanDesign_ScalesByHeight()
        {
            Assert.AreEqual(1000f / 1290f, ScaleToFitParent.ComputeScale(new Vector2(1247f, 1000f), Design), 1e-5f);
        }

        [Test]
        public void ComputeScale_ZeroAvailable_ReturnsOne()
        {
            Assert.AreEqual(1f, ScaleToFitParent.ComputeScale(Vector2.zero, Design));
        }
    }
}
