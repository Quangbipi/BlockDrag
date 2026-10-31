using NUnit.Framework;
using UI;

namespace Tests.EditMode
{
    public class InitLoadingCanvasTests
    {
        [Test]
        public void FormatLoadingText_Zero_ShowsZeroPercent()
        {
            Assert.AreEqual("LOADING... 0%", InitLoadingCanvas.FormatLoadingText(0f));
        }

        [Test]
        public void FormatLoadingText_Fraction_TruncatesPercent()
        {
            Assert.AreEqual("LOADING... 65%", InitLoadingCanvas.FormatLoadingText(0.657f));
        }

        [Test]
        public void FormatLoadingText_OutOfRange_Clamps()
        {
            Assert.AreEqual("LOADING... 0%", InitLoadingCanvas.FormatLoadingText(-1f));
            Assert.AreEqual("LOADING... 100%", InitLoadingCanvas.FormatLoadingText(2f));
        }
    }
}
