using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityEntropy.Content.NPCs.Prophet;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class CruiserLaserMouth : ModProjectile
    {
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }
        public int counter = 0;
        List<Vector2> p = new List<Vector2>();
        List<Vector2> l = new List<Vector2>();
        public int length = 6000;
        NPC ownern = null;
        public float width = 0;
        public int aicounter = 0;
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 800;

        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
        public override void AI()
        {
            if (aicounter == 0)
            {
                if (Projectile.ai[1] > 0)
                {
                    Projectile.timeLeft = (int)Projectile.ai[1];
                }
            }
            if (Projectile.ai[0] >= 0 && ownern == null)
            {
                ownern = ((int)Projectile.ai[0]).ToNPC();
            }
            // 虚无双子的激光不再整段推镜:每帧叠加一枚朝向玩家的定向震动,会把视角持续顶离 Boss 并来回晃,影响走位。
            // 改为只在光束点燃瞬间给一次短促无向震动,光束具备碰撞(width>=0.7)前就衰减完
            if (ownern?.ModNPC is NihilityActeriophage)
            {
                if (aicounter == 0)
                    ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.NoDirQuickShake(5f), Projectile.Center, 1800);
            }
            else
            {
                CEUtils.SetShake(Projectile.Center, 9, 1800);
            }
            if (Projectile.ai[0] >= 0)
            {
                if (ownern != null && ownern.active)
                {
                    Projectile.Center = ownern.Center + (ownern.ModNPC is NihilityActeriophage ? ownern.rotation.ToRotationVector2() * 30 : Vector2.Zero);
                    Projectile.rotation = ownern.rotation;
                    if (ownern.ModNPC is TheProphet)
                    {
                        Projectile.rotation = ownern.velocity.ToRotation();
                    }
                }
                else
                {
                    Projectile.Kill();
                }
            }
            if (ownern != null)
            {
                if (!ownern.HasValidTarget)
                {
                    if (Projectile.timeLeft > 30)
                    {
                        Projectile.timeLeft = 30;
                    }
                    width -= 1f / 60f;
                }
            }
            if (Projectile.timeLeft < 60)
            {
                width -= 1f / 60f;
            }
            else
            {
                if (width < (ownern.ModNPC is NihilityActeriophage ? 0.8f : 1))
                {
                    width += 1f / 60f;
                }
            }
            if (width < 0)
            {
                width = 0;
            }
            aicounter++;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return width >= 0.7f && CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * length, targetHitbox, 38);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            counter++;
            var rand = Main.rand;
            int tspeed = 34;
            if (counter % 1 == 0)
            {
                p.Add(new Vector2(0, rand.Next(0, 41) - 20));
            }
            if (counter % 6 == 0)
            {
                l.Add(new Vector2(0, rand.Next(0, 17) - 8));
            }
            for (int i = 0; i < p.Count; i++)
            {
                p[i] = p[i] + new Vector2(tspeed, 0);
            }
            for (int i = 0; i < l.Count; i++)
            {
                l[i] = l[i] + new Vector2(tspeed, 0);
            }
            for (int i = 0; i < p.Count; i++)
            {
                if (p[i].X > length)
                {
                    p.RemoveAt(i);
                    break;
                }
            }
            for (int i = 0; i < l.Count; i++)
            {
                if (l[i].X > length)
                {
                    l.RemoveAt(i);
                    break;
                }
            }
            Texture2D tb = CEExtraAssets.clback;
            Texture2D px = CEExtraAssets.white;
            Texture2D tl = CEExtraAssets.cllight;
            Texture2D tl2 = CEExtraAssets.cllight2;
            Texture2D th = CEExtraAssets.clinghth;
            Main.spriteBatch.Draw(tb, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(0, tb.Height / 2), new Vector2(length, width), SpriteEffects.None, 0);
            foreach (Vector2 ps in p)
            {
                CEUtils.drawLine(Main.spriteBatch, px, Projectile.Center + (ps * new Vector2(1, width)).RotatedBy(Projectile.rotation), Projectile.Center + ((ps * new Vector2(1, width)) + new Vector2(26, 0)).RotatedBy(Projectile.rotation), Color.White * 0.6f, 2 * width);
            }
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tl2, Projectile.Center - Main.screenPosition, null, new Color(160, 160, 255) * 0.8f, Projectile.rotation, new Vector2(0, tl2.Height / 2), new Vector2(length, width * 1.2f), SpriteEffects.None, 0);

            foreach (Vector2 ps in l)
            {
                Main.spriteBatch.Draw(tl, Projectile.Center + (ps * new Vector2(1, width)).RotatedBy(Projectile.rotation) - Main.screenPosition, null, new Color(160, 160, 255), Projectile.rotation, tl.Size() / 2, new Vector2(1.5f, 1.5f * width), SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(th, Projectile.Center - Main.screenPosition, null, new Color(160, 160, 255) * 0.5f, Projectile.rotation, new Vector2(0, th.Height / 2), new Vector2(1, width), SpriteEffects.None, 0);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}