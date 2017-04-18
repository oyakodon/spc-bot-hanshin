using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace spc_bot_hanshin.Tests
{
    [TestClass()]
    public class RationalTests
    {
        [TestMethod()]
        public void 同じ分数は同じ()
        {
            Assert.IsTrue(new Rational(1, 2) == new Rational(1, 2));
            Assert.IsTrue(new Rational(1, 2) == new Rational(114, 228));

            Assert.IsFalse(new Rational(1, 2) == new Rational(114, 514));
        }

        [TestMethod()]
        public void 四則演算OK()
        {
            Assert.IsTrue(new Rational(1, 2) + new Rational(1, 3) == new Rational(5, 6));
            Assert.IsTrue(new Rational(1, 2) * new Rational(1, 3) == new Rational(1, 6));
            Assert.IsTrue(new Rational(1, 2) - new Rational(1, 5) == new Rational(3, 10));
            Assert.IsTrue(new Rational(1, 2) / new Rational(1, 3) == new Rational(3, 2));

            Assert.IsTrue(new Rational(0.5) + 1 == new Rational(3, 2));
        }

        [TestMethod()]
        public void 小数OK()
        {
            Assert.IsTrue(new Rational(0.5) == new Rational(1, 2));
            Assert.IsTrue(new Rational(0.3) == new Rational(3, 10));
            Assert.IsTrue(new Rational(1.5) == new Rational(3, 2));
        }

        [TestMethod()]
        public void ゼロ除算()
        {
            try
            {
                var rat = new Rational(0, 0);
                Assert.Fail("例外が発生しなかった!");
            }
            catch (DivideByZeroException ex)
            {
                Assert.IsTrue(ex.Message == "Denominator is Zero");
            }
        }

        [TestMethod()]
        public void 負の数OK()
        {
            Assert.IsTrue(new Rational(-2, 1) == new Rational(2, -1));
            Assert.IsTrue(new Rational(2, -1) == -2);
        }

        [TestMethod()]
        public void 最大公約数()
        {
            var pt = new PrivateType(typeof(Rational));

            Assert.IsTrue((long)pt.InvokeStatic("gcd", 10, 15) == 5);
            Assert.IsTrue((long)pt.InvokeStatic("gcd", 630, 300) == 30);
            Assert.IsTrue((long)pt.InvokeStatic("gcd", 2, 3) == 1);
        }

        [TestMethod()]
        public void 最大公約数_逆にしてもOK()
        {
            var pt = new PrivateType(typeof(Rational));

            Assert.IsTrue((long)pt.InvokeStatic("gcd", 10, 15) == (long)pt.InvokeStatic("gcd", 15, 10));
            Assert.IsTrue((long)pt.InvokeStatic("gcd", 630, 300) == (long)pt.InvokeStatic("gcd", 300, 630));
            Assert.IsTrue((long)pt.InvokeStatic("gcd", 2, 3) == (long)pt.InvokeStatic("gcd", 3, 2));
        }

    }
}