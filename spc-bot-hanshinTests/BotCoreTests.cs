using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace spc_bot_hanshin.Tests
{
    [TestClass()]
    public class BotCoreTests
    {
        private BotCore core { get; set; }
        private Hanshin hanshin { get; set; }

        public BotCoreTests()
        {
            // 設定ファイルのコピーと読み込み
            var conf = JsonMgr<Config>.Load("config.json");

            core = new BotCore(conf);
            core.Botid = "<BOTSELFID>";
            hanshin = new Hanshin();
        }

        [TestMethod()]
        public void メッセージ全体が阪神算なら計算した値を返す()
        {
            Assert.IsTrue(core.Respond( "3*3*4" ) == "36");
            Assert.IsFalse(core.Respond("hogehoge33-4fugafuga") == "29");
            Assert.IsTrue(core.Respond("36") == "3*3*4");
            Assert.IsFalse(core.Respond("36") == "36");
        }

        [TestMethod()]
        public void dicedice_dice()
        {
            var actual = core.Respond("dicedice-dice");
            var re = System.Text.RegularExpressions.Regex.IsMatch(actual, @"[^⚀⚁⚂⚃⚄⚅\-]");
            Assert.IsTrue(!re, actual);
        }

        [TestMethod()]
        public void 阪神算_文の中に式が含まれている()
        {
            Assert.IsTrue(core.ExprToValue("29") == 29);
            Assert.IsTrue(core.StrToHanshin("あああ29いいい") == "あああ (" + hanshin.get(29) + ") いいい", core.StrToHanshin("あああ29いいい"));
            // 全角
            Assert.IsTrue(core.StrToHanshin("BBBBB８１０AAAAA") == "BBBBB (" + hanshin.get(810) + ") AAAAA", "全角 810");
            Assert.IsTrue(core.StrToHanshin("BBBBB（８１０）AAAAA") == "BBBBB (" + hanshin.get(810) + ") AAAAA", "全角 810" + core.StrToHanshin("BBBBB（８１０）AAAAA"));
        }

        [TestMethod()]
        public void 阪神算_式だけの時()
        {
            Assert.IsTrue(core.StrToHanshin("29") == hanshin.get(29), "29");
            Assert.IsTrue(core.StrToHanshin("-9") == hanshin.get(-9), "-9");
            Assert.IsNull(hanshin.get(-1), hanshin.get(-1));
            Assert.IsTrue(core.StrToHanshin("-1") == ":no_good: -1 :no_good:", "Returns: " + core.StrToHanshin("-1"));
            // 全角
            Assert.IsTrue(core.StrToHanshin("２９") == hanshin.get(29), "全角29");
            Assert.IsTrue(core.StrToHanshin("－９") == hanshin.get(-9), "全角-9" + core.StrToHanshin("－９"));
            Assert.IsTrue(core.StrToHanshin("－１") == ":no_good: -1 :no_good:", "Returns: " + core.StrToHanshin("－１"));
            Assert.IsTrue(core.StrToHanshin("（170）") == hanshin.get(170), "全角 170" + core.StrToHanshin("（170）"));
            // 複数行
            Assert.IsTrue(core.StrToHanshin("29\n1\n17") == $"{hanshin.get(29)}\n{hanshin.get(1)}\n:no_good: 17 :no_good:", "半角　3行 29, 1, 17");
            Assert.IsTrue(core.StrToHanshin("21\n－１\n３３４") == $"{hanshin.get(21)}\n:no_good: -1 :no_good:\n{hanshin.get(334)}", "全半混じり　3行 21, -1, 334");
            Assert.IsTrue(core.StrToHanshin("あああいいいうううえええおおお\n-1\n３３４") == $"\n:no_good: -1 :no_good:\n{hanshin.get(334)}", "全半混じり　3行 (含まない文), -1, 334");
        }

        [TestMethod()]
        public void 阪神算_式でない場合はnull()
        {
            Assert.IsNull(core.StrToHanshin("なんでや！式ちゃうやろ！"), "Returns: " + core.StrToHanshin("なんでや！式ちゃうやろ！"));
        }

        [TestMethod()]
        public void な阪関を返す()
        {
            Assert.IsTrue(core.Respond("33-4") == "なんでや！阪神関係ないやろ！");
            Assert.IsTrue(core.Respond(" 33-4\n") == "なんでや！阪神関係ないやろ！");
            Assert.IsFalse(core.Respond("ABCD33-4EFGH") == "なんでや！阪神関係ないやろ！");
        }

    }
}