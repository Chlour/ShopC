//代码来源：https://github.com/chi-rei-den/PluginTemplate/blob/master/src/PluginTemplate/Program.cs

using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace ShopC
{
    [ApiVersion(2, 1)]
    public class ShopC : TerrariaPlugin
    {
        //定义插件的作者名称
        public override string Author => "Chlour";

        //插件的一句话描述
        public override string Description => "基于原版货币的商店";

        public static ShopListConfig ShopListConfig = new ShopListConfig();

        //插件的名称
        public override string Name => "ShopC";

        //插件的版本
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        //插件的构造器
        public ShopC(Main game) : base(game)
        {
        }

        //插件加载时执行的代码
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(
                permissions: new List<string> { "chest.shop" },
                cmd: this.Cmd,
                "shopc", "hw"));

            // /reload 时重新读取商店列表
            GeneralHooks.ReloadEvent += ShopListConfig.Load;

            ShopListConfig.Load();
        }

// 2026-10-05：铜币数 → 可读货币（铂/金/银/铜），让 /shopc list 好看些
        private static string FormatCoins(long copper)
        {
            if (copper <= 0) return "0 铜";
            long p = copper / 1000000;          // 铂金
            long g = (copper / 10000) % 100;    // 金
            long s = (copper / 100) % 100;      // 银
            long c = copper % 100;              // 铜
            var sb = new System.Text.StringBuilder();
            if (p > 0) sb.Append(p).Append(" 铂金 ");
            if (g > 0) sb.Append(g).Append(" 金 ");
            if (s > 0) sb.Append(s).Append(" 银 ");
            if (c > 0) sb.Append(c).Append(" 铜");
            return sb.ToString().TrimEnd();
        }

                // 权重抽取：命中返回物品 ID，落在空手区返回 0
        private static int RollLottery(ShopListConfig.LotteryConfig lot, Random rnd)
        {
            double total = Math.Max(0, lot.EmptyWeight);
            foreach (var e in lot.Pools) if (e != null && e.Weight > 0) total += e.Weight;
            if (total <= 0) return 0;

            double roll = rnd.NextDouble() * total;
            foreach (var e in lot.Pools)
            {
                if (e == null || e.Weight <= 0) continue;
                if (roll < e.Weight) return e.Type;
                roll -= e.Weight;
            }
            return 0;
        }

                //执行指令时对指令进行处理的方法
        private void Cmd(CommandArgs args)
        {
            //无参数：显示帮助（原版此处 args.Parameters[0] 会越界崩溃）
            if (args.Parameters.Count == 0 || args.Parameters[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                args.Player.SendSuccessMessage("欢迎光临! 用法: /shopc <物品ID> <数量> | /shopc list | /shopc luck [次数]");
                args.Player.SendSuccessMessage($"本店共有 {ShopListConfig.Shoplist.Count} 种商品，购买时扣除原版货币。");
                return;
            }

            //列出全部商品
            if (args.Parameters[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                args.Player.SendSuccessMessage("=== 商品列表 (ID / 单价:铜币) ===");
                foreach (var item in ShopListConfig.Shoplist)
                {
                    var it = new Item();
                    it.SetDefaults(item.Type);
                    args.Player.SendInfoMessage($"{(item.HardmodeOnly ? "[困难] " : "")}[{item.Type}] {it.Name} - {FormatCoins(item.Price)}" + (item.HardmodeOnly && !Main.hardMode ? "  （肉山后开放）" : ""));
                }
                return;
            }

            //参数解析（原版无参数 / 非数字会崩溃）
            // 抽奖：/shopc luck [次数]
            if (args.Parameters[0].Equals("luck", StringComparison.OrdinalIgnoreCase))
            {
                var lot = ShopListConfig.Lottery;
                if (lot == null || !lot.Enabled)
                {
                    args.Player.SendErrorMessage("抽奖暂未开放。");
                    return;
                }

                int draws = 1;                                    // 不传次数默认抽 1 次（原版会越界崩溃）
                if (args.Parameters.Count >= 2 && !int.TryParse(args.Parameters[1], out draws))
                {
                    args.Player.SendErrorMessage("参数错误：用法 /shopc luck [次数]，例如 /shopc luck 10");
                    return;
                }
                if (draws <= 0)
                {
                    args.Player.SendErrorMessage("参数错误：次数必须是正整数。");
                    return;
                }
                if (draws > 100) draws = 100;                     // 单次上限保护

                long cost = (long)lot.Price * draws;
                if (cost > int.MaxValue)
                {
                    args.Player.SendErrorMessage("金额过大，请减少次数。");
                    return;
                }

                int[] pay = args.Player.TakeMoneyFromPlayer((int)cost);
                if (pay[0] == 1)
                {
                    args.Player.SendErrorMessage($"抽奖失败：{draws} 次需要 {FormatCoins(cost)}，你的货币不足。");
                    return;
                }

                var rnd = new Random();
                var got = new Dictionary<int, int>();
                int emptyCount = 0;
                for (int i = 0; i < draws; i++)                   // 逐次发放（原版只发最后一次）
                {
                    int t = RollLottery(lot, rnd);
                    if (t <= 0) { emptyCount++; continue; }
                    args.Player.GiveItemEX(t, 1, 0);
                    got[t] = got.TryGetValue(t, out int c) ? c + 1 : 1;
                }

                if (lot.Announce)
                {
                    if (draws == 1)
                    {
                        if (got.Count == 0) args.Player.SendInfoMessage("💨 很遗憾，空手而归……");
                        else foreach (var kv in got)
                        {
                            var it = new Item(); it.SetDefaults(kv.Key);
                            args.Player.SendSuccessMessage($"🎉 恭喜抽中：{it.Name}！");
                        }
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append($"🎁 抽奖 ×{draws} 结果：");
                        foreach (var kv in got)
                        {
                            var it = new Item(); it.SetDefaults(kv.Key);
                            sb.Append($"{it.Name} ×{kv.Value}、");
                        }
                        sb.Append($"空手 ×{emptyCount}");
                        args.Player.SendSuccessMessage(sb.ToString());
                    }
                }
                else if (got.Count == 0)
                {
                    args.Player.SendInfoMessage("💨 空手而归");
                }
                return;
            }

            if (!int.TryParse(args.Parameters[0], out int type) || type <= 0)
            {
                args.Player.SendErrorMessage("参数错误：请输入物品 ID。用法: /shopc <物品ID> <数量>");
                return;
            }

            int num = 1;
            if (args.Parameters.Count >= 2 && (!int.TryParse(args.Parameters[1], out num) || num <= 0))
            {
                args.Player.SendErrorMessage("参数错误：数量必须是正整数。");
                return;
            }

            // 困难模式门槛：肉山前买不了困难模式匣子
var entry = ShopListConfig.Shoplist.Find(x => x.Type == type && x.Prefix == 0);
if (entry != null && entry.HardmodeOnly && !Main.hardMode)
{
    args.Player.SendErrorMessage("该商品为困难模式专属：击败血肉墙（肉山）后才会开放。");
    return;
}
int[] result = args.Player.BuyItemC(ShopListConfig, type, num, 0);
            if (result[0] == 0)
            {
                string s = "购买成功!共花费";

                if (result[1] != 0)
                {
                    s = s + result[1] + "铂金币，";
                }

                if (result[2] != 0)
                {
                    s = s + result[2] + "金币，";
                }
                if (result[3] != 0)
                {
                    s = s + result[3] + "银币，";
                }
                if (result[4] != 0)
                {
                    s = s + result[4] + "铜币";
                }

                args.Player.SendSuccessMessage(s);
            }
            else if (result[0] == 1)
            {
                args.Player.SendErrorMessage("购买失败!尚不提供此物品");
            }
            else if (result[0] == 2)
            {
                args.Player.SendErrorMessage("购买失败!你拥有的货币不足以购买");
            }
        }
    }
}
