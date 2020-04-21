using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Wpf;
using System.Globalization;
using System.Reactive.Subjects;
using System.Windows.Media;

namespace ProTaggerTest.Wpf
{
    [TestClass]
    public class BoolToBrushConverterTest
    {
        [TestMethod]
        public void ConvertsToBrush()
        {
            var falseBrush = new SolidColorBrush(Colors.Black);
            var trueBrush = new SolidColorBrush(Colors.White);
            var converter = new BoolToBrushConverter() { FalseBrush = falseBrush, TrueBrush = trueBrush };
            using var source = new BehaviorSubject<bool?>(null);
            var convertedBrush = (Brush?)converter.Convert(false, typeof(Brush), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(falseBrush, convertedBrush);
            convertedBrush = (Brush?)converter.Convert(true, typeof(Brush), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(trueBrush, convertedBrush);
        }
    }
}
