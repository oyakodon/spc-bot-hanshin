using SlackAPI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace spc_bot_hanshin
{
    public class SlackBot
    {
        /// <summary>
        /// Slack
        /// </summary>
        public SlackBot(string filename = "config.json")
        {
            if (System.IO.File.Exists(filename))
            {
                conf = JsonMgr<Config>.Load(filename);
            } else
            {
                var _conf = new Config();
                _conf.token = "<YOUR BOT TOKEN HERE>";
                _conf.next_procon = DateTime.MinValue;
                _conf.procon_notice_time = 0;
                _conf.manager_ids = new List<string>() { "<MANAGER USERNAME HERE>" };
                _conf.channels = new List<string>() { "<CHANNEL HERE (without #)>" };
                _conf.procon_notice_channels = new List<string>() { "<PROCON NOTICE CHANNEL HERE (without #)>" };

                JsonMgr<Config>.Save(_conf, filename);
                throw new Exception(string.Format("{0}を設定してください。", filename));
            }

            client = new SlackSocketClient(conf.token);

            channel_ids = new List<string>();
            core = new BotCore(conf);
            IsConnected = false;

            // プロコン残り日数表通知用
            timer_procon = new System.Timers.Timer();
            timer_procon.Interval = 1000;
            timer_procon.Elapsed += Timer_procon_Elapsed;
        }

        /// <summary>
        /// BOTの実行
        /// </summary>
        public void Run()
        {
            var clientReady = new ManualResetEventSlim(false);
            Console.WriteLine("Slackへの接続を開始します。");

            client.Connect((connected) =>
            {
                clientReady.Set();

                client.GetChannelList(callback =>
                {
                    client.Channels.ForEach(channel => { if (conf.channels.Contains(channel.name)) channel_ids.Add(channel.id); });
                });
                client.GetUserList(null);
                timer_procon.Start();
                core.Botid = client.MySelf.id;

                IsConnected = true;

                Console.WriteLine("起動しました。 " + DateTime.Now);
            });

            // メッセージ受信時に呼び出される
            client.OnMessageReceived += (message) =>
            {
                Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}", message.text, message.channel, message.user, message.type, message.ts, message.subtype, message.reply_to);

                var isDM = client.Channels.Find(channel => channel.id == message.channel) == null; // DMチャンネルかどうか

                if (message.user == client.MySelf.id) return; // 自分
                if (!channel_ids.Contains(message.channel) && !isDM) return; // 返信するチャンネルではなく、DMじゃない
                if (core.launched > message.ts) return; // 起動時刻より前のメッセージ

                // BOTへのメンション
                if (message.text.Contains(string.Format("<@{0}>", client.MySelf.id)))
                {
                    var user = client.Users.Find(u => u.id == message.user);
                    var isMgr = user != null && conf.manager_ids.Contains(user.name);
                    var res_cmd = "<@" + client.MySelf.id + ">\n" + core.ExecuteCmd(message.text.Replace($"<{client.MySelf.id}>", "").Trim(), isDM, isMgr);

                    Console.WriteLine("[ExeCmd] responce => " + res_cmd);
                    client.SendMessage(null, message.channel, res_cmd);
                    return;
                }

                var res = core.Respond(message.text);
                client.SendMessage(null, message.channel, res);

                // 33-4 達成判定
                if (message.text == "dicedice-dice" && res == ("⚂⚂-⚃") && !isDM)
                {
                    var user = client.Users.Find(u => u.id == message.user);
                    res = core.CelebrateHanshin(user.name);

                    Console.WriteLine("[Achiever] responce => " + res);
                    client.SendMessage(null, message.channel, res);
                }

            };

            clientReady.Wait(15 * 1000); // 接続待ち
            if (IsConnected) clientReady.Reset();
            else throw new Exception("接続に失敗しました。tokenを確認してください。");

            clientReady.Wait();
        }

        /// <summary>
        /// プロコン通知用タイマーイベント (1秒毎)
        /// </summary>
        private void Timer_procon_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var res = core.NoticeRemainingProcon();
            if (client.IsConnected && res != null)
            {
                foreach (var channel in conf.procon_notice_channels)
                {
                    var _id = client.Channels.Find(c => c.name == channel).id;
                    client.SendMessage(callback => { Console.WriteLine("[Procon] responce => " + res); }, _id, res);
                }
            }
        }

        /// <summary>
        /// BotCore
        /// </summary>
        private static BotCore core { get; set; }

        /// <summary>
        /// 接続済みかどうか
        /// </summary>
        private static bool IsConnected { get; set; }

        /// <summary>
        /// 返事をするチャンネル一覧
        /// </summary>
        private static List<string> channel_ids { get; set; }

        /// <summary>
        /// プロコンの残り日数表通知用タイマー
        /// </summary>
        private static System.Timers.Timer timer_procon { get; set; }

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
