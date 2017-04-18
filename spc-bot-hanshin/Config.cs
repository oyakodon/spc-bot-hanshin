using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace spc_bot_hanshin
{
    /// <summary>
    /// 設定
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Slackトークン
        /// </summary>
        [JsonProperty("token")]
        public string token { get; set; }

        /// <summary>
        /// 返信するチャンネル (チャンネル名, #なし)
        /// </summary>
        [JsonProperty("channels")]
        public List<string> channels { get; set; }

        /// <summary>
        /// 権限のあるユーザ (ユーザ名)
        /// </summary>
        [JsonProperty("manager_ids")]
        public List<string> manager_ids { get; set; }

        /// <summary>
        /// 次回のプロコン
        /// </summary>
        [JsonProperty("next_procon")]
        public DateTime next_procon { get; set; }

        /// <summary>
        /// 残り日数を通知するチャンネル　(チャンネル名, #なし)
        /// </summary>
        [JsonProperty("procon_notice_channels")]
        public List<string> procon_notice_channels { get; set; }

        /// <summary>
        /// 何時に通知するか
        /// </summary>
        [JsonProperty("procon_notice_time")]
        public int procon_notice_time { get; set; }

    }
}
