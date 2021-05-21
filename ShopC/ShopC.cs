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
            else if(args.Parameters[0].Equals("luck")){
                if (ShopListConfig.IsLotteryActive)
                {
                    int num = int.Parse(args.Parameters[1]);
                    int type = 0;
                    int[] result=args.Player.TakeMoneyFromPlayer(ShopListConfig.Lotteryprice*num);
                    
                    if(result[0]==0){
                    Random random=new Random();
                    for (int i = 0; i < num; i++)
                    {

                        double flag = random.NextDouble();
                        if (flag <= ShopListConfig.IronBoxChance)
                        {
                            type = 2335;
                        }
                        //多种宝匣
                        else if (flag <= ShopListConfig.IronBoxChance + ShopListConfig.ClutterBoxChance)
                        {
                            Random random1 = new Random();
                            flag = random1.NextDouble();
                            if (flag <= 0.1)
                            {
                                type = 3208;
                            }
                            else if (flag <= 0.2)
                            {
                                type = 3206;
                            }
                            else if (flag <= 0.3)
                            {
                                type = 3203;
                            }
                            else if (flag <= 0.4)
                            {
                                type = 3204;
                            }
                            else if (flag <= 0.5)
                            {
                                type = 3207;
                            }
                            else if (flag <= 0.6)
                            {
                                type = 3205;
                            }
                            else if (flag <= 0.7)
                            {
                                type = 4405;
                            }
                            else if (flag <= 0.8)
                            {
                                type = 4407;
                            }
                            else if (flag <= 0.9)
                            {
                                type = 4877;
                            }
                            else if (flag <= 1)
                            {
                                type = 5002;
                            }

                        }
                        else if (flag <= ShopListConfig.IronBoxChance + ShopListConfig.ClutterBoxChance +
                            ShopListConfig.GoldenBoxChance)
                        {
                            type = 2336;
                        }
                        else if (flag <= ShopListConfig.IronBoxChance + ShopListConfig.ClutterBoxChance +
                            ShopListConfig.GoldenBoxChance + ShopListConfig.WoodenBoxChance)
                        { 
                            type = 2334;
                        }
                        else
                        {
                            type = 0;
                        }

                        if (type == 0)
                        {
                            //没有抽中
                        }
                        else
                        {
                            args.Player.GiveItemEX(type, 1, 0);
                        }

                    }
                    string s = "购买成功！共花费";
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
                    else
                    {
                        args.Player.SendErrorMessage("购买失败！请确保您有足够的货币");
                    }


                }
                else
                {
                    args.Player.SendErrorMessage("尚未启用抽奖");
                }

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
                    args.Player.SendErrorMessage("购买失败!本店尚不提供此物品");
                }
                else if (result[0] == 2)
                {
                    args.Player.SendErrorMessage("购买失败!请确保您有足够的货币");
                }
            }
        }
    }
}