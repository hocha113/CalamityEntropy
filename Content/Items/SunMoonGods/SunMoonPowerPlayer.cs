using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.SunMoonGods
{
    /// <summary>
    /// 日光之力与星月之力的永久加成,两枚各只能生效一次
    /// 旗标随角色存档,联机走 CEMessageType.SyncSunMoonPower
    /// </summary>
    public class SunMoonPowerPlayer : ModPlayer
    {
        /// <summary>每枚增加的生命上限与魔力上限</summary>
        public const int LifeBonus = 25;
        public const int ManaBonus = 25;
        public const int DayDefense = 5;
        /// <summary>白天暴击率加成,单位是百分点</summary>
        public const float DayCrit = 5f;
        public const float NightDamage = 0.05f;
        public const float NightMoveSpeed = 0.05f;

        public bool SunlightUsed;
        public bool StarmoonUsed;

        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
            health = StatModifier.Default;
            mana = StatModifier.Default;
            int count = (SunlightUsed ? 1 : 0) + (StarmoonUsed ? 1 : 0);
            health.Base = count * LifeBonus;
            mana.Base = count * ManaBonus;
        }

        public override void PostUpdateEquips() {
            if (SunlightUsed && Main.dayTime) {
                Player.statDefense += DayDefense;
                Player.GetCritChance(DamageClass.Generic) += DayCrit;
            }
            if (StarmoonUsed && !Main.dayTime) {
                Player.GetDamage(DamageClass.Generic) += NightDamage;
                Player.moveSpeed += NightMoveSpeed;
            }
        }

        public override void SaveData(TagCompound tag) {
            if (SunlightUsed) {
                tag["SunlightPower"] = true;
            }
            if (StarmoonUsed) {
                tag["StarmoonPower"] = true;
            }
        }

        public override void LoadData(TagCompound tag) {
            SunlightUsed = tag.ContainsKey("SunlightPower");
            StarmoonUsed = tag.ContainsKey("StarmoonPower");
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)CEMessageType.SyncSunMoonPower);
            packet.Write((byte)Player.whoAmI);
            packet.Write(PackFlags());
            packet.Send(toWho, fromWho);
        }

        public override void CopyClientState(ModPlayer targetCopy) {
            SunMoonPowerPlayer clone = (SunMoonPowerPlayer)targetCopy;
            clone.SunlightUsed = SunlightUsed;
            clone.StarmoonUsed = StarmoonUsed;
        }

        public override void SendClientChanges(ModPlayer clientPlayer) {
            SunMoonPowerPlayer clone = (SunMoonPowerPlayer)clientPlayer;
            if (clone.SunlightUsed != SunlightUsed || clone.StarmoonUsed != StarmoonUsed) {
                SyncPlayer(-1, Main.myPlayer, false);
            }
        }

        private byte PackFlags() => (byte)((SunlightUsed ? 1 : 0) | (StarmoonUsed ? 2 : 0));

        /// <summary>服务端用发包者的下标,不信包里写的那个</summary>
        public static void Receive(BinaryReader reader, int sender) {
            int index = reader.ReadByte();
            byte flags = reader.ReadByte();
            if (Main.dedServ) {
                index = sender;
            }
            if (index < 0 || index >= Main.maxPlayers) {
                return;
            }
            SunMoonPowerPlayer power = Main.player[index].GetModPlayer<SunMoonPowerPlayer>();
            power.SunlightUsed = (flags & 1) != 0;
            power.StarmoonUsed = (flags & 2) != 0;
            if (Main.dedServ) {
                power.SyncPlayer(-1, sender, false);
            }
        }
    }
}