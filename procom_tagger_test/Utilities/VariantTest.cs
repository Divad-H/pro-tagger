using Microsoft.VisualStudio.TestTools.UnitTesting;
using procom_tagger;

namespace procom_tagger_test
{
    [TestClass]
    public class VariantTest
    {
        [TestMethod]
        public void IntStringVariant()
        {
            const int intVal = 3;
            const string strVal = "rat";
            var variant = new Variant<string, int>(intVal);
            Assert.IsTrue(variant.Is<int>());
            Assert.IsFalse(variant.Is<string>());
            Assert.AreEqual(intVal, variant.Get<int>());
            variant.Assign(strVal);
            Assert.IsTrue(variant.Is<string>());
            Assert.IsFalse(variant.Is<int>());
            Assert.AreEqual(strVal, variant.Get<string>());
        }
    }
}
