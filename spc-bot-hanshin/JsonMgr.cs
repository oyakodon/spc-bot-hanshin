using Newtonsoft.Json;
using System.IO;

namespace spc_bot_hanshin
{
    public static class JsonMgr<T>
    {
        /// <summary>
        /// jsonファイルを読み込みます。
        /// </summary>
        /// <param name="filename">ファイル名</param>
        public static T Load(string filename)
        {
            T obj = default(T);

            if (File.Exists(filename))
            {
                // 存在しているのでロード
                using (var _sr = new StreamReader(filename, System.Text.Encoding.UTF8))
                {
                    var json = _sr.ReadToEnd();
                    obj = JsonConvert.DeserializeObject<T>(json);
                }
            }
            else
            {
                throw new FileNotFoundException("jsonファイルが存在しません");
            }

            return obj;
        }

        /// <summary>
        /// jsonファイルに書き込みます。もしファイルが存在しない場合は、新しく作成します。
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <param name="filename">ファイル名</param>
        public static void Save(T obj, string filename)
        {
            using (var _sw = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
            {
                var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                _sw.Write(json);
            }
        }

    }
}