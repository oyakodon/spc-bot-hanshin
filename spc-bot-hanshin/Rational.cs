using System;

namespace spc_bot_hanshin
{
    /// <summary>
    /// 実数
    /// </summary>
    public class Rational
    {
        public Rational(long num, long den)
        {
            if (den == 0) throw new DivideByZeroException("Denominator is Zero");

            var r = num == 0 ? 1 : gcd(num, den);
            Numerator = num / r;
            Denominator = den / r;
        }

        public Rational(long val) : this((decimal)val) { }
        public Rational(double val) : this((decimal)val) { }
        public Rational(decimal val)
        {
            // 分母や分子に小数を入れないために桁を繰り上げる
            var sign = val < 0 ? -1 : 1;
            var num = Math.Abs(val);
            long den = 1;

            while ((num % 10) != 0)
            {
                num *= 10;
                den *= 10;
            }

            var r = num == 0 ? 1 : gcd((long)num, den);
            Numerator = sign * (long)(num / r);
            Denominator = den / r;
        }

        public static implicit operator Rational(long val)
        {
            return new Rational(val);
        }

        public static implicit operator Rational(double val)
        {
            return new Rational(val);
        }

        public static implicit operator Rational(decimal val)
        {
            return new Rational(val);
        }

        public static explicit operator long(Rational a)
        {
            return a.Numerator / a.Denominator;
        }

        public static explicit operator double(Rational a)
        {
            return (double)a.Numerator / a.Denominator;
        }

        public static explicit operator decimal(Rational a)
        {
            return (decimal)a.Numerator / a.Denominator;
        }

        public static Rational operator +(Rational a, Rational b)
        {
            var num = a.Numerator * b.Denominator + b.Numerator * a.Denominator;
            var den = a.Denominator * b.Denominator;
            return new Rational(num, den);
        }

        public static Rational operator -(Rational a, Rational b)
        {
            var num = a.Numerator * b.Denominator - b.Numerator * a.Denominator;
            var den = a.Denominator * b.Denominator;
            return new Rational(num, den);
        }

        public static Rational operator *(Rational a, Rational b)
        {
            var num = a.Numerator * b.Numerator;
            var den = a.Denominator * b.Denominator;
            return new Rational(num, den);
        }

        public static Rational operator /(Rational a, Rational b)
        {
            return a * new Rational(b.Denominator, b.Numerator);
        }

        public static bool operator ==(Rational a, Rational b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

            var c = a - b;
            return c.Numerator == 0;
        }

        public static bool operator !=(Rational a, Rational b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            return this == (Rational)obj;
        }

        public override int GetHashCode()
        {
            return (int)(Numerator ^ Denominator);
        }

        private static long gcd(long a, long b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);

            if (a < b) return gcd(b, a);
            if (b < 1) throw new ArgumentException("b < 1");

            if (a % b == 0) return b;
            return gcd(b, a % b);
        }

        public override string ToString()
        {
            if (Numerator == 0) return "0";
            if (Denominator == 1) return Numerator.ToString();

            return string.Format("{0} / {1}", Numerator, Denominator);
        }

        public long Numerator { get; private set; }
        public long Denominator { get; private set; }

    }
}
