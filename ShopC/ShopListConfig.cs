using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TShockAPI;
using TShockAPI.Hooks;

namespace ShopC
{
    public class ShopListConfig
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            // 中文 helptext 不转义
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public void Load(ReloadEventArgs args = null)
        {
            var path = Path.Combine(TShock.SavePath, "ShopList.json");
            try
            {
                if (!File.Exists(path))
                {
                    FileTools.CreateIfNot(path, JsonSerializer.Serialize(this, JsonOptions));
                }

                var loaded = JsonSerializer.Deserialize<ShopListConfig>(File.ReadAllText(path), JsonOptions);
                if (loaded != null)
                {
                    ShopC.ShopListConfig = loaded;
                }
                TShock.Log.ConsoleInfo("<ShopC> 成功读取商店列表文件.");
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                TShock.Log.ConsoleError("<ShopC> 读取商店列表文件失败.");
            }
        }

        [JsonPropertyName("helptext")]
        public string Helptext { get; set; } = "原版匣子抽奖商店：用原版货币购买匣子。用法 /shopc <物品ID> <数量>，price 单位为铜币（1金=10000，1铂金=1000000）。";

        [JsonPropertyName("shoplist")]
        public List<SellsItem> Shoplist { get; set; } = new List<SellsItem>();

        public class SellsItem
        {
            [JsonPropertyName("type")]
            public int Type { get; set; } = 71;

            [JsonPropertyName("price")]
            public int Price { get; set; } = 50;

            [JsonPropertyName("prefix")]
            public int Prefix { get; set; } = 0;
        }
    }
}
