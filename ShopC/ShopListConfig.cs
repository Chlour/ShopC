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

        // 2026-10-05 新增：抽奖配置（权重制 —— 改数字即可调概率，不用重新编译）
        [JsonPropertyName("lottery")]
        public LotteryConfig Lottery { get; set; } = new LotteryConfig();

        public class LotteryConfig
        {
            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; } = false;

            [JsonPropertyName("price")]
            public int Price { get; set; } = 70000;              // 单抽价格（铜）

            [JsonPropertyName("announce")]
            public bool Announce { get; set; } = true;            // 是否播报中奖结果

            [JsonPropertyName("emptyWeight")]
            public double EmptyWeight { get; set; } = 37.33;      // 「空手」权重

            [JsonPropertyName("pools")]
            public List<LotteryEntry> Pools { get; set; } = new List<LotteryEntry>();
        }

        public class LotteryEntry
        {
            [JsonPropertyName("type")]
            public int Type { get; set; } = 2334;

            [JsonPropertyName("weight")]
            public double Weight { get; set; } = 1;
        }

        public class SellsItem
        {
            [JsonPropertyName("type")]
            public int Type { get; set; } = 71;

            [JsonPropertyName("price")]
            public int Price { get; set; } = 50;

            [JsonPropertyName("prefix")]
            public int Prefix { get; set; } = 0;

            // 2026-10-05：困难模式限定（击败血肉墙后才可购买）
            [JsonPropertyName("hardmodeOnly")]
            public bool HardmodeOnly { get; set; } = false;
        }
    }
}
