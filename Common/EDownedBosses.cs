using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Common
{
    public class EDownedBosses : ModSystem
    {
        public static bool downedCruiser = false;
        public static bool downedNihilityTwin = false;
        public static bool EntropyMode = false;
        public static bool TDR = false;
        public static bool downedProphet = false;
        public static bool downedLuminaris = false;
        public static bool downedApsychos = false;
        public static bool downedAcropolis = false;
        //脱离灾厄:原存于灾厄DownedBossSystem,改为自有旗标(tag键与网络位序不变,旧档兼容)
        public static bool downedPrimordialWyrm = false;
        //虚空驱逐舰(月后 T2)
        public static bool downedVoidDestroyer = false;
        //日月双神(驱逐舰后)
        public static bool downedSunGoddess = false;
        public static bool downedStarmoonSage = false;
        //虚空史莱姆(虚无双子后)
        public static bool downedVoidSlime = false;
        //沙海冥龙(石巨人后)
        public static bool downedSandseaDragon = false;
        public static Point ForbiddenArchiveCenter = new Point(-1, -1);
        public override void ClearWorld() {
            EntropyMode = false;
            downedCruiser = false;
            downedNihilityTwin = false;
            downedProphet = false;
            downedLuminaris = false;
            downedAcropolis = false;
            downedApsychos = false;
            ForbiddenArchiveCenter = new Point(-1, -1);
            downedPrimordialWyrm = false;
            downedVoidDestroyer = false;
            downedSunGoddess = false;
            downedStarmoonSage = false;
            downedVoidSlime = false;
            downedSandseaDragon = false;
        }

        public override void SaveWorldData(TagCompound tag) {
            if (EntropyMode) {
                tag["EntropyMode"] = true;
            }
            if (TDR) {
                tag["TDR"] = true;
            }
            if (downedCruiser) {
                tag["downedCruiser"] = true;
            }
            if (downedNihilityTwin) {
                tag["downedNihilityTwin"] = true;
            }
            if (downedProphet) {
                tag["downedProphet"] = true;
            }
            if (downedLuminaris) {
                tag["downedLuminaris"] = true;
            }
            if (downedAcropolis) {
                tag["downedAcropolis"] = true;
            }
            if (downedPrimordialWyrm) {
                tag["downedPrimordialWyrm"] = true;
            }
            if (downedApsychos) {
                tag["downedApsychos"] = true;
            }
            if (downedVoidDestroyer) {
                tag["downedVoidDestroyer"] = true;
            }
            if (downedSunGoddess) {
                tag["downedSunGoddess"] = true;
            }
            if (downedStarmoonSage) {
                tag["downedStarmoonSage"] = true;
            }
            if (downedVoidSlime) {
                tag["downedVoidSlime"] = true;
            }
            if (downedSandseaDragon) {
                tag["downedSandseaDragon"] = true;
            }
            tag["DungeonArchiveCenterX"] = ForbiddenArchiveCenter.X;
            tag["DungeonArchiveCenterY"] = ForbiddenArchiveCenter.Y;
        }
        public static Vector2 GetDungeonArchiveCenterPos() {
            return ForbiddenArchiveCenter.ToVector2() * 16 + new Vector2(8, 1288);
        }

        public override void LoadWorldData(TagCompound tag) {
            downedCruiser = tag.ContainsKey("downedCruiser");
            downedNihilityTwin = tag.ContainsKey("downedNihilityTwin");
            EntropyMode = tag.ContainsKey("EntropyMode");
            downedProphet = tag.ContainsKey("downedProphet");
            downedLuminaris = tag.ContainsKey("downedLuminaris");
            downedAcropolis = tag.ContainsKey("downedAcropolis");
            downedApsychos = tag.ContainsKey("downedApsychos");
            downedPrimordialWyrm = tag.ContainsKey("downedPrimordialWyrm");
            downedVoidDestroyer = tag.ContainsKey("downedVoidDestroyer");
            downedSunGoddess = tag.ContainsKey("downedSunGoddess");
            downedStarmoonSage = tag.ContainsKey("downedStarmoonSage");
            downedVoidSlime = tag.ContainsKey("downedVoidSlime");
            downedSandseaDragon = tag.ContainsKey("downedSandseaDragon");
            TDR = tag.ContainsKey("TDR");
            if (tag.ContainsKey("DungeonArchiveCenterX") && tag.ContainsKey("DungeonArchiveCenterY")) {
                ForbiddenArchiveCenter = new(tag.GetInt("DungeonArchiveCenterX"), tag.GetInt("DungeonArchiveCenterY"));
            }
            else {
                ForbiddenArchiveCenter = new(-1, -1);
            }
        }

        public override void NetSend(BinaryWriter writer) {
            var flags = new BitsByte();
            var flags2 = new BitsByte();
            //flags[1] 是已移除的深渊亡魂旧位,留空不复用,其余位序保持不变
            flags[0] = downedCruiser;
            flags[2] = downedNihilityTwin;
            flags[3] = downedProphet;
            flags[4] = downedLuminaris;
            flags[5] = downedAcropolis;
            flags[6] = downedPrimordialWyrm;
            flags[7] = downedVoidDestroyer;
            flags2[0] = EntropyMode;
            flags2[1] = TDR;
            flags2[2] = downedApsychos;
            flags2[3] = downedSunGoddess;
            flags2[4] = downedStarmoonSage;
            flags2[5] = downedSandseaDragon;
            //虚空史莱姆占最高位,flags2[6] 空着
            flags2[7] = downedVoidSlime;

            writer.Write(flags);
            writer.Write(flags2);
            writer.Write(ForbiddenArchiveCenter.X);
            writer.Write(ForbiddenArchiveCenter.Y);
        }

        public override void NetReceive(BinaryReader reader) {
            BitsByte flags = reader.ReadByte();
            BitsByte flags2 = reader.ReadByte();

            downedCruiser = flags[0];
            downedNihilityTwin = flags[2];
            downedProphet = flags[3];
            downedLuminaris = flags[4];
            downedAcropolis = flags[5];
            downedPrimordialWyrm = flags[6];
            downedVoidDestroyer = flags[7];
            EntropyMode = flags2[0];
            TDR = flags2[1];
            downedApsychos = flags2[2];
            downedSunGoddess = flags2[3];
            downedStarmoonSage = flags2[4];
            downedSandseaDragon = flags2[5];
            downedVoidSlime = flags2[7];
            ForbiddenArchiveCenter = new Point(reader.ReadInt32(), reader.ReadInt32());
        }
    }
}