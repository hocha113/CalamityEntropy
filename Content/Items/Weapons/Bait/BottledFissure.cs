using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.GameContent.Animations.IL_Actions.Sprites;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class BottledFissure : ModItem, IBaitItem
    {
        public static int TagDamage = 25;
        public static float DamageMult = 0.8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 230;
            Item.knockBack = 0;
            Item.shootSpeed = 44;
            Item.useAnimation = Item.useTime = 36;
            Item.value = CalamityGlobalItem.RarityDarkBlueBuyPrice;
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.width = 60;
            Item.height = 60; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            var snd = CEUtils.GetSound("SwingMid", 1, 8);
            snd.PitchRange = (0.5f, 0.75f);
            snd.Volume = 0.65f;
            Item.UseSound = snd;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<BottledFissureProjectile>();
            Item.autoReuse = true;
        }
        public override bool CanUseItem(Player player)
        {
            return player.Entropy().BaitUsable;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, (int)(damage * DamageMult), knockback, player.whoAmI, 0, 0, TagDamage);
            return false;
        }

        public override void AddRecipes()
        {
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class BottledFissureProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<BottledFissure>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 32;
            Projectile.light = 1;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.GetOwner().Entropy().BaitCharge--;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (StickNPC < 0)
            {
                if (Counter > 16)
                {
                    Projectile.velocity.Y += 1f;
                }
            }
            else
            {
                NPC npc = StickNPC.ToNPC();
                if (!npc.active)
                {
                    Projectile.Kill();
                    return;
                }
                Main.player[Projectile.owner].MinionAttackTargetNPC = npc.whoAmI;
                npc.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 2;
                if (IsActive)
                {
                    Projectile.GetOwner().Calamity().mouseWorldListener = true;
                    npc.GetGlobalNPC<WhipDebuffNPC>().ClearBaitTags();
                    npc.GetGlobalNPC<WhipDebuffNPC>().Tags.Add(new WhipTag(this.GetType().Name, 5, this.TagDamage, 1, 0, this.GetType().Name) { IsABaitTag = true });
                }
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if (ActiveCounter > 150)
                {
                    if (IsActive)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                        SetActive();
                        Projectile.Kill();
                    }
                }
            }
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
            if (StickNPC >= 0)
            {
                oldPos.Clear();
            }
            else
            {
                for (float i = 0; i < 1; i += 0.1f)
                {
                    oldPos.Add(Projectile.Center + Projectile.velocity * i);
                    if (oldPos.Count > 80)
                        oldPos.RemoveAt(0);
                }
            }
        }
        public override void ActiveEffect(float damageMul)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * 46, ModContent.ProjectileType<VoidEater>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner);
                }
            }
            if (!Main.dedServ)
            {
                for (int i = 0; i < 5; i++)
                {
                    float r = 0;
                    float scale = 0.4f + 0.7f * i;
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 60, 255), new Color(160, 160, 255), (i / 5f)), "CalamityEntropy/Assets/Extra/ShatteredExplosion", Vector2.One, r, scale * 0.005f, scale * 0.06f, 12 + i * 2, true, 1));
                }
                for (int i = 0; i < 40; i++)
                {
                    var p = new Particles.Particle();
                    p.position = Projectile.Center;
                    p.alpha = Main.rand.NextFloat(0.8f, 1.2f);
                    p.shape = 4;
                    p.vd = 0.92f;
                    p.velocity = CEUtils.randomPointInCircle(14);
                    VoidParticles.particles.Add(p);
                }
            }
        }
        public float activeEffectAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            float pn = ActiveCounter / 150f;
            if (activeEffectAlpha >= 0.01f)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Texture2D pulse = CEUtils.getExtraTex("ShatteredExplosion");
                for (float i = 0; i < 1f; i += 0.2f)
                {
                    float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly * 12);
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.LightBlue * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.2f * pn, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseAdditiveClamp();
            for (int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, Color.White * p * 0.5f, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage()
        {
            return StickNPC == -1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.tileCollide = false;
            OnHitEffect(Projectile.Center);
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            for (int i = 0; i < 5; i++)
            {
                float r = 0;
                float scale = 0.4f + 0.7f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 60, 255), new Color(160, 160, 255), (i / 5f)), "CalamityEntropy/Assets/Extra/ShatteredExplosion", Vector2.One, r, scale * 0.005f, scale * 0.06f, 12 + i * 2, true, 1));
            }
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("crystalsound2", Main.rand.NextFloat(1f, 1.3f), pos);
        }
        public override void OnKill(int timeLeft)
        {
            if(timeLeft > 0 && !Main.dedServ)
            {
                OnHitEffect(Projectile.Center);
            }
        }
    }
    public class VoidEater : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4500;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 40;
            Projectile.localNPCHitCooldown = 16;
            Projectile.timeLeft = 580;
            Projectile.MaxUpdates = 2;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 30;
        }
        public bool InGround = true;
        public static int Segments = 10;
        public Vector2 PortalPos;
        public float PortalAlpha = 1;
        public bool Hide = false;
        public override bool? CanDamage()
        {
            return !Hide;
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
                PortalPos = Projectile.Center;
            if (Projectile.timeLeft < 18)
                Projectile.Opacity -= 1 / 18f;
            Player player = Projectile.GetOwner();
            if (!Hide)
            {
                NPC target = Projectile.FindMinionTarget();
                if (target != null)
                {
                    if (Projectile.localAI[2] > 12 && Projectile.timeLeft > 60)
                    {
                        AttackTarget(target);
                    }
                }
            }
            if (Projectile.localAI[2]++ > 12)
            {
                PortalAlpha *= 0.98f;
            }
            if(Projectile.timeLeft == 180)
            {
                PortalAlpha = 1;
                PortalPos = Projectile.Center + Projectile.velocity.normalize() * 200;
            }
            if(Projectile.timeLeft < 180)
            {
                Projectile.velocity = (PortalPos - Projectile.Center).normalize() * 46;
                if(CEUtils.getDistance(Projectile.Center, PortalPos) < 60)
                {
                    Hide = true;
                }
            }
            if (Hide)
                Projectile.velocity *= 0;
            Projectile.rotation = Projectile.velocity.ToRotation();
            for(float i = 0; i < 1f; i += 0.2f)
            {
                oldPos.Add(Projectile.Center + Projectile.velocity * i);
                if (oldPos.Count > 100)
                    oldPos.RemoveAt(0);
            }
        }
        internal ref float Time => ref base.Projectile.ai[0];

        internal ref float FlyAcceleration => ref base.Projectile.ai[1];
        internal void AttackTarget(NPC target)
        {
            float num = 0.18f;
            Vector2 center = target.Center;
            float num2 = base.Projectile.Distance(center);
            if (base.Projectile.Distance(center) > 725f)
            {
                center += (Time % 30f / 30f * (MathF.PI * 2f)).ToRotationVector2() * 145f;
                num2 = base.Projectile.Distance(center);
                num *= 2.5f;
            }

            if (num2 > 600f && Time > 30f)
            {
                num = MathHelper.Min(6f, FlyAcceleration + 1f);
            }

            FlyAcceleration = MathHelper.Lerp(FlyAcceleration, num, 0.3f);
            float num3 = Vector2.Dot(base.Projectile.velocity.SafeNormalize(Vector2.Zero), base.Projectile.SafeDirectionTo(center));
            if (num2 > 200f)
            {
                float num4 = base.Projectile.velocity.Length();
                if (num4 < 23f)
                {
                    num4 += 0.08f;
                }

                if (num4 > 32f)
                {
                    num4 -= 0.08f;
                }

                if (num3 < 0.85f && num3 > 0.5f)
                {
                    num4 += 6f;
                }

                if (num3 < 0.5f && num3 > -0.7f)
                {
                    num4 -= 10f;
                }

                num4 = MathHelper.Clamp(num4, 16f, 34f) * 1.3f;
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(base.Projectile.AngleTo(center), FlyAcceleration * 1.3f).ToRotationVector2() * num4;
            }
            Projectile.ai[2]--;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EGlobalNPC.AddVoidTouch(target, 60, 4, 600, 12);
            CEUtils.PlaySound("DoGLaserWallSpawn", 1.2f, Projectile.Center, 16, 0.4f);
            CEUtils.PlaySound("DnBite", 1.2f, Projectile.Center, 12, 0.36f);
            for (int i = 0; i < 12; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(CEUtils.randomPoint(target.getRect()), Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(26, 32), true, 24, Main.rand.NextFloat(1.2f, 1.6f) * 0.04f, Color.LightBlue, new Vector2(0.2f, 1f)));
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return null;
        }
        public void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f)
        {
            Main.spriteBatch.End();
            Effect effect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Vortex", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(22);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEUtils.getExtraTex("VoronoiShapes"), pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEUtils.getExtraTex("VoronoiShapes").Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
            CEUtils.DrawGlow(pos, Color.White * 0.4f * glow, 0.8f * Size * glow);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public override bool PreDraw(ref Color lightColor)
        {
            float Scale = 1.8f;
            DrawVortex(PortalPos, new Color(200, 180, 255) * PortalAlpha, Scale * PortalAlpha * 2);
            DrawVortex(PortalPos, Color.White * PortalAlpha, Scale * 0.6f * PortalAlpha * 2);
            Main.spriteBatch.UseAdditiveClamp();
            Texture2D g = CEUtils.getExtraTex("Circle");
            Main.spriteBatch.Draw(g, PortalPos - Main.screenPosition, null, new Color(80, 80, 255) * PortalAlpha, 0, g.Size() * 0.5f, new Vector2(1, 0.06f) * 0.6f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(g, PortalPos - Main.screenPosition, null, new Color(255, 255, 255) * PortalAlpha, 0, g.Size() * 0.5f, new Vector2(1, 0.06f) * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(g, PortalPos - Main.screenPosition, null, new Color(80, 80, 255) * PortalAlpha, 0, g.Size() * 0.5f, new Vector2(0.06f, 1) * 0.6f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(g, PortalPos - Main.screenPosition, null, new Color(255, 255, 255) * PortalAlpha, 0, g.Size() * 0.5f, new Vector2(0.06f, 1) * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            Texture2D gt = CEUtils.getExtraTex("lightball");
            CEUtils.DrawGlow(PortalPos, Color.Black * PortalAlpha, 0.4f * Scale, false, gt);
            CEUtils.DrawGlow(PortalPos, Color.Black * PortalAlpha, 0.4f * Scale, false, gt);
            Main.spriteBatch.ExitShaderRegion();
            if (Hide)
                return false;

            Main.spriteBatch.UseAdditiveClamp();
            Texture2D tex = CEUtils.getExtraTex("lightball");
            for(int i = 0; i < oldPos.Count; i++)
            {
                float ap = (i + 1f) / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, new Color(90, 90, 255) * ap, Projectile.velocity.ToRotation(), tex.Size() / 2, new Vector2(1 + (Projectile.velocity.Length() * 0.01f), (1f / (1 + (Projectile.velocity.Length() * 0.01f)))) * 0.36f * ap * (Projectile.ai[0] == 1 ? 1 : 0.8f), SpriteEffects.None, 0);
                ap += 1f / (float)oldPos.Count;
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public void DrawSeg(Texture2D tex, Vector2 pos, Rectangle? frame, float rot, Vector2 origin, Color color, bool outline = false)
        {
            if (outline)
            {
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
                {
                    Main.EntitySpriteDraw(tex, pos + i.ToRotationVector2() * 3 - Main.screenPosition, frame, color * Projectile.Opacity, rot + MathHelper.Pi, origin, Projectile.scale, SpriteEffects.None);
                }
            }
            else
            {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color * Projectile.Opacity, rot + MathHelper.Pi, origin, Projectile.scale, SpriteEffects.None);
            }
        }
    }
}
