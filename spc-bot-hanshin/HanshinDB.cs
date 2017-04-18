using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace spc_bot_hanshin
{
    /// <summary>
    /// HanshinDB
    /// </summary>
    public class HanshinDB
    {
        /// <summary>
        /// 阪神算の式と結果
        /// </summary>
        [JsonProperty("map")]
        public Dictionary<int, string> map { get; set; }

        /// <summary>
        /// 33-4達成者
        /// </summary>
        [JsonProperty("achievers")]
        public Dictionary<string, Tuple<int, DateTime>> achievers { get; set; }

    }
}
