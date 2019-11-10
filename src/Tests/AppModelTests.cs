using System;
using UltraCal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AppModelTests
    {
        [TestMethod]
        public void ConvertMeasure_Works()
        {
            var target = new AppModel();

            Assert.AreEqual(10.0, target.ConvertMeasure(10.0, CardUnits.Millimeters, CardUnits.Millimeters));
            Assert.AreEqual(1.0, target.ConvertMeasure(10.0, CardUnits.Millimeters, CardUnits.Centimeters));
            Assert.AreEqual(0.3937, target.ConvertMeasure(10.0, CardUnits.Millimeters, CardUnits.Inches));
            Assert.AreEqual(100.0, target.ConvertMeasure(10.0, CardUnits.Centimeters, CardUnits.Millimeters));
            Assert.AreEqual(10.0, target.ConvertMeasure(10.0, CardUnits.Centimeters, CardUnits.Centimeters));
            Assert.AreEqual(3.937, target.ConvertMeasure(10.0, CardUnits.Centimeters, CardUnits.Inches));
            Assert.AreEqual(253.9999, target.ConvertMeasure(10.0, CardUnits.Inches, CardUnits.Millimeters));
            Assert.AreEqual(25.4, target.ConvertMeasure(10.0, CardUnits.Inches, CardUnits.Centimeters));
            Assert.AreEqual(10.0, target.ConvertMeasure(10.0, CardUnits.Inches, CardUnits.Inches));

            Assert.AreEqual(1.524, target.ConvertMeasure(0.06, CardUnits.Inches, CardUnits.Millimeters));

        }
    }
}
