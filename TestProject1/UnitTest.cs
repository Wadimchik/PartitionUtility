using NUnit.Framework;
using PartitionUtility;

namespace TestProject
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_Security_Protect()
        {
            string data = "12345678";
            string actual = Security.Protect(data);

            string expected = "MTIzNDU2Nzg=";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_Security_Unprotect()
        {
            string data = "MTIzNDU2Nzg=";
            string actual = Security.Unprotect(data);

            string expected = "12345678";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_MeasureComparer_ScaleValue_1()
        {
            decimal actual = MeasureComparer.ScaleValue(2, 0, 1);
            decimal expected = 60;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_MeasureComparer_ScaleValue_2()
        {
            decimal actual = MeasureComparer.ScaleValue(2, 0, 2);
            decimal expected = 730;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_MeasureComparer_ScaleValue_3()
        {
            decimal actual = MeasureComparer.ScaleValue(60, 1, 0);
            decimal expected = 2;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_MeasureComparer_ScaleValue_4()
        {
            decimal actual = MeasureComparer.ScaleValue(2, 1, 2);
            decimal expected = 24;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_MeasureComparer_ScaleValue_5()
        {
            decimal actual = MeasureComparer.ScaleValue(730, 2, 0);
            decimal expected = 2;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_MeasureComparer_ScaleValue_6()
        {
            decimal actual = MeasureComparer.ScaleValue(24, 2, 1);
            decimal expected = 2;
            Assert.AreEqual(expected, actual);
        }
    }
}