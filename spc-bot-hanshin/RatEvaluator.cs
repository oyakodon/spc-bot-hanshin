using System;
using System.Text.RegularExpressions;

namespace spc_bot_hanshin
{
    public class RatEvaluator
    {
        /// <summary>
        /// 与えられた式を計算します。
        /// </summary>
        public Rational eval(string _expr)
        {
            expr = _expr;
            pos = 0;
            return eval_expr();
        }

        /// <summary>
        /// 式
        /// </summary>
        private string expr { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        private int pos { get; set; }

        /// <summary>
        /// 次の文字を返す
        /// </summary>
        private char next_ch()
        {
            return pos < expr.Length ? expr[pos] : ' ';
        }

        /// <summary>
        /// 式
        /// </summary>
        private Rational eval_expr()
        {
            var res = eval_term();
            while (true)
            {
                switch (next_ch())
                {
                    case '+':
                        pos++;
                        res += eval_term();
                        break;

                    case '-':
                        pos++;
                        res -= eval_term();
                        break;

                    default:
                        return res;
                }
            }
        }

        /// <summary>
        /// 項
        /// </summary>
        private Rational eval_term()
        {
            var res = eval_fact();
            while (true)
            {
                switch (next_ch())
                {
                    case '*':
                        pos++;
                        res *= eval_fact();
                        break;

                    case '/':
                        pos++;
                        res /= eval_fact();
                        break;

                    default:
                        return res;
                }
            }
        }

        /// <summary>
        /// 因子
        /// </summary>
        private Rational eval_fact()
        {
            var res = new Rational(0, 1);

            if (next_ch() == '-')
            {
                pos++;
                if (!Regex.IsMatch(next_ch().ToString(), "[0-9]"))
                {
                    throw new FormatException("parse error: expected number.");
                }

                while (Regex.IsMatch(next_ch().ToString(), "[0-9]"))
                {
                    res *= 10;
                    res += next_ch() - '0';
                    pos++;
                }

                res *= -1;
            }
            else if (Regex.IsMatch(next_ch().ToString(), "[0-9]"))
            {
                while (Regex.IsMatch(next_ch().ToString(), "[0-9]"))
                {
                    res *= 10;
                    res += next_ch() - '0';
                    pos++;
                }
            }
            else if (next_ch() == '(')
            {
                pos++;
                res = eval_expr();
                if (next_ch() != ')')
                {
                    throw new FormatException("parse error: expected ')'.");
                }
                pos++;
            }
            else if (next_ch() == '-')
            {
                pos++;
                res = -1 * eval_expr();
            }
            else
            {
                throw new FormatException("parse error: expected number or '('.");
            }

            return res;
        }


    }
}
