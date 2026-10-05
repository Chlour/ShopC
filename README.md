# ShopC

基于**原版货币**的 TShock 自定义商店插件。

把原版物品（尤其是各种**匣子**）上架成商品，玩家用铜/银/金/铂金币购买 —— 因为匣子本身是"开箱随机"，所以**买匣子就等于抽奖** 🎁

- 作者：Chlour
- 已适配：**TShock 6.2.1 / Terraria 1.4.5.8 / .NET 9**（在 Linux arm64 树莓派上实测通过）

---

## 环境要求

| 项目 | 版本 |
|---|---|
| TShock | 6.x（API 版本 2.1） |
| Terraria | 1.4.5.x |
| .NET | 9.0（插件编译目标 `net9.0`） |

## 安装

1. 编译得到 `ShopC.dll`（见下方「编译」）
2. 把 `ShopC.dll` 放进服务器的 `ServerPlugins/` 目录
3. 重启服务器（或使用 TShock 的插件重载）
4. 首次运行会在 `tshock/ShopList.json` 生成默认配置

## 命令

命令：`/shopc`，别名 `/hw`　权限：`chest.shop`

| 命令 | 说明 |
|---|---|
| `/shopc` 或 `/shopc help` | 显示帮助与商品数量 |
| `/shopc list` | 列出全部商品（ID / 名称 / 单价） |
| `/shopc <物品ID> [数量]` | 购买指定物品，数量省略时为 1 |

给玩家组授权：

```
/group addperm <组名> chest.shop
```

（控制台以 Server 身份执行时无需该权限）

## 配置：`tshock/ShopList.json`

```json
{
  "helptext": "原版匣子抽奖商店：用原版货币购买匣子。",
  "shoplist": [
    { "type": 2334, "price": 5500,  "prefix": 0 },
    { "type": 2335, "price": 33000, "prefix": 0 },
    { "type": 2336, "price": 83000, "prefix": 0 }
  ]
}
```

- `type`：物品 ID
- `price`：单价，**单位为铜币**（100 铜 = 1 银，10000 = 1 金，1000000 = 1 铂金）
- `prefix`：修饰语 ID，`0` 表示无修饰

**修改后执行 `/reload` 即可生效，无需重启服务器。**

### 示例：17 种原版匣子

| 匣子 | ID | 价格(铜) | 匣子 | ID | 价格(铜) |
|---|---|---|---|---|---|
| 木匣 | 2334 | 5500 | 黑曜石匣 | 4877 | 27000 |
| 珍珠木匣 | 3979 | 4000 | 绿洲匣 | 4407 | 28000 |
| 铁匣 | 2335 | 33000 | 腐化匣 | 3203 | 31000 |
| 秘银匣 | 3980 | 24000 | 猩红匣 | 3204 | 31000 |
| 钛金匣 | 3981 | 64000 | 海洋匣 | 5002 | 33000 |
| 丛林匣 | 3208 | 30000 | 荆棘匣 | 3987 | 30000 |
| 冰冻匣 | 4405 | 27000 | 金匣 | 2336 | 83000 |
| 天空匣 | 3206 | 28000 | 神圣匣 | 3207 | 30000 |
| 地牢匣 | 3205 | 29000 | | | |

## 编译

```bash
# 需要 .NET 9 SDK
# 程序集引用指向 TShock 服务器目录（csproj 里用 HintPath 配置）
DOTNET_ROOT=$HOME/.dotnet $HOME/.dotnet/dotnet build -c Release ShopC/ShopC.csproj

# 产物：ShopC/bin/Release/ShopC.dll
```

`ShopC.csproj` 引用了服务器目录下的：`TShockAPI.dll`、`OTAPI.dll`、`OTAPI.Runtime.dll`、`TerrariaServer.dll`、`ModFramework.dll`、`HttpServer.dll`（均为 `<Private>false</Private>`，不会复制进产物）。

## 定价参考（可选）

匣子的开箱期望值（内容物按商店售价折算，约为**下限**）：

```
木匣 ≈44银    铁匣 ≈2.67金    金匣 ≈6.61金    钛金匣 ≈5.11金
生物群系匣 ≈2.2~2.6金        秘银匣 ≈1.93金
```

建议：**售价 ≈ 期望值 × 1.2~1.25**（低于期望值会被"买→开→卖店"套利；过高则没人买）。
稀有掉落（如猎鹰之刃、生命水晶、附魔剑）的价值远高于售价，这正是抽奖的吸引力所在。

## 移植记录（2026-10，TShock 6.2.1）

- `Item.NewItem` 适配 1.4.5.x：新版需要 `IEntitySource` 参数，且 `EntitySource_Misc` 已被移除 → 自定义 `ShopItemSource : IEntitySource`
- 配置读写由 Newtonsoft.Json 改为 .NET 内置 `System.Text.Json`（字段改属性 + `JsonPropertyName`）
- 挂接 `GeneralHooks.ReloadEvent`，支持 `/reload` 重载配置
- 修复 `/shopc` 无参数、参数非数字时的越界崩溃；新增 `/shopc list` 子命令
- 移除 `TakeMoneyFromPlayer` 中"每次购买前无条件扣掉背包槽 51 的 10 个物品"的破坏性行为
- 新建 `ShopC.csproj`（原仓库缺失），并加入 `.gitignore` 忽略 `bin/`、`obj/`

## 许可

未指定许可证（沿用原作者）。`ShopC.cs` 的插件模板来自 [chi-rei-den/PluginTemplate](https://github.com/chi-rei-den/PluginTemplate)。
