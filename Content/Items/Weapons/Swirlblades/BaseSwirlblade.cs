using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public abstract class BaseSwirlblade : ModProjectile
    {
        public static void ApplyShader(Color color)
        {
            Effect shader = CommonEffects.colorLerp;
            Main.spriteBatch.End();
            shader.Parameters["color"].SetValue(color.ToVector4());
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, true, -1);
            Projectile.width = Projectile.height = 24;
            Projectile.localNPCHitCooldown = 6;
        }
        public float BladeScale = 0;
        public virtual int BladeOpenTime => 7;
        public Player player => Projectile.GetOwner();
        public bool Spreaded = false;
        public int Counter { get { return (int)Projectile.ai[2]; } set { Projectile.ai[2] = value; } }
        public virtual float Radius => 80;
        public virtual int FlyTime => Projectile.MaxUpdates * 14;
        public virtual int SpreadTime => 17 * Projectile.MaxUpdates;
        public virtual int TimeUtilSpread => Counter - FlyTime - SpreadTime;
        public List<Vector2> oldPos = new List<Vector2>();
        public virtual bool CollideWithNPC => true;
        public virtual void OnCollideWithNPC(NPC npc) { }
        public virtual int OldPosLength => 9;
        public virtual void FlyBack()
        {
            Projectile.velocity *= 1f - float.Min(TimeUtilSpread, 26) * 0.006f;
            Projectile.velocity += (player.MountedCenter - Projectile.Center).normalize() * float.Min(TimeUtilSpread, 26) * 0.47f;
            if (Projectile.Distance(player.MountedCenter) <= Projectile.velocity.Length() * 1.05f + 16)
            {
                BackKill();
                Projectile.velocity = (player.MountedCenter - Projectile.Center);
            }
        }
        public virtual void BackKill()
        {
            if (Projectile.timeLeft > 2)
                Projectile.timeLeft = 2;
        }
        public virtual Rectangle CollisionRect => Projectile.Center.getRectCentered(Radius * 0.7f, Radius * 0.7f);
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * Projectile.velocity.Length() * 0.1f;
            if (Counter < FlyTime && CollideWithNPC)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && !npc.dontTakeDamage)
                    {
                        if (CollisionRect.Intersects(npc.getRect()))
                        {
                            Counter = FlyTime;
                            OnCollideWithNPC(npc);
                            if(Projectile.owner == Main.myPlayer)
                                CEUtils.SyncProj(Projectile.whoAmI);
                            break;
                        }
                    }
                }
            }
            if (Counter >= FlyTime && Counter <= FlyTime + SpreadTime)
            {
                if (!Spreaded)
                {
                    Projectile.velocity *= 0;
                    Spreaded = true;
                    OnSpread();
                }
            }
            if(Counter > FlyTime + SpreadTime)
            {
                if(Spreaded)
                {
                    Spreaded = false;
                    Projectile.velocity += CEUtils.randomPointInCircle(42);
                    oldPos.Clear();
                    OnRetract();
                }
                else
                {
                    FlyBack();
                }
            }
            if(Counter >= FlyTime && Counter < FlyTime + SpreadTime)
            {
                float t = ((float)Counter - FlyTime) / (float)BladeOpenTime;
                BladeScale = float.Min(1f, CEUtils.Parabola(Utils.Clamp(t, 0, 1) * 0.5f, 1));
            }
            if(Counter > FlyTime + SpreadTime)
            {
                if (BladeScale > 0)
                    BladeScale -= 1f / BladeOpenTime;
                if (BladeScale < 0)
                    BladeScale = 0;
            }
            if (Spreaded)
                Projectile.rotation += Math.Sign(Projectile.Center.X - player.Center.X) * 0.54f;
            Counter++;

            oldPos.Add(Projectile.Center + Projectile.velocity);
            if (oldPos.Count > OldPosLength)
                oldPos.RemoveAt(0);
        }
        public override bool? CanDamage()
        {
            return Spreaded ? null : false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(projHitbox.Center.ToVector2(), Radius * Projectile.scale * BladeScale).Intersects(targetHitbox);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center + Vector2.UnitX * -Radius * Projectile.scale, Projectile.Center + Vector2.UnitX * Radius * Projectile.scale, Radius * 2 * Projectile.scale, DelegateMethods.CutTiles);
        }
        public virtual void OnSpread()
        { }
        public virtual void OnRetract()
        { }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }
            if (Counter > FlyTime + SpreadTime + 60)
                Projectile.tileCollide = false;
            if(oldPos.Count > 0)
            {
                oldPos.RemoveAt(oldPos.Count - 1);
                oldPos.Add(Projectile.Center + Projectile.velocity);
            }
            return false;
        }
    }
}