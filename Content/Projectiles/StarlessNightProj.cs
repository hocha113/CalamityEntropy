using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class StarlessNightProj : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/StarlessNight";
        List<float> odr = new List<float>();
        List<float> ods = new List<float>();
        public int[] NPCHitCounts = new int[Main.npc.Length];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 36;
            Projectile.ArmorPenetration = 80;
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 3;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rotSpeed);

        }
        public int spawnVoidStarCount = 5;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            NPCHitCounts[target.whoAmI]++;
            CalamityEntropy.Instance.screenShakeAmp = 6;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), 0, 0, Projectile.owner);
            CEUtils.PlaySound("he" + (Main.rand.NextBool() ? 1 : 3).ToString(), Main.rand.NextFloat(0.7f, 1.3f), Projectile.Center, volume: 0.7f * CEUtils.WeapSound);
            if (spawnVoidStarCount > 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = CEUtils.randomRot().ToRotationVector2() * 16;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, vel, ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 5, 1, Projectile.owner).ToProj().DamageType = DamageClass.Melee;
                }
                spawnVoidStarCount--;
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            rotSpeed = reader.ReadSingle();
        }
        public float scaleD = 0.64f;
        public float rotSpeed = 0f;
        public float rotSpeedJ = 0;
        float glowalpha = 0;
        public bool playsound1 = true;
        public bool playsound2 = true;
        public override void AI()
        {

            float updates = Projectile.MaxUpdates + 1;
            if (Projectile.ai[0] == 0)
            {
                Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.rotation -= 2.42f * Projectile.direction;
            }
            Player owner = Projectile.owner.ToPlayer();
            float meleeSpeed = owner.GetTotalAttackSpeed(Projectile.DamageType);
            if (Projectile.ai[0] >= 64 * updates && playsound1)
            {
                playsound1 = false;
                CEUtils.PlaySound("sn_swing", Main.rand.NextFloat(0.6f, 0.8f), Projectile.Center, volume: CEUtils.WeapSound);
            }
            if (Projectile.ai[0] >= 74 * updates && playsound2)
            {
                playsound2 = false;
                CEUtils.PlaySound("sn_swing", Main.rand.NextFloat(0.8f, 1f), Projectile.Center, volume: CEUtils.WeapSound);
            }
            Projectile.Center = owner.MountedCenter + owner.gfxOffY * Vector2.UnitY;
            Projectile.rotation += rotSpeed * meleeSpeed;
            if (Projectile.ai[0] < 60 * updates)
            {
                Projectile.ai[0] = 60 * updates;
            }
            else
            {
                if (Projectile.ai[0] < 86 * updates)
                {
                    rotSpeed += 0.00121f * Projectile.direction * meleeSpeed;
                }
                else
                {
                    rotSpeed *= (float)Math.Pow(0.94, 1.0 / meleeSpeed);
                    if (Projectile.ai[0] > 90 * updates)
                    {
                        if (Projectile.owner == Main.myPlayer)
                        {
                            Projectile.direction = (Main.MouseWorld - owner.Center).X > 0 ? 1 : -1;
                            float targetrot = (Main.MouseWorld - owner.Center).ToRotation() - 2.42f * Projectile.direction;
                            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetrot, 0.07f * meleeSpeed, false);
                        }
                    }
                    if (Projectile.ai[0] > 100 * updates)
                    {
                        owner.itemTime = 0;
                        owner.itemAnimation = 0;
                        Projectile.Kill();
                        return;
                    }
                }
            }
            Projectile.ai[0] += meleeSpeed;
            if (odr.Count > 0)
            {
                odr.Add(CEUtils.RotateTowardsAngle(odr[odr.Count - 1], Projectile.rotation, 0.5f, false));
                ods.Add(scaleD);
            }
            odr.Add(Projectile.rotation);
            ods.Add(scaleD);
            if (odr.Count > 84)
            {
                odr.RemoveAt(0);
                ods.RemoveAt(0);
            }
            if (odr.Count > 84)
            {
                odr.RemoveAt(0);
                ods.RemoveAt(0);
            }
            if (Projectile.velocity.X > 0)
            {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (NPCHitCounts[target.whoAmI] > 1)
            {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            flag = true;
            drawSlash();
            flag = false;
            return false;
        }
        public void drawSword()
        {
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Player player = Main.player[Projectile.owner];
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(CEUtils.getExtraTex("StarlessNightGlow"), Projectile.owner.ToPlayer().MountedCenter - Main.screenPosition, null, new Color(180, 180, 255) * glowalpha * 0.8f, Projectile.rotation + (float)Math.PI * 0.25f, new Vector2(32, 168), Projectile.scale * 3f * scaleD, SpriteEffects.None, 0);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(TextureAssets.Projectile[Projectile.type].Value, Projectile.owner.ToPlayer().MountedCenter - Main.screenPosition, null, Color.White, Projectile.rotation + (float)Math.PI * 0.25f, new Vector2(0, TextureAssets.Projectile[Projectile.type].Value.Height), Projectile.scale * 2.86f * scaleD, SpriteEffects.None, 0);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public bool flag = false;
        public void drawSlash()
        {
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Player player = Main.player[Projectile.owner];

            Texture2D tail = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/MotionTrail2").Value;
            Texture2D tail2 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/GradientNoise").Value;
            var r = Main.rand;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(255, 255, 255) * (i / (float)odr.Count);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(680 * ods[i] * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3(i / (float)odr.Count, 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(0 * ods[i] * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3(i / (float)odr.Count, 0, 1),
                      b));
            }

            if (ve.Count >= 3)
            {
                Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/SlashTrans", AssetRequestMode.ImmediateLoad).Value;
                Main.instance.GraphicsDevice.Textures[1] = CEUtils.getExtraTex("sn_colormap");
                shader.CurrentTechnique.Passes["EnchantedPass"].Apply();

                sb.End();
                sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);

                gd.Textures[0] = tail2;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                if (flag)
                {
                    gd.Textures[0] = tail;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    gd.Textures[0] = tail;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                sb.End();
                sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);

            }

            sb.End();
            sb.Begin(0, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 628 * Projectile.scale * scaleD, targetHitbox, 64);
        }
    }

}