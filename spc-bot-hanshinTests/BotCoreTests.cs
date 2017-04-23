using Microsoft.VisualStudio.TestTools.UnitTesting;
using spc_bot_hanshin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spc_bot_hanshin.Tests
{
    [TestClass()]
    public class BotCoreTests
    {
        private BotCore core { get; set; }

        public BotCoreTests()
        {
            // 設定ファイルのコピーと読み込み
            System.IO.File.Copy("../", "config.json", true);
            var conf = JsonMgr<Config>.Load("config.json");

            core = new BotCore(conf);
            core.Botid = "<BOTSELFID>";
        }

        [TestMethod()]
        public void 阪神算_33_4()
        {
            Assert.IsTrue(core.Respond("33-4") == "なんでや！阪神関係ないやろ！");
            Assert.IsTrue(core.Respond(" 33-4\n") == "なんでや！阪神関係ないやろ！");
            Assert.IsFalse(core.Respond("ABCD33-4EFGH") == "なんでや！阪神関係ないやろ！");
        }



    }
}