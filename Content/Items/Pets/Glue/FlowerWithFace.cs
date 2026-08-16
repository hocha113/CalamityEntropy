using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Projectiles.Pets.Desert;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.NPCs.VanillaNPCAIOverrides.RegularEnemies.RevengeanceAndDeathAI;

namespace CalamityEntropy.Content.Items.Pets.Glue
{
    public class FlowerWithFace : ModItem, IDevItem
    {
        public string DevName => "Flowery";

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<Flowery>();
            Item.buffType = ModContent.BuffType<FloweryBuff>();
            Item.UseSound = null;
            Item.noUseGraphic = true;
            Item.useStyle = -1;
            Item.rare = ItemRarityID.Yellow;
            Item.width = Item.height = 40;
            Item.value = Item.buyPrice(0, 0, 20, 0);
        }
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Sunflower)
                .AddIngredient(ItemID.ReflectiveGoldDye)
                .AddCondition(Mod.GetLocalization("DeltaruneCH5ShadowCrystal"), () => ShadowCrystalDeltarune.Ch5Crystal)
                .Register();
        }
    }
    public class FloweryBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        
        public override void Update(Player player, ref int buffIndex)
        {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<Flowery>());
        }
    }
    public class Flowery : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 32;
            Projectile.height = 102;
        }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = Projectile.GetOwner().Center.Y > 32 + Projectile.height / 2 + Projectile.Center.Y;
            return true;
        }
        public int dir = 1;
        public enum AnimationStyle
        {
            Walk,
            Walk2,
            Fly,
            Run,
            Teleport
        }
        public AnimationStyle anm = AnimationStyle.Walk;
        public float Counter = 0;
        public int Frame = 0;
        public class OldStats
        {
            public Vector2 Center;
            public Texture2D Texture;
            public Rectangle Frame;
            public SpriteEffects spriteEffects;
            public Vector2 offset;
            public OldStats(Vector2 center, Texture2D texture, Rectangle frame, SpriteEffects spriteEffects, Vector2 of)
            {
                Center = center;
                Texture = texture;
                Frame = frame;
                this.spriteEffects = spriteEffects;
                this.offset = of;
            }
        }
        public List<OldStats> oldStats = new List<OldStats>();
        public Texture2D GetTex()
        {
            if (JaronaC > 0)
            {
                return this.getTextureAlt("Jarona2");
            }
            if (JaronaTarget > -1)
            {
                return Counter > 0.3f ? this.getTextureAlt("Jarona1") : this.getTextureAlt("Jarona0");
            }
            if (anm == AnimationStyle.Walk2)
            {
                return this.getTextureAlt("WalkB");
            }
            if (anm == AnimationStyle.Fly)
            {
                return this.getTextureAlt("Fly");
            }
            if (anm == AnimationStyle.Run)
            {
                return this.getTextureAlt("Run");
            }
            return Projectile.GetTexture();
        }
        public Rectangle GetFrame(Texture2D tex)
        {
            int Max = 4;
            if (anm == AnimationStyle.Fly)
            {
                Max = 5;
            }
            if (anm == AnimationStyle.Run)
            {
                Max = 8;
            }
            if (JaronaC > 0)
            {
                Max = 1;
            }
            if (JaronaTarget > -1)
            {
                Max = 1;
            }
            int fh = tex.Height / Max;
            int fc = Frame % Max;
            return new Rectangle(0, fh * fc, tex.Width, fh - 2);
        }
        public int VoiceClipCounter = Main.rand.Next(300, 1200);
        public int ImFallingCooldown = 0;
        public int Walk2 = 0;
        public int JaronaTarget = -1;
        public Vector2 lamp = Vector2.Zero;
        public int JaronaC = 0;
        public SpriteEffects SprEf => dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            player.Entropy().floweryPosition = Projectile.Center;
            VoiceClipCounter--;
            ImFallingCooldown--;
            if(VoiceClipCounter <= 0 && !Main.dedServ)
            {
                VoiceClipCounter = Main.rand.Next(600, 1800);
                PlayRandomVoiceClip();
            }
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.position.Y -= 80;
                PlayRandomVoiceClip();
            }
            if (player.dead || (player.HasBuff(ModContent.BuffType<FloweryBuff>())))
            {
                Projectile.timeLeft = 3;
            }
            if(CEUtils.getDistance(Projectile.Center, player.Center) > 3800)
            {
                anm = AnimationStyle.Teleport;
                Counter = 0;
                Projectile.velocity *= 0;
                Projectile.Center = player.Center + new Vector2(180 * (Main.rand.NextBool() ? 1 : -1), player.height / 2 - Projectile.height / 2);
                lamp = Projectile.Center;
            }
            Projectile.tileCollide = anm != AnimationStyle.Fly;
            if (JaronaTarget > -1)
                Projectile.tileCollide = false;
            if (JaronaTarget > -1)
            {
                Frame = 0;
                anm = AnimationStyle.Fly;
                NoTransformTime = 100;
                if (Counter >= 1 || !JaronaTarget.ToNPC().active)
                {
                    JaronaTarget = -1;
                    return;
                }
                dir = (JaronaTarget.ToNPC().Center.X - Projectile.Center.X) > 0 ? 1 : -1;
                if (Counter == 0f)
                {
                    Projectile.velocity = new Vector2(dir * -24, 0);
                }
                if (Counter > 0.3f)
                {
                    Projectile.velocity = (JaronaTarget.ToNPC().Center - Projectile.Center).normalize() * 40;
                    if (Projectile.Center.getRectCentered(100, 100).Intersects(JaronaTarget.ToNPC().Hitbox))
                    {
                        JaronaC = 30;
                        if (Main.dedServ)
                            return;
                        ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Projectile.velocity.normalize() * -2, 2), Main.LocalPlayer.Distance(Projectile.Center));
                        if (Main.myPlayer == Projectile.owner)
                        {
                            int dmg = 9;
                            if (NPC.downedBoss1 || NPC.downedSlimeKing)
                                dmg = 19;
                            if (NPC.downedBoss2)
                                dmg = 29;
                            if (NPC.downedBoss3)
                                dmg = 39;
                            if (NPC.downedBoss3)
                                dmg = 49;
                            if (Main.hardMode)
                                dmg = 59;
                            if ((NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) || NPC.downedPlantBoss || DownedBossSystem.downedCalamitasClone)
                                dmg = 99;
                            if (NPC.downedGolemBoss)
                                dmg = 219;
                            if (NPC.downedMoonlord)
                                dmg = 329;
                            if (DownedBossSystem.downedProvidence)
                                dmg = 579;
                            if (DownedBossSystem.downedDoG)
                                dmg = 729;
                            if (DownedBossSystem.downedCalamitas || DownedBossSystem.downedExoMechs)
                                dmg = 999;
                            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), player, JaronaTarget.ToNPC().Center, dmg, 160, DamageClass.Generic).ArmorPenetration = dmg;
                        }
                        for (int i = 0; i < 9; i++)
                            CombatText.NewText(JaronaTarget.ToNPC().getRect(), Color.Gold, 999, true);
                        CEUtils.PlaySound("HIT", 1, Projectile.Center);
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center + Projectile.velocity.normalize() * 10, Projectile.velocity.normalize() * 4, Color.Gold, "CalamityMod/Particles/SoftRoundExplosion", new Vector2(0.3f, 1f), Projectile.velocity.ToRotation(), 0.05f, 0.16f, 24), true);
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center + Projectile.velocity.normalize() * 15, Projectile.velocity.normalize() * 7, Color.Gold, "CalamityMod/Particles/SoftRoundExplosion", new Vector2(0.2f, 1f), Projectile.velocity.ToRotation(), 0.05f, 0.2f, 24), true);
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center + Projectile.velocity.normalize() * 20, Projectile.velocity.normalize() * 10, Color.Gold, "CalamityMod/Particles/SoftRoundExplosion", new Vector2(0.15f, 1f), Projectile.velocity.ToRotation(), 0.05f, 0.24f, 24), true);
                        for(int i = 0; i < 32; i++)
                        {
                            GeneralParticleHandler.SpawnParticle(new AltLineParticle(Projectile.Center + Projectile.velocity, -Projectile.velocity.RotatedByRandom(1.9f) * Main.rand.NextFloat(0.4f, 1f), false, Main.rand.Next(26, 32), Main.rand.NextFloat(1.5f, 2f), new Color(255, 235, 175)));
                        }
                        Projectile.velocity *= -1;
                        JaronaTarget = -1;
                        return;
                    }
                }
                else
                {
                    Projectile.velocity *= 0.9f;
                }
                Counter += 0.02f;
            }
            else
            {
                JaronaC--;
                if (JaronaC < 0)
                {
                    if (anm != AnimationStyle.Teleport && Main.rand.NextBool(10))
                    {
                        NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1200);
                        if (VoiceClipCounter < 500)
                            VoiceClipCounter = 500;
                        if (target != null)
                        {
                            CEUtils.PlaySound("VoiceClips/Jarona" + Main.rand.Next(0, 4), 1, Projectile.Center);
                            Counter = 0;
                            JaronaTarget = target.whoAmI;
                            return;
                        }
                    }
                }
                if (anm == AnimationStyle.Fly)
                {
                    Vector2 targetPos = player.Center + new Vector2(-120 * player.direction, -100);
                    Projectile.velocity *= 0.84f;
                    Projectile.velocity += (targetPos - Projectile.Center) * 0.01f;
                    if(JaronaC <= 0)
                        dir = Projectile.Center.X < player.Center.X ? 1 : -1;
                    Counter += 0.1f;
                    if (Counter >= 1)
                    {
                        Counter = 0;
                        Frame++;
                    }
                    if (NoTransformTime <= 0 && CEUtils.getDistance(Projectile.Center, player.Center) < 300 && JaronaC <= 0)
                    {
                        if (!CEUtils.CheckSolidTile(Projectile.getRect()))
                        {
                            if (CEUtils.CheckSolidTileOrPlatform((Projectile.Center + new Vector2(0, 120)).getRectCentered(48, 120)))
                            {
                                anm = AnimationStyle.Walk;
                                Projectile.velocity *= 0.2f;
                                Projectile.velocity.Y = -6;
                                PlayRandomVoiceClip();
                            }
                        }
                    }
                }
                if (anm == AnimationStyle.Walk || anm == AnimationStyle.Walk2 || anm == AnimationStyle.Run)
                {
                    anm = AnimationStyle.Walk;
                    if (Walk2 != 0)
                        anm = AnimationStyle.Walk2;
                    if (Projectile.velocity.Y >= 14)
                    {
                        if (ImFallingCooldown <= 0)
                        {
                            ImFallingCooldown = 10 * 60;
                            CEUtils.PlaySound("VoiceClips/ImFalling");
                        }
                    }
                    if (CEUtils.getDistance(Projectile.Center, player.Center) > 750 || (CEUtils.getDistance(Projectile.Center, player.Center) > 600 && Projectile.velocity.Y > 16))
                    {
                        Projectile.velocity *= 0;
                        anm = AnimationStyle.Fly;
                        PlayRandomVoiceClip();
                        return;
                    }
                    float ms = 0.3f;
                    if (Math.Abs(Projectile.Center.X - player.Center.X) > 400)
                    {
                        ms = 0.9f;
                        anm = AnimationStyle.Run;
                    }
                    if (Projectile.velocity.Y < -0.5f && Projectile.velocity.Length() < 10)
                    {
                        ms = 1;
                    }
                    if (Projectile.Center.X < player.Center.X - 200)
                    {
                        Walk2 = 0;
                        Projectile.velocity.X += ms;
                    }
                    else if (Projectile.Center.X > player.Center.X + 200)
                    {
                        Walk2 = 0;
                        Projectile.velocity.X -= ms;
                    }
                    else
                    {
                        if (Walk2 == 0 && Main.rand.NextBool(180))
                            Walk2 = Math.Sign(player.Center.X - Projectile.Center.X);
                    }
                    Projectile.velocity.X += Walk2 * 0.2f;
                    Projectile.velocity.X *= 0.93f;
                    if (CEUtils.CheckSolidTile((Projectile.Center + new Vector2(Projectile.velocity.X * 5, 0)).getRectCentered(120, Projectile.height / 2 - 4)))
                    {
                        if (Projectile.Distance(player.Center) > 260)
                        {
                            if (Projectile.velocity.Y == 0)
                                Projectile.velocity.Y = -12;
                        }
                    }
                    if (Projectile.velocity.Y < 40)
                        Projectile.velocity.Y += 0.38f;
                    if (JaronaC <= 0)
                        dir = Projectile.velocity.X > 1 ? 1 : (Projectile.velocity.X < -1 ? -1 : dir);
                    if (Projectile.velocity.Length() < 2)
                    {
                        Frame = 0;
                    }
                    else
                    {
                        Counter += Math.Abs(Projectile.velocity.X) * (anm == AnimationStyle.Run ? 0.02f : 0.04f);
                        if (Counter >= 1)
                        {
                            Counter = 0;
                            Frame++;
                        }
                    }
                }
            }
            if (anm == AnimationStyle.Teleport)
            {
                if (VoiceClipCounter < 500)
                    VoiceClipCounter = 500;
                oldStats.Clear();
                Counter += 0.005f;
                if (Main.GameUpdateCount % 6 == 0)
                    Frame++;
                if (Counter >= 1)
                {
                    Counter = 0;
                    anm = AnimationStyle.Walk;
                    PlayRandomVoiceClip();
                }
                if (Counter >= 0.05f && Counter <= 0.95f)
                    Projectile.velocity.X = 0.36f;
                else
                    Projectile.velocity *= 0;
            }
            else
            {
                if (!Main.dedServ)
                {
                    var nt = GetTex();
                    oldStats.Add(new OldStats(Projectile.Center, nt, GetFrame(nt), SprEf, new Vector2(0, anm == AnimationStyle.Run ? 12 : -2)));
                    if (oldStats.Count > 20)
                        oldStats.RemoveAt(0);
                }
            }
            NoTransformTime--;
        }
        public int NoTransformTime = 0;
        public override void OnKill(int timeLeft)
        {
            CEUtils.ExplotionParticleLOL(Projectile.Center);
        }
        public void PlayRandomVoiceClip()
        {
            CEUtils.PlaySound("VoiceClips/Random" + Main.rand.Next(0, 25), 1, Projectile.Center);
        }
        public static Effect wt;
        public override bool PreDraw(ref Color lightColor)
        {
            if (wt == null)
                wt = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/WhiteTrans", AssetRequestMode.ImmediateLoad).Value;
            var dofs = new Vector2(0, anm == AnimationStyle.Run ? 12 : -2);
            Vector2 drawPos = Projectile.Center + dofs;
            Texture2D tex = GetTex();
            Rectangle rect = GetFrame(tex);
            SpriteEffects ef = SprEf;
            float ta = float.Min(1, Projectile.velocity.Length() * 0.03f);
            Main.spriteBatch.End();
            wt.Parameters["strength"].SetValue(1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, wt, Main.GameViewMatrix.TransformationMatrix);
            wt.CurrentTechnique.Passes[0].Apply();
            for (int i = 0; i < oldStats.Count; i++)
            {
                Color clr = Main.hslToRgb((Main.GameUpdateCount * 0.03f + i * 0.018f) % MathHelper.PiOver2, 1f, 0.5f) * 1f * ta * (i / (oldStats.Count + 1f));
                Rectangle df = oldStats[i].Frame;
                Main.spriteBatch.Draw(oldStats[i].Texture, oldStats[i].Center + oldStats[i].offset - Main.screenPosition, oldStats[i].Frame, clr, 0, oldStats[i].Frame.Size() * 0.5f, Projectile.scale * 2, oldStats[i].spriteEffects, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            if(anm == AnimationStyle.Teleport)
            {
                float lalpha = 1;
                if(Counter < 0.05f)
                {
                    lalpha = Counter / 0.05f;
                }
                if(Counter > 0.95f)
                {
                    lalpha = 1 - ((Counter - 0.95f) / 0.05f);
                }
                float fp = Utils.Remap(Counter, 0.1f, 0.9f, 0, 1);
                Texture2D slamp = this.getTextureAlt("Streetlight");
                if (Counter >= 0.1f)
                {
                    drawPos.X -= (1 - fp) * 20;
                    int w = int.Min(tex.Width, (int)(drawPos.X - lamp.X));
                    if(w < 0)
                        w = 0;
                    rect.X = rect.Width - w;
                    rect.Width = w;
                    Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, rect, lightColor, 0, rect.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None, 0);
                }
                Main.EntitySpriteDraw(slamp, lamp - Main.screenPosition + new Vector2(0, -10), null, Color.White * lalpha, 0, slamp.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None, 0);
                return false;
            }
            Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, rect, lightColor, 0, rect.Size() * 0.5f, Projectile.scale * 2, ef, 0);
            return false;
        }
    }
}