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
        // 2026-10-05 重写（第二版）：自己实现扣款 —— 全背包统计硬币 → 清空 → 找零写回硬币槽并同步客户端。
        //   ① 统计范围不限 50~53（硬币放哪儿都算得到）
        //   ② 找零直接进背包（不再掉地上、不发重复包）
        //   ③ 关键步骤写日志，便于直接排查
        public static int[] TakeMoneyFromPlayer(this TSPlayer plr, int price)
        {
            int[] result = new int[5];
            if (price <= 0) { result[0] = 1; return result; }

            var inv = plr.TPlayer.inventory;
            long total = 0;
            for (int i = 0; i < inv.Length; i++)
            {
                if (inv[i] == null) continue;
                switch (inv[i].type)
                {
                    case 71: total += inv[i].stack; break;
                    case 72: total += (long)inv[i].stack * 100; break;
                    case 73: total += (long)inv[i].stack * 10000; break;
                    case 74: total += (long)inv[i].stack * 1000000; break;
                }
            }

            if (total < price)
            {
                TShock.Log.ConsoleInfo($"[ShopC] {plr.Name} 余额不足：有 {total} 铜，需 {price} 铜");
                result[0] = 1;
                return result;
            }

            // 1) 清空背包内所有硬币
            for (int i = 0; i < inv.Length; i++)
            {
                if (inv[i] == null) continue;
                int t = inv[i].type;
                if (t >= 71 && t <= 74)
                {
                    inv[i].SetDefaults(0);
                    plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i);
                }
            }

            // 2) 找零写回硬币槽
            long change = total - price;
            GiveCoins(plr, change);

            TShock.Log.ConsoleInfo($"[ShopC] {plr.Name} 扣款：原 {total} 铜 - 花费 {price} 铜 = 余 {change} 铜");

            result[0] = 0;
            result[1] = price / 1000000;
            result[2] = (price / 10000) % 100;
            result[3] = (price / 100) % 100;
            result[4] = price % 100;
            return result;
        }

        // 把 copper 个铜币拆成 铂/金/银/铜 写回硬币槽（50=铜 51=银 52=金 53=铂），并同步客户端
        private static void GiveCoins(TSPlayer plr, long copper)
        {
            if (copper <= 0) return;
            var inv = plr.TPlayer.inventory;
            int[] slotOf   = { 53, 52, 51, 50 };
            int[] coinType = { 74, 73, 72, 71 };
            long[] amount  = { copper / 1000000, (copper / 10000) % 100 , (copper / 100) % 100, copper % 100 };

            for (int k = 0; k < 4; k++)
            {
                long num = amount[k];
                int slot = slotOf[k], type = coinType[k];
                while (num > 0)
                {
                    int put = (int)Math.Min(num, 100);
                    num -= put;
                    if (inv[slot] == null || inv[slot].type == 0)
                    {
                        inv[slot].SetDefaults(type);
                        inv[slot].stack = put;
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, slot);
                    }
                    else if (inv[slot].type == type && inv[slot].stack + put <= 100)
                    {
                        inv[slot].stack += put;
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, slot);
                    }
                    else
                    {
                        plr.GiveItem(type, put, 0);   // 槽位被占/已满 → 交给 TShock 找位置
                        break;
                    }
                }
            }
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
