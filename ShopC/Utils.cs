using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TShockAPI;

namespace ShopC
{
    static class Utils
    {
        // 2026-10-05 修复「买完钱反而变多」：
        //   旧实现把物品/找零「掉到地上」，并且在 Item.NewItem（它自身已会同步给客户端）之后
        //   又多发了一次 NetMessage.SendData(90, ...)，客户端可能收到两份 → 捡起来就是双倍货币。
        //   现统一改为直接放进玩家背包（TShock 的 GiveItem 会处理堆叠上限与放不下的情况）。
        public static void GiveItemEX(this TSPlayer plr, int type, int stack, int prefix)
        {
            plr.GiveItem(type, stack, prefix);
        }

        public static bool DelItemFromInventory(this TSPlayer plr, int type, int num, int prefix)
        {
            var tempnum = 0;
            int slot = 0;
            for (int slotIdx = 0; slotIdx < plr.TPlayer.inventory.Length; slotIdx++)
            {
                Item i = plr.TPlayer.inventory[slotIdx];
                if (i != null && i.type == type && (prefix == 0 ? true : i.prefix == prefix))
                {
                    if (tempnum < num)
                    {
                        if (num - tempnum >= i.stack)
                        {
                            tempnum += i.stack;
                            i.SetDefaults(0);
                            plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, slot); //移除玩家背包内的物品
                        }
                        else
                        {
                            i.stack -= num - tempnum;
                            tempnum = num;
                            plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, slot); //移除玩家背包内的物品
                        }
                    }

                }

                slot++;
            }
            if (tempnum == num) return true;
            return false;
        }

        public static int[] BuyItemC(this TSPlayer plr, ShopListConfig shopListConfig, int type, int num, int prefix)
        {

            int[] result = new int[5];
            int price = 0;

            shopListConfig.Shoplist.ForEach(item =>
            {
                if (item.Type == type && item.Prefix == prefix)
                {
                    price = item.Price * num;
                }

            });

            if (price == 0)
            {
                //本商店没有这种商品
                result[0] = 1;
                return result;
            }
            int[] takeMoneyResult = new int[5];
            takeMoneyResult = plr.TakeMoneyFromPlayer(price);
            if (takeMoneyResult[0] == 1)
            {
                //货币不足
                result[0] = 2;
                return result;
            }
            plr.GiveItemEX(type, num, prefix);
            //购买成功返回0
            return takeMoneyResult;
        }

        /**
         * 以铜币为单位从背包中扣除金币
         */
        // 2026-10-05 重写：改用泰拉瑞亚内置扣款 Player.BuyItem(price)
        //   旧手写逻辑的两个隐患：
        //     ① 只统计背包槽 50~53 的硬币（硬币放在别的槽就统计不到）
        //     ② 「先删硬币、再把找零掉到地上」的方式容易与服务端/客户端状态不同步
        //   现在由游戏内置逻辑处理：扣款、找零、硬币合并全部标准，买完只剩正确余额。
        public static int[] TakeMoneyFromPlayer(this TSPlayer plr, int price)
        {
            int[] result = new int[5];
            if (price <= 0)
            {
                result[0] = 1;
                return result;
            }

            if (!plr.TPlayer.BuyItem(price))
            {
                result[0] = 1;   // 货币不足
                return result;
            }

            // 把可能被改动的背包槽同步给该玩家（硬币可能在任意槽位，找零也会回到背包）
            for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
            {
                plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i);
            }

            result[0] = 0;
            result[1] = price / 1000000;
            result[2] = (price / 10000) % 100;
            result[3] = (price / 100) % 100;
            result[4] = price % 100;
            return result;
        }


        public static bool DelItemFromInventoryByIndex(this TSPlayer plr, int index, int num)
        {
            var tempnum = 0;


            Item i = plr.TPlayer.inventory[index];
            if (i != null)
            {
                if (tempnum < num)
                {
                    if (num - tempnum >= i.stack)
                    {
                        tempnum += i.stack;
                        i.SetDefaults(0);
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, index); //移除玩家背包内的物品
                    }
                    else
                    {
                        i.stack -= num - tempnum;
                        tempnum = num;
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, index); //移除玩家背包内的物品
                    }
                }

            }
            if (tempnum == num) return true;
            return false;
        }



    }



}
