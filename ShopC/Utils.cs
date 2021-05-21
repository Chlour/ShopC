using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace ShopC
{
    static class Utils
    {
        public static void GiveItemEX(this TSPlayer plr, int type, int stack, int prefix)
        {
            var item = new Item();
            item.SetDefaults(type);
            int num = 0;

            while (num < stack)
            {
                int itemID;
                if (stack - num > item.maxStack)
                {
                    itemID = Item.NewItem(plr.TPlayer.position, item.height, item.width, type, item.maxStack, false,
                        prefix);
                    num += item.maxStack;
                }
                else
                {
                    itemID = Item.NewItem(plr.TPlayer.position, item.height, item.width, type, stack - num, false,
                        prefix);
                    num = stack;
                }

                NetMessage.SendData(90, -1, -1, null, itemID, 0);
            }
        }

        public static bool DelItemFromInventory(this TSPlayer plr, int type, int num, int prefix)
        {
            var tempnum = 0;
            int slot = 0;
            plr.TPlayer.inventory.ForEach(i =>
            {
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
            });
            if (tempnum == num) return true;
            return false;
        }

        public static int[] BuyItemC(this TSPlayer plr,ShopListConfig shopListConfig,int type, int num,int prefix)
        {

            int[] result = new int[5];
            int price = 0;
            
            shopListConfig.Shoplist.ForEach(item =>
            {
                if (item.type == type && item.prefix == prefix)
                {
                    price = item.price * num;
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
            if (takeMoneyResult[0]==1)
            {
                //货币不足
                result[0] = 2;
                return result;
            }
            plr.GiveItemEX(type,num,prefix);
            //购买成功返回0
            return takeMoneyResult;
        }

        /**
         * 以铜币为单位从背包中扣除金币
         */
        public static int[] TakeMoneyFromPlayer(this TSPlayer plr, int price)
        {
            int[] result = new int[5];
            plr.DelItemFromInventoryByIndex(51, 10);
            int copper_coin = 0;
            int silver_coin = 0;
            int gold_coin = 0;
            int platinum_coin = 0;
            //铜币为单位的总数
            int total = 0;
            //买家的货币数量
            for (int i = 50; i < 54; i++)
            {
                if (plr.TPlayer.inventory[i].type == 71)
                {
                    copper_coin = copper_coin + plr.TPlayer.inventory[i].stack;
                }
                else if (plr.TPlayer.inventory[i].type == 72)
                {
                    silver_coin = silver_coin + plr.TPlayer.inventory[i].stack;
                }
                else if (plr.TPlayer.inventory[i].type == 73)
                {
                    gold_coin = gold_coin + plr.TPlayer.inventory[i].stack;
                }
                else if (plr.TPlayer.inventory[i].type == 74)
                {
                    platinum_coin = platinum_coin + plr.TPlayer.inventory[i].stack;
                }
            }
            total = copper_coin + silver_coin * 100 + gold_coin * 10000 + platinum_coin * 1000000;

            if (price>total)
            {
                //货币数不够
                result[0] = 1;
                return result;
            }

            //采用先删除后添加的方式
            for (int i = 50; i < 54; i++)
            {
                if (plr.TPlayer.inventory[i] != null)
                {
                    plr.DelItemFromInventoryByIndex(i, 1000);
                }
            }

            total = total - price;
            int total_p = total / 1000000;
            int total_g = (total / 10000) % 100;
            int total_s=(total / 100) % 100;
            int total_c=total % 100;
            if(total_p!=0)
            {plr.GiveItemEX(74,total_p,0);}
            if (total_g!=0)
            {
                plr.GiveItemEX(73,total_g,0);
            }
            if (total_s!=0)
            {
                plr.GiveItemEX(72,total_s,0);
            }
            if (total_c!=0)
            {
                plr.GiveItemEX(71,total_c,0);
            }

            result[0] = 0;
            result[1] = price / 1000000;
            result[2] = (price / 10000) % 100;
            result[3] = (price / 100) % 100;
            result[4] =price % 100;
            return result;
        }
        
        
        
        public static bool DelItemFromInventoryByIndex(this TSPlayer plr,int index,int num)
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