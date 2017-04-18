using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace spc_bot_hanshin
{
    public class Hanshin
    {
        /// <summary>
        /// 阪神算
        /// </summary>
        public Hanshin(string _filename = "hanshin.json")
        {
            filename = _filename;
            if (System.IO.File.Exists(filename))
            {
                hdb = JsonMgr<HanshinDB>.Load(filename);
            }
            else
            {
                hdb = new HanshinDB();
                hdb.map = new Dictionary<int, string>();
                hdb.map[334] = "334";
                JsonMgr<HanshinDB>.Save(hdb, filename);
            }

            rat = new RatEvaluator();
        }

        /// <summary>
        /// 式が正しい形式かどうかを返す
        /// </summary>
        public bool isValid(string expr)
        {
            if (!Regex.IsMatch(expr, @"[34\+\-\*\/\(\)]")) return false;
            var arr = "334";
            int count = 0;

            for (var i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '3' || expr[i] == '4')
                {
                    if (expr[i] != arr[count]) return false;
                    count = (count + 1) % 3;
                }
            }

            if (count != 0) return false;
            try
            {
                rat.eval(expr);
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 数字から式を得る
        /// </summary>
        public string get(int n)
        {
            return hdb.map.ContainsKey(n) ? hdb.map[n] : null;
        }

        /// <summary>
        /// 式をDBに登録する
        /// </summary>
        /// <param name="expr">式</param>
        /// <param name="with_save">保存するか</param>
        /// <param name="force">強制的に変更するか</param>
        /// <returns>計算結果</returns>
        public int? set(string expr, bool with_save = true, bool force = false)
        {
            if (!isValid(expr)) return null;
            var _rat = rat.eval(expr);
            if (_rat.Denominator != 1) return null;
            var n = (int)_rat.Numerator;
            if (!force && hdb.map.ContainsKey(n) && !set_cost(expr, hdb.map[n])) return null;
            hdb.map[n] = expr;
            if (with_save) JsonMgr<HanshinDB>.Save(hdb, filename);
            return n;
        }

        /// <summary>
        /// DBから指定された式を消去する
        /// </summary>
        /// <param name="n">消す数字</param>
        /// <param name="with_save">保存するか (default: true)</param>
        /// <returns>消去できたらTrue</returns>
        public bool delete(int n, bool with_save = true)
        {
            if (hdb.map.ContainsKey(n))
            {
                hdb.map.Remove(n);
                if (with_save) JsonMgr<HanshinDB>.Save(hdb, filename);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// DBの登録数を返す
        /// </summary>
        public int GetCount() => hdb.map.Count;

        /// <summary>
        /// 33-4達成者のリストを取得する
        /// </summary>
        public Dictionary<string, Tuple<int, DateTime>> GetAchievers => hdb.achievers;

        /// <summary>
        /// 33-4達成者を追加する
        /// </summary>
        /// <param name="user">ユーザ名(idではない)</param>
        /// <param name="dt">達成時刻</param>
        public int AddAchiever(string user, DateTime dt)
        {
            if (hdb.achievers.ContainsKey(user))
            {
                var times = hdb.achievers[user].Item1;
                times++;
                hdb.achievers[user] = new Tuple<int, DateTime>(times, dt);
                return times;
            }
            else
            {
                hdb.achievers.Add(user, new Tuple<int, DateTime>(1, dt));
                return 1;
            }
        }

        /// <summary>
        /// どちらがよりよい33-4かを返す
        /// </summary>
        /// <returns>lの方がコストが低かったらTrue</returns>
        private bool set_cost(string l, string r) => l.Length < r.Length || (l.Length == r.Length && l.Count(c => c == '4') < r.Count(c => c == '4'));

        /// <summary>
        /// HanshinDBファイル
        /// </summary>
        private string filename { get; set; }

        /// <summary>
        /// 計算機
        /// </summary>
        private RatEvaluator rat { get; set; }

        /// <summary>
        /// HanshinDB
        /// </summary>
        private HanshinDB hdb { get; set; }
    }
}
