//代码来源：https://github.com/chi-rei-den/PluginTemplate/blob/master/src/PluginTemplate/Program.cs

using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

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
            //恋恋给出的模板代码中展示了如何为TShock添加一个指令
            Commands.ChatCommands.Add(new Command(
                permissions: new List<string> { "chest.shop" },
                cmd: this.Cmd,
                "shopc", "hw"));
            ShopListConfig.Load();
        }

        //执行指令时对指令进行处理的方法
        private void Cmd(CommandArgs args)
        {
            Player clientPlayer = args.TPlayer;


            if (args.Parameters[0].Equals("help"))
            {
                args.Player.SendSuccessMessage("欢迎光临!");
                args.Player.SendSuccessMessage(ShopListConfig.Shoplist[1].type.ToString());
            }
            else
            {
                int type = int.Parse(args.Parameters[0]);
                int num = int.Parse(args.Parameters[1]);
                int[] result=args.Player.BuyItemC(ShopListConfig,type, num,0);
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
}