using CalamityEntropy.Common;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ActiveAcc
    {
        public int timeleft;
        public int index;
        public ActiveAcc(int id)
        {
            index = id;
            timeleft = Main.rand.Next(3, 30) * 60;
        }
    }
    public class WispLanternProj : ModProjectile
    {
        public int maxAccs = 1;
        public List<ActiveAcc> accs = new List<ActiveAcc>();
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
        }
        public int accChangeCd = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(accs.Count);
            for (int i = 0; i < accs.Count; i++)
            {
                writer.Write(accs[i].index);
                writer.Write(accs[i].timeleft);
            }
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            accs.Clear();
            maxAccs = reader.ReadInt32();
            for (int i = 0; i < maxAccs; i++)
            {
                accs.Add(new ActiveAcc(reader.ReadInt32()) { timeleft = reader.ReadInt32() });
            }
        }
        public override void AI()
        {

            Player player = Projectile.owner.ToPlayer();
            Vector2 targetPos = player.Center + new Vector2(100 * player.direction, -100);
            if (player.dead)
            {
                Projectile.Kill();
                return;
            }
            Projectile.velocity = (targetPos - Projectile.Center) * 0.14f;
            if (player.Entropy().visualWispLantern)
            {
                Lighting.AddLight(Projectile.Center, 1.2f, 0.8f, 1.2f);
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (Main.GameUpdateCount % 60 == 0)
                {
                    Projectile.netUpdate = true;
                }
                for (int i = accs.Count - 1; i >= 0; i--)
                {
                    if (accs[i].timeleft-- <= 0)
                    {
                        accs.RemoveAt(i);
                        Projectile.netUpdate = true;
                    }
                }
                accChangeCd--;
                if (accChangeCd <= 0 || maxAccs == 0)
                {
                    maxAccs = 1;
                    if (Main.rand.NextBool(3))
                    {
                        maxAccs++;
                    }
                    if (Main.rand.NextBool(4))
                    {
                        maxAccs++;
                    }
                    accChangeCd = Main.rand.Next(3, 16) * 60;
                    Projectile.netUpdate = true;
                }
                List<Item> CanApply = new List<Item>();
                List<int> index = new List<int>();
                int accCountInv = 0;
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];

                    if (item.active && item.accessory)
                    {
                        bool skip = false;
                        /*foreach (ActiveAcc acc in accs)
                        {
                            if (player.inventory[acc.index].type == item.type)
                            {
                                skip = true;
                                break;
                            }
                        }*/
                        foreach (var pp in CanApply)
                        {
                            if (pp.type == item.type)
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip)
                        {
                            continue;
                        }
                        accCountInv++;
                        index.Add(i);
                        CanApply.Add(item);
                    }
                }
                if (maxAccs > accCountInv)
                {
                    maxAccs = accCountInv;
                }
                while (accs.Count > maxAccs)
                {
                    accs.RemoveAt(0);
                }
                while (accs.Count < maxAccs)
                {
                    int id = Main.rand.Next(0, CanApply.Count);
                    accs.Add(new ActiveAcc(index[id]));
                    index.RemoveAt(id);
                    CanApply.RemoveAt(id);
                    Projectile.netUpdate = true;
                }
            }
            if (player.Entropy().accWispLantern || player.Entropy().visualWispLantern)
            {
                Projectile.timeLeft = 2;
            }
            for (int i = accs.Count - 1; i >= 0; i--)
            {
                if (!player.inventory[accs[i].index].active || !player.inventory[accs[i].index].accessory)
                {
                    accs.RemoveAt(i);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.netUpdate = true;
                    }
                }
            }
        }
        public void applyEffects()
        {
            Player player = Projectile.owner.ToPlayer();
            List<Item> applied = new List<Item>();
            foreach (ActiveAcc ac in accs)
            {
                if (player.Entropy().accWispLantern)
                {
                    bool f = true;
                    foreach (Item i in applied)
                    {
                        if (!ItemLoader.CanAccessoryBeEquippedWith(player.inventory[ac.index], i))
                            f = false;
                    }
                    if (f)
                    {
                        player.ApplyEquipFunctional(player.inventory[ac.index], !player.Entropy().visualWispLantern);
                        applied.Add(player.inventory[ac.index]);
                    }
                }
                if (player.Entropy().visualWispLantern)
                {
                    player.ApplyEquipVanity(player.inventory[ac.index]);
                }
            }

        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float yoffset = (float)Math.Cos(Main.GameUpdateCount * 0.05f) * 9;
            if (Projectile.owner.ToPlayer().Entropy().visualWispLantern)
            {
                Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + Vector2.UnitY * yoffset, null, Color.White, Projectile.rotation, tex.Size() / 2, Projectile.scale, SpriteEffects.None);
                List<Item> items = new List<Item>();
                foreach (var ac in accs)
                {
                    int i = ac.index;
                    Item item = Projectile.owner.ToPlayer().inventory[i];
                    items.Add(item);
                }
                float rot = Projectile.owner.ToPlayer().Entropy().CasketSwordRot * 0.3f;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Wisp", AssetRequestMode.ImmediateLoad).Value;
                shader.Parameters["minColor"].SetValue(new Color(40, 6, 100).ToVector4());
                shader.Parameters["maxColor"].SetValue(new Color(255, 220, 255).ToVector4());
                sb.End();
                sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);

                shader.CurrentTechnique.Passes["EnchantedPass"].Apply();

                for (int i = 0; i < items.Count; i++)
                {
                    Item item = items[i];
                    Vector2 pos = Projectile.Center - Main.screenPosition + rot.ToRotationVector2() * 56;


                    ItemWispEffectGlobalItem.checkItemColor(item);

                    shader.Parameters["min"].SetValue(item.Entropy().wispColor[0]);
                    shader.Parameters["max"].SetValue(item.Entropy().wispColor[1]);


                    Texture2D itex = TextureAssets.Item[item.type].Value;
                    Main.spriteBatch.Draw(itex, pos + Vector2.UnitY * yoffset, item.GetFrame(itex), Color.White, 0, new Vector2(item.GetFrame(itex).Width, item.GetFrame(itex).Height) / 2, (14f / item.GetFrame(itex).Width + 14f / item.GetFrame(itex).Height), SpriteEffects.None, 0);

                    rot += MathHelper.ToRadians(360f / (float)(items.Count));

                }
                sb.End();
                sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D light = CEUtils.getExtraTex("lightball");
                Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition + Vector2.UnitY * yoffset, null, new Color(200, 160, 255) * (0.6f + (float)(Math.Cos(Main.GameUpdateCount * 0.07f)) * 0.05f), Projectile.rotation, light.Size() / 2, Projectile.scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            }

            return false;
        }
    }

}