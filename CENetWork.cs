using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Armor.AzafureT3;
using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Content.UI.Poops;
using CalamityMod.Events;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static CalamityEntropy.CalamityEntropy;

namespace CalamityEntropy
{
    public enum CEMessageType : byte
    {
        LotteryMachineRightClicked,
        TurnFriendly,
        Text,
        BossKilled,
        PlayerSetRB,
        PlayerSetPos,
        VoidTouchDamageShow,
        PoopSync,
        SpawnItem,
        PickUpPoop,
        SyncEntropyMode,
        RuneDashSync,
        SendDashNPCDoUpdate,
        DestroyChest,
        SyncPlayerLife,
        NihilityConnet,
        SyncBookmarks,
        AcropolisTrans,
        SyncPlayerDead,
        SyncDRShadowCrystal,
        SyncPlayer = 255
    }

    internal class CENetWork
    {
        public static void Handle(BinaryReader reader, int whoAmI)
        {
            CEMessageType messageType = (CEMessageType)reader.ReadByte();
            if (messageType == CEMessageType.LotteryMachineRightClicked)
            {
                int plr = reader.ReadInt32();
                int npc = reader.ReadInt32();
                int wai = reader.ReadInt32();
                if (npc.ToNPC().ModNPC is LotteryMachine lm)
                {
                    lm.RightClicked(Main.player[plr]);
                    if (Main.dedServ)
                    {
                        ModPacket packet = Instance.GetPacket();
                        packet.Write((byte)CEMessageType.LotteryMachineRightClicked);
                        packet.Write(plr);
                        packet.Write(npc);
                        packet.Write(wai);
                        packet.Send();//如果接受端是服务器，说明是来自客户端的广播，所以可以忽略来源的客户端
                        //by polaris:这个客户端发送的时候并没有执行对应的代码，得给客户端发一遍
                    }
                }
            }
            else if (messageType == CEMessageType.TurnFriendly)
            {
                int id = reader.ReadInt32();
                int owner = reader.ReadInt32();
                id.ToNPC().Entropy().ToFriendly = true;
                id.ToNPC().Entropy().f_owner = owner;
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.TurnFriendly);
                    packet.Write(id);
                    packet.Write(owner);
                    packet.Send();
                }
            }
            else if (messageType == CEMessageType.Text)
            {
                Main.NewText(reader.ReadString());
            }
            else if (messageType == CEMessageType.BossKilled)
            {
                bool flag = reader.ReadBoolean();
            }
            else if (messageType == CEMessageType.PlayerSetRB)
            {
                int playerIndex = reader.ReadInt32();
                bool active = reader.ReadBoolean();
                playerIndex.ToPlayer().Entropy().rBadgeActive = active;

                if (Main.dedServ)
                {
                    if (!active)
                    {
                        playerIndex.ToPlayer().velocity *= 0.2f;
                    }
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.PlayerSetRB);
                    packet.Write(playerIndex);
                    packet.Write(active);
                    packet.Send();
                }
                else
                {
                    if (playerIndex != Main.myPlayer)
                    {
                        if (!active)
                        {
                            playerIndex.ToPlayer().velocity *= 0.2f;
                        }
                        if (active)
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AscendantActivate"), playerIndex.ToPlayer().Center);
                        }
                        else
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AscendantOff"), playerIndex.ToPlayer().Center);
                        }
                    }
                }
            }
            else if (messageType == CEMessageType.PlayerSetPos)
            {
                int id = reader.ReadInt32();
                Vector2 pos = reader.ReadVector2();
                if (id != Main.myPlayer)
                {
                    id.ToPlayer().Center = pos;
                }
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.PlayerSetPos);
                    packet.Write(id);
                    packet.WriteVector2(pos);
                    packet.Send(-1, whoAmI);//如果接受端是服务器，说明是来自客户端的广播，所以可以忽略来源的客户端
                }
            }
            else if (messageType == CEMessageType.VoidTouchDamageShow)
            {
                if (!Main.dedServ)
                {
                    NPC npc = reader.ReadInt32().ToNPC();
                    int damageDone = reader.ReadInt32();
                    CombatText.NewText(npc.getRect(), new Color(148, 148, 255), damageDone);
                }
            }
            else if (messageType == CEMessageType.PoopSync)
            {
                Player player = reader.ReadInt32().ToPlayer();
                bool holding = reader.ReadBoolean();
                player.Entropy().holdingPoop = holding;
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.PoopSync);
                    packet.Write(player.whoAmI);
                    packet.Write(holding);
                    packet.Send(-1, whoAmI);//如果接受端是服务器，说明是来自客户端的广播，所以可以忽略来源的客户端
                }
            }
            else if (messageType == CEMessageType.SpawnItem)
            {
                int plr = reader.ReadInt32();
                int itemtype = reader.ReadInt32();
                int stack = reader.ReadInt32();
                Player player = plr.ToPlayer();

                if (!Main.dedServ && itemtype == ModContent.ItemType<PoopPickup>())
                {
                    CEUtils.PlaySound("fart", 1, player.Center);
                }
                if (Main.dedServ)
                {
                    int i = Item.NewItem(player.GetSource_FromThis(), player.getRect(), new Item(itemtype, stack), false, true);
                    if (i < Main.item.Length)
                    {
                        Main.item[i].noGrabDelay = 100;
                    }
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.SpawnItem);
                    packet.Write(player.whoAmI);
                    packet.Write(itemtype);
                    packet.Write(stack);
                    packet.Send(-1, whoAmI);//如果接受端是服务器，说明是来自客户端的广播，所以可以忽略来源的客户端
                }
            }
            else if (messageType == CEMessageType.PickUpPoop)
            {
                int plr = reader.ReadInt32();
                string name = reader.ReadString();
                Player player = plr.ToPlayer();
                Poop poop = new PoopNormal();
                foreach (Poop p in Poop.instances)
                {
                    if (p.FullName == name)
                    {
                        poop = p;
                    }
                }
                player.Entropy().poops.Add(poop);
            }
            else if (messageType == CEMessageType.SyncEntropyMode)
            {
                bool enabled = reader.ReadBoolean();
                EntropyMode = enabled;
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.SyncEntropyMode);
                    packet.Write(enabled);
                    packet.Send();
                }
                else
                {
                    if (EntropyMode)
                    {
                        Main.NewText(Instance.GetLocalization("EntropyModeActive").Value, new Color(170, 18, 225));
                    }
                    else
                    {
                        Main.NewText(Instance.GetLocalization("EntropyModeDeactive").Value, new Color(170, 18, 225));
                    }
                }
            }
            else if (messageType == CEMessageType.RuneDashSync)
            {
                int wai = reader.ReadInt32();
                Player player = wai.ToPlayer();
                float dir = reader.ReadSingle();
                bool e = reader.ReadBoolean();
                if (e)
                {
                    player.Entropy().RuneDash = 0;
                }
                else
                {
                    player.Entropy().RuneDash = RuneWing.MAXDASHTIME;
                    player.Entropy().RuneDashDir = dir;
                }
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)(CEMessageType.RuneDashSync));
                    packet.Write(wai);
                    packet.Write(dir);
                    packet.Write(e);
                    packet.Send(-1, wai);
                }
            }
            else if (messageType == CEMessageType.DestroyChest)
            {
                if (Main.dedServ)
                {
                    Player player = reader.ReadInt32().ToPlayer();
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    CEUtils.CheckChestDestroy(player, x, y);
                }
            }
            else if (messageType == CEMessageType.SyncPlayerLife)
            {
                int plr = reader.ReadInt32();
                int lifeTo = reader.ReadInt32();
                plr.ToPlayer().statLife = lifeTo;
                if (Main.dedServ)
                {
                    ModPacket packet = CalamityEntropy.Instance.GetPacket();
                    packet.Write((byte)CEMessageType.SyncPlayerLife);
                    packet.Write(plr);
                    packet.Write(lifeTo);
                    packet.Send(-1, whoAmI);
                }
            }
            else if (messageType == CEMessageType.NihilityConnet)
            {
                bool reset = reader.ReadBoolean();
                int plr1 = reader.ReadInt32();
                int plr2 = reader.ReadInt32();
                if (reset)
                {
                    plr1.ToPlayer().Entropy().NihTwinArmorConnetPlayer = -1;
                    plr2.ToPlayer().Entropy().NihTwinArmorConnetPlayer = -1;
                }
                else
                {
                    plr1.ToPlayer().Entropy().NihTwinArmorConnetPlayer = plr2;
                    plr2.ToPlayer().Entropy().NihTwinArmorConnetPlayer = plr1;
                    if (!Main.dedServ)
                    {
                        CEUtils.PlaySound("ksLand", 1, plr1.ToPlayer().Center);
                        CEUtils.PlaySound("ksLand", 1, plr2.ToPlayer().Center);
                    }
                }
                if (Main.dedServ)
                {
                    ModPacket packet = CalamityEntropy.Instance.GetPacket();
                    packet.Write((byte)CEMessageType.NihilityConnet);
                    packet.Write(reset);
                    packet.Write(plr1);
                    packet.Write(plr2);
                    packet.Send(-1);
                }
            }
            else if (messageType == CEMessageType.SyncBookmarks)
            {
                int wai = reader.ReadInt32();
                var plr = wai.ToPlayer();
                int bookmarkCount = reader.ReadInt32();
                plr.Entropy().EBookStackItems = new();
                for (int i = 0; i < bookmarkCount; i++)
                {
                    Item itm = new Item(reader.ReadInt32());
                    ItemIO.Receive(itm, reader);
                    plr.Entropy().EBookStackItems.Add(itm);
                }
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.SyncBookmarks);
                    packet.Write(wai);
                    packet.Write(bookmarkCount);
                    foreach (var item in plr.Entropy().EBookStackItems)
                    {
                        packet.Write(item.type);
                        ItemIO.Send(item, packet);
                    }
                    packet.Send(-1, whoAmI);
                }
            }
            else if (messageType == CEMessageType.AcropolisTrans)
            {
                int plr = reader.ReadInt32();
                bool active = reader.ReadBoolean();
                int reload = reader.ReadInt32();
                int bullet = reader.ReadInt32();
                float slash = reader.ReadSingle();
                int dir = reader.ReadInt32();
                bool mode = reader.ReadBoolean();
                if (Main.myPlayer != plr)
                {
                    var mp = plr.ToPlayer().GetModPlayer<AcropolisArmorPlayer>();
                    mp.MechTrans = active;
                    mp.Reload = reload;
                    mp.Bullet = bullet;
                    mp.SlashP = slash;
                    mp.slashDir = dir;
                    mp.CannonMode = mode;
                }
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.AcropolisTrans);
                    packet.Write(plr);
                    packet.Write(active);
                    packet.Write(reload);
                    packet.Write(bullet);
                    packet.Write(slash);
                    packet.Write(dir);
                    packet.Write(mode);
                    packet.Send(-1, plr);
                }
            }
            else if (messageType == CEMessageType.SyncPlayerDead)
            {
                int plr = reader.ReadInt32();
                bool d = reader.ReadBoolean();
                Vector2 pos = reader.ReadVector2();
                if (Main.myPlayer != plr)
                {
                    plr.ToPlayer().dead = d;
                    plr.ToPlayer().position = pos;
                }
                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.SyncPlayerDead);
                    packet.Write(plr);
                    packet.Write(d);
                    packet.WriteVector2(pos);
                    packet.Send(-1, plr);
                }
            }
            else if (messageType == CEMessageType.SyncDRShadowCrystal)
            {
                int plr = reader.ReadInt32();
                bool ch1 = reader.ReadBoolean();
                bool ch2 = reader.ReadBoolean();
                bool ch3 = reader.ReadBoolean();
                bool ch4 = reader.ReadBoolean();
                bool ch5 = reader.ReadBoolean();
                plr.ToPlayer().Entropy().drCrystals = new List<bool>() { ch1, ch2, ch3, ch4, ch5 };
                if (Main.dedServ)
                {
                    var mp = Instance.GetPacket();
                    mp.Write((byte)CEMessageType.SyncDRShadowCrystal);
                    mp.Write(plr);
                    mp.Write(ch1);
                    mp.Write(ch2);
                    mp.Write(ch3);
                    mp.Write(ch4);
                    mp.Write(ch5);
                    mp.Send(-1, plr);
                }
            }
            else if (messageType == CEMessageType.SyncPlayer)
            {
                int wai = reader.ReadInt32();
                int loreCount = reader.ReadInt32();
                Player plr = wai.ToPlayer();
                bool local = wai == Main.myPlayer;
                if (!local)
                    plr.Entropy().enabledLoreItems.Clear();
                for (int i = 0; i < loreCount; i++)
                {
                    int t = reader.ReadInt32();
                    if (!local)
                        plr.Entropy().enabledLoreItems.Add(t);
                }

                if (Main.dedServ)
                {
                    ModPacket packet = Instance.GetPacket();
                    packet.Write((byte)CEMessageType.SyncPlayer);
                    packet.Write(wai);
                    packet.Write(loreCount);
                    foreach (var i in plr.Entropy().enabledLoreItems)
                    {
                        packet.Write(i);
                    }
                    packet.Send(-1, wai);
                }
            }
        }
    }
}
