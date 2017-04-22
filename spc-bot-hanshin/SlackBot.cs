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
            conf = JsonMgr<Config>.Load(filename);
            client = new SlackSocketClient(conf.token);
            channel_ids = new List<string>();
            core = new BotCore(conf);

            core.Botid = client.MySelf.id;

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

            clientReady.Wait();
        }

        /// <summary>
        /// プロコン通知用タイマーイベント (1秒毎)
        /// </summary>
        private void Timer_procon_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var res = core.NoticeRemainingProcon();
            if (client.IsConnected && res == null)
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
        private BotCore core { get; set; }

        /// <summary>
        /// 返事をするチャンネル一覧
        /// </summary>
        private List<string> channel_ids { get; set; }

        /// <summary>
        /// プロコンの残り日数表通知用タイマー
        /// </summary>
        private System.Timers.Timer timer_procon { get; set; }

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
