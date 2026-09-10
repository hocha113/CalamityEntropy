using CalamityEntropy.Content.NPCs.SpiritFountain;
using CalamityEntropy.Core.CalamityRef;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class EntropyBossbar : ModBossBarStyle
    {
        //血条贴图在加载期就位,不再每帧 Request;只在客户端 Draw 里读取
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/Ebar1")]
        internal static Asset<Texture2D> Ebar1Tex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/Ebar1Alt")]
        internal static Asset<Texture2D> Ebar1AltTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/Ebar2")]
        internal static Asset<Texture2D> Ebar2Tex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/Ebar3")]
        internal static Asset<Texture2D> Ebar3Tex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/EbarLock")]
        internal static Asset<Texture2D> EbarLockTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/EBarWhite")]
        internal static Asset<Texture2D> EBarWhiteTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/EBarWhite2")]
        internal static Asset<Texture2D> EBarWhite2Tex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/Ebarc")]
        internal static Asset<Texture2D> EbarcTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/CrackedNoiseB")]
        internal static Asset<Texture2D> CrackedNoiseBTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/hl")]
        internal static Asset<Texture2D> HlTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/df")]
        internal static Asset<Texture2D> DfTex;
        [VaultLoaden("CalamityEntropy/Assets/Bossbar/atk")]
        internal static Asset<Texture2D> AtkTex;
        public override string DisplayName => Language.GetTextValue("Mods.CalamityEntropy.ModNameOverride");
        public Color barColor = Color.White;
        public Color buttomColor = Color.Yellow;
        public float comboProg = 1;
        public int comboTime = 0;
        public float lastProg = 1;
        public float comboTarget = 1;
        public static Dictionary<int, Color> bossbarColor;
        //亵渎天神与三守卫激怒时血条转青。加载期填 CEID,未命中的 0 不进集合
        public static HashSet<int> profanedEnrageNPCs = new HashSet<int>();
        public float whiteLerp = 0;
        public int comboTimeCount = 0;
        public override void Load()
        {
            bossbarColor = new Dictionary<int, Color>();
            profanedEnrageNPCs = new HashSet<int>();
        }
        public override void Unload()
        {
            bossbarColor = null;
            profanedEnrageNPCs = null;
        }
        public static Color getNpcBarColor(NPC npc)
        {
            EntropyBossbar.bossbarColor[636] = Main.DiscoColor;
            int type = npc.type;
            /*if (npc.type == ModContent.NPCType<CruiserHead>() && npc.ModNPC is CruiserHead cr)
            {
                if (cr.phaseTrans >= 120)
                {
                    return new Color(150, 60, 255);
                }
            }*/
            if (profanedEnrageNPCs != null && profanedEnrageNPCs.Contains(npc.type) && CECal.NPCEnraged(npc))
            {
                return new Color(102, 255, 255);
            }
            if (bossbarColor.ContainsKey(npc.type))
            {
                return bossbarColor[npc.type];
            }
            return new Color(255, 50, 50);
        }
        public int drawOfs = 0;
        public override bool PreventDraw => true;
        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            bool flag = false;
            NPC npc = null;
            if (info.npcIndexToAimAt >= 0 && info.npcIndexToAimAt.ToNPC().active && info.npcIndexToAimAt.ToNPC().IsABoss())
            {
                flag = true;
                npc = info.npcIndexToAimAt.ToNPC();
            }
            else
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (n.IsABoss())
                    {
                        if (n.realLife < 0 && CEUtils.getDistance(n.Center, Main.LocalPlayer.Center) < 9000)
                        {
                            flag = true;
                            npc = n;
                            break;
                        }
                    }
                }
            }
            if (!flag)
            {
                return;
            }
            Color turnColorBtm = Color.Yellow;
            if (CECal.NPCIncreasingDefenseOrDR(npc))
            {
                turnColorBtm = Color.Gray;
            }
            if (CECal.NPCEnraged(npc))
            {
                turnColorBtm = Color.Red;
            }
            buttomColor = Color.Lerp(buttomColor, turnColorBtm, 0.1f);
            bool slimeGodCore = CEID.NPC_SlimeGodCore > 0 && npc.type == CEID.NPC_SlimeGodCore;
            bool immune = npc.dontTakeDamage && !(npc.ModNPC is SpiritFountain) && !slimeGodCore;

            Vector2 center = new Vector2(Main.screenWidth / 2, Main.screenHeight - 70);

            float Shield = 0;
            float ShieldMax = 0;
            int life = npc.life;
            int lifeMax = npc.lifeMax;
            bool drawShield = false;
            float life_ = life;
            float lifeMax_ = lifeMax;
            if (npc.BossBar != null && npc.BossBar is ModBossBar mbb)
            {
                bool? v = mbb.ModifyInfo(ref info, ref life_, ref lifeMax_, ref Shield, ref ShieldMax);
                if (v.HasValue && v.Value)
                {
                    drawShield = Shield > 0;
                    life = (int)life_;
                    lifeMax = (int)lifeMax_;
                }
            }

            float shieldPerc = drawShield ? (Shield / ShieldMax) : 0;
            float prog = (float)life / (float)lifeMax;
            if (prog < 0)
                prog = 0;

            if (drawShield)
            {
                drawOfs -= 3;
                barColor = Color.Lerp(barColor, new Color(180, 180, 255), 0.1f);
            }
            else if (immune)
            {
                barColor = Color.Lerp(barColor, new Color(200, 106, 205), 0.1f);
            }
            else
            {
                barColor = Color.Lerp(barColor, getNpcBarColor(npc), 0.1f);
                drawOfs -= 9;
                if (drawOfs < -4500)
                    drawOfs += 4500;
            }
            if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
            {
                int eowLifes = 0;
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail)
                    {
                        eowLifes += n.life;
                    }

                }
                prog = (float)eowLifes / (float)ModContent.GetInstance<EModSys>().eowMaxLife;
            }
            if (slimeGodCore)
            {
                int sgLifes = 0;
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (EntropyModeGNPC.IsSlimeGodSlime(n.type))
                    {
                        sgLifes += n.life;
                    }
                }
                int sgMax = ModContent.GetInstance<EModSys>().slimeGodMaxLife;
                if (sgMax > 0)
                {
                    prog = (float)sgLifes / (float)sgMax;
                }
                if (prog == 0)
                {
                    prog = npc.life / (float)npc.lifeMax;
                }
            }
            if (prog < 0)
                prog = 0;
            if (prog == 0)
            {
                if (!immune)
                {
                    barColor = getNpcBarColor(npc);
                }
            }
            if (immune)
            {
                comboTime = 0;
            }
            comboTime--;
            if (comboTime < 0 || comboTarget - prog > 0.2f || comboTimeCount > 240)
            {
                comboTime = 0;
                comboTarget = prog;
                comboTimeCount = 0;
            }
            if (comboTime > 0)
            {
                comboTimeCount++;
            }
            if (prog < lastProg)
            {
                comboTime = 60;
            }
            lastProg = prog;
            comboProg = comboProg + (comboTarget - comboProg) * 0.1f;
            Texture2D bar1Norm = Ebar1Tex.Value;
            Texture2D bar1_ = Ebar1AltTex.Value;
            Texture2D bar2 = Ebar2Tex.Value;
            Texture2D bar3 = Ebar3Tex.Value;
            Texture2D barLocked = EbarLockTex.Value;
            Texture2D barWhite = EBarWhiteTex.Value;
            Texture2D barWhite2 = EBarWhite2Tex.Value;
            Texture2D barc = EbarcTex.Value;
            Texture2D crack = CrackedNoiseBTex.Value;
            Texture2D bar1 = bar1Norm;
            if (npc.GetBossHeadTextureIndex() < 0)
            {
                bar1 = bar1_;
            }

            spriteBatch.Draw(barWhite, center, new Rectangle(0, 0, 18 + (int)(500 * comboProg), bar1.Height), Color.White, 0, bar1.Size() * 0.5f, 1, SpriteEffects.None, 0);
            if (npc.dontTakeDamage && !slimeGodCore)
            {
                spriteBatch.Draw(barWhite2, center, new Rectangle(0, 0, 18 + (int)(500 * prog) + 2, bar1.Height), Color.Lerp(barColor, Color.White, 0.5f), 0, bar1.Size() * 0.5f, 1, SpriteEffects.None, 0);
            }

            spriteBatch.UseSampleState_UI(SamplerState.LinearWrap);
            try
            {
                spriteBatch.Draw(bar2, center + new Vector2(0, 8), new Rectangle(drawOfs, 0, (int)(500 * prog), bar2.Height), barColor, 0, bar2.Size() * 0.5f, 1, SpriteEffects.None, 0);
            }
            catch { }
            spriteBatch.UseSampleState_UI(SamplerState.AnisotropicClamp);
            if (drawShield)
            {
                Main.spriteBatch.UseBlendState_UI(BlendState.Additive, SamplerState.LinearWrap);
                spriteBatch.Draw(barLocked, center, new Rectangle(0, 0, 18 + (int)(500 * shieldPerc), bar1.Height), Color.Lerp(barColor, Color.White, 0.6f), 0, bar1.Size() * 0.5f, 1, SpriteEffects.None, 0);
                for (float h = 1; h > 0; h -= 0.2f)
                {
                    if (shieldPerc <= h)
                    {
                        spriteBatch.Draw(crack, center + new Vector2(0, 8), new Rectangle((int)(500 * h), 0, (int)(500 * shieldPerc), crack.Height), Color.White * (h - (shieldPerc - (1 - h))) * 5f, 0, crack.Size() * 0.5f, 1, SpriteEffects.None, 0);
                    }
                }
                Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
            }
            else if (immune)
            {
                spriteBatch.Draw(barLocked, center, new Rectangle(0, 0, 18 + (int)(500 * prog), bar1.Height), Color.Lerp(barColor, Color.White, 0.36f), 0, bar1.Size() * 0.5f, 1, SpriteEffects.None, 0);
            }


            spriteBatch.Draw(bar3, center, null, buttomColor, 0, bar1.Size() / 2, 1, SpriteEffects.None, 0);
            spriteBatch.Draw(bar1, center, null, Color.White, 0, bar1.Size() / 2, 1, SpriteEffects.None, 0);

            if (npc.GetBossHeadTextureIndex() >= 0)
            {
                Texture2D headBoss = TextureAssets.NpcHeadBoss[npc.GetBossHeadTextureIndex()].Value;
                spriteBatch.Draw(headBoss, center + new Vector2(0, -14), null, Color.White, 0, headBoss.Size() / 2, 1, SpriteEffects.None, 0);
            }
            spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
            Texture2D hl = HlTex.Value;
            Texture2D df = DfTex.Value;
            Texture2D atk = AtkTex.Value;
            Vector2 statDrawPos = center + new Vector2(-170, -30);
            Main.spriteBatch.Draw(hl, statDrawPos, null, Color.White, 0, hl.Size() / 2, 1, SpriteEffects.None, 0);
            string dstring = life.ToString() + "/" + lifeMax.ToString() + "(" + ((int)(((float)life / (float)lifeMax) * 100)).ToString() + "%)";

            Main.spriteBatch.DrawString(CalamityEntropy.efont2, dstring, statDrawPos + new Vector2(6, 0), Color.Yellow, 0, CalamityEntropy.efont2.MeasureString(dstring) / 2, 0.45f, SpriteEffects.None, 0);

            statDrawPos.X += 105 + 45 + 4 + 146;

            Main.spriteBatch.Draw(df, statDrawPos, null, Color.White, 0, df.Size() / 2, 1, SpriteEffects.None, 0);
            float dr = CECal.GetDisplayDR(npc) * EGlobalNPC.DamageReduceMult(npc);
            dstring = dr > 0f
                ? npc.defense.ToString() + "(-" + (int)(dr * 100f) + "%)"
                : npc.defense.ToString();

            if (drawShield)
            {
                dstring = ((int)Shield).ToString() + "(" + ((int)(shieldPerc * 100)).ToString() + "%)";
                Main.spriteBatch.DrawString(CalamityEntropy.efont2, dstring, statDrawPos + new Vector2(10, 0), new Color(190, 160, 255), 0, CalamityEntropy.efont2.MeasureString(dstring) / 2, 0.5f, SpriteEffects.None, 0);
            }
            else
            {
                Main.spriteBatch.DrawString(CalamityEntropy.efont2, dstring, statDrawPos + new Vector2(8, 0), Color.Yellow, 0, CalamityEntropy.efont2.MeasureString(dstring) / 2, 0.44f, SpriteEffects.None, 0);
            }
            statDrawPos.X += 70 + 33;

            Main.spriteBatch.Draw(atk, statDrawPos, null, Color.White, 0, atk.Size() / 2, 1, SpriteEffects.None, 0);
            dstring = npc.damage.ToString();
            Main.spriteBatch.DrawString(CalamityEntropy.efont2, dstring, statDrawPos + new Vector2(8, 0), Color.Yellow, 0, CalamityEntropy.efont2.MeasureString(dstring) / 2, 0.5f, SpriteEffects.None, 0);

            string name = npc.FullName;
            Color tColor = getNpcBarColor(npc);
            if (!bossbarColor.ContainsKey(npc.type))
            {
                tColor = new Color(126, 135, 255);
            }
            for (int i = 0; i < 36; i++)
            {
                Main.spriteBatch.DrawString(CalamityEntropy.efont1, name, center + new Vector2(0, 28) + new Vector2(2, 0).RotatedBy(MathHelper.ToRadians(i * 10)), new Color(tColor.R / 2, tColor.G / 2, tColor.B / 2), 0, CalamityEntropy.efont1.MeasureString(name) / 2 * new Vector2(1, 0), 1.4f, SpriteEffects.None, 0);
            }

            if ((Math.Abs(tColor.R - buttomColor.R / 2) + Math.Abs(tColor.G - buttomColor.G / 2) + Math.Abs(tColor.B - buttomColor.B / 2)) / 3 < 90)
            {
                if (whiteLerp < 1)
                {
                    whiteLerp += 0.05f;
                }
            }
            else
            {
                if (whiteLerp > 0)
                {
                    whiteLerp -= 0.05f;
                }
            }
            Main.spriteBatch.DrawString(CalamityEntropy.efont1, name, center + new Vector2(0, 28), tColor * 1.1f, 0, CalamityEntropy.efont1.MeasureString(name) / 2 * new Vector2(1, 0), 1.4f, SpriteEffects.None, 0);
            spriteBatch.UseSampleState_UI(SamplerState.AnisotropicClamp);



        }
    }
}
