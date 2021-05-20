using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TShockAPI;
using TShockAPI.Hooks;

namespace ShopC
{
    public class ShopListConfig
    {
        public void Load(ReloadEventArgs args = null)
        {
            if (!File.Exists(Path.Combine(TShock.SavePath, "ShopList.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                TextReader tr = new StringReader(JsonConvert.SerializeObject(ShopC.ShopListConfig));
                JsonTextReader jtr = new JsonTextReader(tr);
                object obj = serializer.Deserialize(jtr);
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,//格式化缩进
                    Indentation = 4,  //缩进四个字符
                    IndentChar = ' '  //缩进的字符是空格
                };
                serializer.Serialize(jsonWriter, obj);
                FileTools.CreateIfNot(Path.Combine(TShock.SavePath, "ShopList.json"), textWriter.ToString());
            }
            try
            {
                ShopC.ShopListConfig = JsonConvert.DeserializeObject<ShopListConfig>(File.ReadAllText(Path.Combine(TShock.SavePath, "ShopList.json")));
                TShock.Log.ConsoleInfo($"<ShopC> 成功读取商店列表文件.");
            }
            catch (Exception ex) { TShock.Log.Error(ex.Message); TShock.Log.ConsoleError("[C/66D093:<ShopC>] 读取配商店列表文件失败."); }
        }
        [JsonProperty]
        public string helptext = "列表示例：[{\"type\":\"71\",\"price\":\"50\",\"prefix\":\"0\"},{\"type\":\"72\",\"price\":\"100\",\"prefix\":\"0\"}]";
        public class SellsItem
        {
            
            public int type= 71;
            public int price = 50;
            public int prefix = 0;
        }
        [JsonProperty]
        public List<SellsItem> Shoplist = new List<SellsItem>();
       
    }
}