using SlackAPI;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;

namespace spc_bot_hanshin
{
    public class SlackBot
    {
        /// <summary>
        /// Slack
        /// </summary>
        public SlackBot(string filename = "config.json")
        {
            conf = JsonMgr<Config>.Load(filename);
            client = new SlackSocketClient(conf.token);

            hanshin = new Hanshin();
            rat = new RatEvaluator();
            rnd = new Random(Environment.TickCount + 334);
            channel_ids = new List<string>();

            // プロコン残り日数表通知用
            timer_procon = new System.Timers.Timer();
            timer_procon.Interval = 1000;
            sent_procon = DateTime.Now.AddDays(-1);
            timer_procon.Elapsed += Timer_procon_Elapsed;
        }

        /// <summary>
        /// BOTの実行
        /// </summary>
        public void Run()
        {
            var clientReady = new ManualResetEventSlim(false);
            launched = DateTime.Now;

            client.Connect((connected) =>
            {
                clientReady.Set();

                // チャンネル名 -> チャンネルid
                client.GetChannelList(callback =>
                {
                    client.Channels.ForEach(channel => { if (conf.channels.Contains(channel.name)) channel_ids.Add(channel.id); });
                });

                client.GetUserList(null);

                timer_procon.Start();

                Console.WriteLine("Bot launched.");
            });

            // メッセージ受信時に呼び出される
            client.OnMessageReceived += (message) =>
            {
                Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}", message.text, message.channel, message.user, message.type, message.ts, message.subtype, message.reply_to);

                var isDM = client.Channels.Find(channel => channel.id == message.channel) == null; // DMチャンネルかどうか

                if (message.user == client.MySelf.id) return; // 自分
                if (!channel_ids.Contains(message.channel) && !isDM) return; // 返信するチャンネルではなく、DMじゃない
                if (launched > message.ts) return; // 起動時刻より前のメッセージ

                // BOTへのメンション
                if (message.text.Contains(string.Format("<@{0}>", client.MySelf.id)))
                {
                    Console.WriteLine("Mention to me!");
                    var res_cmd = ExecuteCmd(message.text, message.user, message.channel);
                    client.SendMessage(callback => { Console.WriteLine("[ExeCmd] responce => " + res_cmd); }, message.channel, res_cmd);
                    return;
                }

                // 33-4
                if (message.text.Trim() == "33-4")
                {
                    client.SendMessage(callback => { Console.WriteLine("responce => なんでや！阪神関係ないやろ！"); }, message.channel, "なんでや！阪神関係ないやろ！");
                    return;
                }

                var res = "";
                // FizzBuzz
                var m = Regex.Match(message.text, @"fizzbuzz ([0-9\+\-\*\/\(\)]+)");
                if (m.Success)
                {
                    var _v = ExprToValue(m.Value.Split()[1]);
                    if (_v.HasValue)
                    {
                        int v = _v.Value > 100 ? 100 : _v.Value;
                        res = "1";
                        Enumerable.Range(2, v - 1).ToList().ForEach(n => res += n % 3 == 0 ? (n % 5 == 0 ? " FizzBuzz" : " Fizz") : (n % 5 == 0 ? " Buzz" : " " + n));
                        res = StrToHanshin(res);
                        client.SendMessage(callback => { Console.WriteLine("[FizzBuzz] responce => " + res); }, message.channel, res);
                        return;
                    }
                }

                var _res = StrToDice(message.text);
                res = StrToHanshin(_res);
                res = string.IsNullOrEmpty(res) && _res != message.text ? _res : res;

                if (string.IsNullOrEmpty(res))
                {
                    // 日時
                    if (message.text.Contains("時") || message.text.Contains("日"))
                    {
                        var tzi = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                        var jst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Now.ToUniversalTime(), tzi);
                        var str = jst.ToString("yyyy年M月d日 H時m分s秒");
                        str = StrToHanshin(str);
                        client.SendMessage(callback => { Console.WriteLine("[DateTime] responce => " + str); }, message.channel, str);
                    }
                }
                else
                {
                    client.SendMessage(callback => { Console.WriteLine("responce => " + res); }, message.channel, res);

                    // 33-4 達成判定
                    if (res.Contains("⚂⚂-⚃") && !isDM)
                    {
                        var user = client.Users.Find(u => u.id == message.user);
                        var times = hanshin.AddAchiever(user.name, DateTime.Now);
                        if (times == 1)
                        {
                            res = $"なんでや！阪神関係ないやろ！\nおめでとうございます！あなたは {hanshin.GetAchievers.Count} 人目の33-4達成者です！";
                        }
                        else
                        {
                            res = $"なんでや！阪神関係ないやろ！\nおめでとうございます！{times} 回目の33-4達成です！";
                        }
                        res = StrToHanshin(res);

                        client.SendMessage(callback => { Console.WriteLine("[Achiever] responce => " + res); }, message.channel, res);
                    }
                }
            };

            clientReady.Wait();
        }

        /// <summary>
        /// 式を値にして返す
        /// </summary>
        private int? ExprToValue(string expr)
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
        private string StrToHanshin(string str)
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
        private string StrToDice(string str)
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
        /// コマンドを実行して出力を返す
        /// </summary>
        private string ExecuteCmd(string str, string _user, string _channel)
        {
            var user = client.Users.Find(u => u.id == _user);
            var isMgr = user != null && conf.manager_ids.Contains(user.name);
            var isDM = client.Channels.Find(c => c.id == _channel) == null;
            var cmd = str.Replace("<@" + client.MySelf.id + ">", "").Trim().Split();
            var res = "<@" + client.MySelf.id + ">\n";
            int n;

            switch (cmd[0])
            {
                case "help":
                    res += $"[Help]\nUsage: @{client.MySelf.name} <Command>\nCommands:\ninfo - 各種情報を表示します。\nhelp - ヘルプを表示します。\n<i>他にも隠しコマンドがあるかも?</i>\n";
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
        /// プロコン通知用タイマーイベント (1秒毎)
        /// </summary>
        private void Timer_procon_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DateTime.Now.Date > sent_procon.Date && DateTime.Now >= DateTime.Today.AddHours(conf.procon_notice_time))
            {
                var days = (conf.next_procon.Date - DateTime.Now.Date).Days;
                string res = null;
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

                if (res != null && client.IsConnected)
                {
                    res = StrToHanshin(res);
                    foreach (var channel in conf.procon_notice_channels)
                    {
                        var _id = client.Channels.Find(c => c.name == channel).id;
                        client.SendMessage(callback => { Console.WriteLine("[Procon] responce => " + res); }, _id, res);
                    }

                    sent_procon = DateTime.Now;
                }
            }
        }

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
        /// 返事をするチャンネル一覧
        /// </summary>
        private List<string> channel_ids { get; set; }

        /// <summary>
        /// プロコンの残り日数表通知用タイマー
        /// </summary>
        private System.Timers.Timer timer_procon { get; set; }

        /// <summary>
        /// 最後に残り日数を通知した時刻
        /// </summary>
        private static DateTime sent_procon { get; set; }

        /// <summary>
        /// 起動時刻
        /// </summary>
        private static DateTime launched { get; set; }

        /// <summary>
        /// 乱数
        /// </summary>
        private static Random rnd { get; set; }

        /// <summary>
        /// Slackクライアント
        /// </summary>
        private static SlackSocketClient client { get; set; }

        /// <summary>
        /// 設定
        /// </summary>
        private static Config conf { get; set; }

    }
}
