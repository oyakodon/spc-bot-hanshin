using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace spc_bot_hanshin
{
    public class BotCore
    {
        public BotCore(Config _conf)
        {
            conf = _conf;

            hanshin = new Hanshin();
            rat = new RatEvaluator();
            rnd = new Random(Environment.TickCount + 334);
            launched = DateTime.Now;

            sent_procon = DateTime.Now.AddDays(-1);
        }

        public string Respond(string msg)
        {
            // 33-4
            if (msg.Trim() == "33-4")
            {
                Console.WriteLine("responce => なんでや！阪神関係ないやろ！");
                return "なんでや！阪神関係ないやろ！";
            }

            var res = "";

            // FizzBuzz
            var m = Regex.Match(msg, @"fizzbuzz ([0-9\+\-\*\/\(\)]+)");
            if (m.Success)
            {
                var _v = ExprToValue(m.Value.Split()[1]);
                if (_v.HasValue)
                {
                    int v = _v.Value > 100 ? 100 : _v.Value;
                    res = "1";
                    Enumerable.Range(2, v - 1).ToList().ForEach(n => res += n % 3 == 0 ? (n % 5 == 0 ? " FizzBuzz" : " Fizz") : (n % 5 == 0 ? " Buzz" : " " + n));

                    Console.WriteLine("[FizzBuzz] responce => " + res);
                    return StrToHanshin(res);
                }
            }

            var _res = StrToDice(msg);
            res = StrToHanshin(_res);
            res = string.IsNullOrEmpty(res) && _res != msg ? _res : res;

            if (string.IsNullOrEmpty(res))
            {
                // 日時
                if (msg.Contains("時") || msg.Contains("日"))
                {
                    var tzi = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                    var jst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Now.ToUniversalTime(), tzi);
                    var str = jst.ToString("yyyy年M月d日 H時m分s秒");
                    str = StrToHanshin(str);
                    Console.WriteLine("[DateTime] responce => " + str);
                    return str;
                }
            }
            else
            {
                return res;
            }

            return null;
        }

        public string CelebrateHanshin (string user)
        {
            var res = "";
            var times = hanshin.AddAchiever(user, DateTime.Now);
            if (times == 1)
            {
                res = $"\nなんでや！阪神関係ないやろ！\nおめでとうございます！あなたは {hanshin.GetAchievers.Count} 人目の33-4達成者です！";
            }
            else
            {
                res = $"なんでや！阪神関係ないやろ！\nおめでとうございます！{times} 回目の33-4達成です！";
            }
            res = StrToHanshin(res);

            return res;
        }

        public string NoticeRemainingProcon ()
        {
            string res = null;

            if (DateTime.Now.Date > sent_procon.Date && DateTime.Now >= DateTime.Today.AddHours(conf.procon_notice_time))
            {
                var days = (conf.next_procon.Date - DateTime.Now.Date).Days;
                if (days >= -1)
                {
                    if (days == 0)
                    {
                        var v = string.IsNullOrEmpty(hanshin.get(1)) ? ":no_good: 1 :no_good:" : hanshin.get(1);
                        res = $"プロコン {v} 日目です！頑張ってください！";
                    }
                    else if (days == -1)
                    {
                        var v = string.IsNullOrEmpty(hanshin.get(2)) ? ":no_good: 2 :no_good:" : hanshin.get(2);
                        res = $"プロコン {v} 日目です！頑張ってください！";
                    }
                    else
                    {
                        var v = string.IsNullOrEmpty(hanshin.get(days)) ? $":no_good: {days} :no_good:" : hanshin.get(days);
                        res = $"プロコンまであと {v} 日です。";
                    }
                }

                if (res != null)
                {
                    res = StrToHanshin(res);
                    sent_procon = DateTime.Now;
                }
            }

            return res;
        }

        /// <summary>
        /// コマンドを実行して出力を返す
        /// </summary>
        public string ExecuteCmd(string _cmd, bool isDM, bool isMgr)
        {
            var res = "";
            var cmd = _cmd.Split();
            int n;

            switch (cmd[0])
            {
                case "help":
                    res += $"[Help]\nUsage: @{Botid} <Command>\nCommands:\ninfo - 各種情報を表示します。\nhelp - ヘルプを表示します。\n<i>他にも隠しコマンドがあるかも?</i>\n";
                    break;

                case "info":
                    var dbcount = "DB Count: " + hanshin.GetCount();
                    var _ut = DateTime.Now - launched;
                    var uptime = string.Format("Uptime: {0:D2}:{1:D2}:{2:D2}", (int)_ut.TotalHours, _ut.Hours, _ut.Minutes);

                    res += $"[Info]\n{dbcount}\n{uptime}";
                    if (isMgr && isDM) res += "\nYou are manager.";
                    break;

                case "achiever":
                    if (cmd.Count() == 2)
                    {
                        if (hanshin.GetAchievers.ContainsKey(cmd[1]))
                        {
                            var achiever = hanshin.GetAchievers[cmd[1]];
                            res += $"[Achiever]\n{cmd[1]} 回数: {achiever.Item1} 回 日時: {achiever.Item2.ToString()}\n";
                        }
                        else
                        {
                            res += "Error: user is not found or not achieved yet.";
                        }
                    }
                    else if (cmd.Count() == 1)
                    {
                        Tuple<int, DateTime> latest = null;
                        var latest_user = "";
                        foreach (var a in hanshin.GetAchievers)
                        {
                            if (latest == null || latest.Item2 < a.Value.Item2)
                            {
                                latest = a.Value;
                                latest_user = a.Key;
                            }
                        }
                        res += $"[Achiever]\nCount: {hanshin.GetAchievers.Count}\nLatest: {latest_user} 回数:{latest.Item1}回 日時: {latest.Item2.ToString()}\n";
                    }
                    else
                    {
                        res += "Error: Invalid argument.";
                    }
                    break;

                case "delete":
                    if (isMgr && isDM)
                    {
                        if (cmd.Count() != 2)
                        {
                            res += "Error: Invalid argument.";
                            break;
                        }

                        if (int.TryParse(cmd[1], out n))
                        {
                            res += hanshin.delete(n) ? n + " is successfully deleted." : "key is not found.";
                        }
                    }
                    else
                    {
                        res += isDM ? "You have no permission to execute " + cmd[0] + "." : "You must run the command in DM.";
                    }
                    break;

                case "force":
                    if (isMgr && isDM)
                    {
                        if (cmd.Count() != 2)
                        {
                            res += "Error: Invalid argument.";
                            break;
                        }

                        var v = hanshin.set(cmd[1], true, true);
                        res += v.HasValue ? string.Format("{0} => {1}", v.Value, cmd[1]) : "cannot set \"" + cmd[1] + "\"";
                    }
                    else
                    {
                        res += isDM ? "You have no permission to execute " + cmd[0] + "." : "You must run the command in DM.";
                    }
                    break;

                default:
                    res += (cmd.Count() == 0 ? "" : cmd[0]) + " : command not found.";
                    break;
            }

            return res;
        }

        /// <summary>
        /// 式を値にして返す
        /// </summary>
        public int? ExprToValue(string expr)
        {
            hanshin.set(expr);
            Rational v = null;
            try
            {
                v = rat.eval(expr);
            }
            catch (FormatException)
            {
                return null;
            }
            if (v.Denominator != 1) return null;
            return (int)v.Numerator;
        }

        /// <summary>
        /// 式を阪神算して返す
        /// </summary>
        public string StrToHanshin(string str)
        {
            var list = str.Split('\n');
            if (list.Count() >= 2)
            {
                return string.Join("\n", list.Select(s => StrToHanshin(s)));
            }

            for (var i = 0; i < zenkaku_tbl.Count(); i++)
            {
                str = str.Replace(zenkaku_tbl[i], hankaku_tbl[i]);
            }

            var re = Regex.Matches(str, @"([^0-9\+\-\*\/\(\)]+|[0-9\+\-\*\/\(\)]+)");

            if (re.Count == 0) return null;
            var res = "";
            foreach (Match m in re)
            {
                if (Regex.IsMatch(m.Value, @"[0-9\+\-\*\/\(\)]+"))
                {
                    var v = ExprToValue(m.Value);
                    if (v == null) return null;
                    var expr = hanshin.get(v.Value);
                    if (re.Count != 1) res += " (";
                    if (expr == null)
                    {
                        res += string.Format(":no_good: {0} :no_good:", m.Value);
                    }
                    else
                    {
                        res += expr;
                    }
                    if (re.Count != 1) res += ") ";

                }
                else
                {
                    res += m.Value;
                }
            }

            return res;
        }

        /// <summary>
        /// diceをサイコロにして返す
        /// </summary>
        public string StrToDice(string str)
        {
            var re = Regex.Split(str, @"(:daisuke:|:dicek:|サイコロ|[dD][iI][cC][eE])");

            for (var i = 0; i < re.Count(); i++)
            {
                if (re[i] == ":daisuke:") re[i] = ":dicek:";
                else if (re[i] == ":dicek:") re[i] = ":daisuke:";
                else if (Regex.IsMatch(re[i], @"サイコロ|[dD][iI][cC][eE]"))
                {
                    re[i] = dice[rnd.Next(6)] + "";
                }
            }

            var res = "";
            foreach (var s in re) res += s;

            return res;
        }
        
        /// <summary>
        /// ボットの名前
        /// </summary>
        public string Botid { get; set; }

        /// <summary>
        /// 起動時刻
        /// </summary>
        public DateTime launched { get; set; }

        /// <summary>
        /// 全角文字対応テーブル
        /// </summary>
        private char[] zenkaku_tbl = { '１', '２', '３', '４', '５', '６', '７', '８', '９', '＋', 'ー', '×', '÷', '＊' };

        /// <summary>
        /// 半角文字対応テーブル
        /// </summary>
        private char[] hankaku_tbl = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '-', '*', '/', '*' };

        /// <summary>
        /// サイコロ
        /// </summary>
        private char[] dice = { '⚀', '⚁', '⚂', '⚃', '⚄', '⚅' };

        /// <summary>
        /// 33-4
        /// </summary>
        private Hanshin hanshin { get; set; }

        /// <summary>
        /// 計算機
        /// </summary>
        private RatEvaluator rat { get; set; }

        /// <summary>
        /// 最後に残り日数を通知した時刻
        /// </summary>
        private static DateTime sent_procon { get; set; }

        /// <summary>
        /// 乱数
        /// </summary>
        private static Random rnd { get; set; }

        /// <summary>
        /// 設定
        /// </summary>
        private static Config conf { get; set; }

    }
}
