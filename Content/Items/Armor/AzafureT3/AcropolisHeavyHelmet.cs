using CalamityEntropy.Common;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.AzafureT3
{
    [AutoloadEquip(EquipType.Head)]
    public class AcropolisHeavyHelmet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.value = Item.buyPrice(platinum: 1);
            Item.defense = 22;
            Item.rare = ItemRarityID.Red;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == Type && body.type == ModContent.ItemType<AcropolisHeavyArmor>() && legs.type == ModContent.ItemType<AcropolisHeavyLegArmor>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Mod.GetLocalization("AzafureSet3").Value.Replace("[KEY]", CEKeybinds.AcropolisMechTransformation.TooltipKeyHint());
            player.GetModPlayer<AcropolisArmorPlayer>().ArmorSetBonus = true;
            // 潜行体系退役:原潜行条(上限1.2)按容量×10%换算为大招充能速度;灾厄肾上腺素抑制随 ripper 系统退役删除
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.12f;

            player.maxMinions += 1;
            player.noKnockback = true;
            var mp = player.GetModPlayer<AcropolisArmorPlayer>();
            if (mp.MechFrame > 18)
            {
                if (player.channel)
                    player.channel = false;
                if (mp.DummyCannon == null)
                {
                    mp.DummyCannon = new Item();
                    mp.DummyCannon.SetDefaults(0);
                }
                mp.DummyCannon.stack = 0;
                mp.DummyCannon.type = 0;
                if (Main.myPlayer == player.whoAmI)
                {
                    if (Main.mouseItem.type != 0 && Main.mouseItem.stack > 0)
                        Main.LocalPlayer.QuickSpawnItem(null, Main.mouseItem, Main.mouseItem.stack);
                    Main.mouseItem = mp.DummyCannon;
                }
                ItemIO.Load(player.inventory[58], ItemIO.Save(mp.DummyCannon));
                player.selectedItem = 58;
                //player.itemTime = player.itemAnimation = 2;

            }
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.AddCritDamage(DamageClass.Generic, 0.1f);
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_UnholyEssence))
            {
                CreateRecipe()
                .AddIngredient<AzafureSteamKnightHelmet>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddIngredient(CEID.Item_UnholyEssence, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AzafureSteamKnightHelmet>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class AcropolisArmorPlayer : ModPlayer
    {
        #region parts
        public class AcropolisLeg
        {
            public Vector2 StandPoint = Vector2.Zero;
            public float Scale = 1f;
            public Player Player;
            public Vector2 offset;
            public int NoMoveTime = 0;
            public Vector2 targetPos;
            public int SoundCD = 0;
            public AcropolisLeg(Player plr, Vector2 offset, float scale = 1)
            {
                Player = plr;
                this.offset = offset;
                this.Scale = scale;
                StandPoint = plr.Center;
            }
            bool o = false;
            public bool OnTile => !CEUtils.isAir(StandPoint, true) && o;
            public bool sound = true;
            public bool Update()
            {
                SoundCD--;
                if (CEUtils.getDistance(StandPoint, targetPos) < ms * (Player.velocity.Y > 1f ? 3 : 1))
                {
                    StandPoint = targetPos;
                    if (sound)
                    {
                        sound = false;
                        if (!CEUtils.isAir(targetPos + new Vector2(0, 2), true))
                        {
                            if (SoundCD <= 0)
                            {
                                CEUtils.SpawnExplotionFriendly(Player.GetSource_FromThis(), Player, targetPos + new Vector2(0, -10), 220, 32, DamageClass.Generic);
                                SoundCD = 6;
                                CEUtils.PlaySound("mechStep", Main.rand.NextFloat(1.6f, 2f), StandPoint, 32, 0.16f);
                            }
                        }
                    }
                }
                else
                {
                    StandPoint += (targetPos - StandPoint).normalize() * ms * (Player.velocity.Y > 0.5f ? 3 : 1);
                }
                NoMoveTime--;
                float distToMove = 100;
                if (Player.velocity.Y < -4)
                {
                    o = false;
                    targetPos = Player.Center + new Vector2(offset.X * 0.4f, 140);
                    ms = CEUtils.getDistance(targetPos, StandPoint) * 0.25f;
                    return false;
                }
                if (!OnTile || (NoMoveTime <= 0 && CEUtils.getDistance(StandPoint, Player.Center + Player.velocity * 8 + (offset * 1).RotatedBy(Player.direction > 0 ? Player.fullRotation : (Player.fullRotation))) > distToMove) || CEUtils.getDistance(StandPoint, Player.Center + Player.velocity * 16 + (offset * 1).RotatedBy(Player.fullRotation)) > distToMove * 1.8f)
                {
                    targetPos = FindStandPoint(Player.Center + Player.velocity * 8 + (offset * 1).RotatedBy(Player.fullRotation) + new Vector2(Math.Sign(Player.velocity.X) == Math.Sign(offset.X) ? (Math.Sign(Player.velocity.X) * 12) : 0, 0), 85 * 1, 160);
                    ms = CEUtils.getDistance(targetPos, StandPoint) * 0.25f;
                    if (NoMoveTime < 7)
                        NoMoveTime = 7;
                    if (!CEUtils.isAir(targetPos + new Vector2(0, 2), true))
                    {
                        sound = true;
                    }
                    return true;
                }
                return false;
            }
            public float ms;
            public bool LastOnTile = false;
            public static bool CanStandOn(Vector2 pos)
            {
                return !CEUtils.isAir(pos, true);
            }
            public Vector2 FindStandPoint(Vector2 center, float MaxOffset, float MaxTry = 64)
            {
                o = false;
                for (int i = 0; i < MaxTry; i++)
                {
                    Vector2 pos = CEUtils.randomPointInCircle(MaxTry) * new Vector2(1f, 1f) + center;
                    if (CEUtils.getDistance(pos, center) <= MaxOffset * 0.9f && CanStandOn(pos))
                    {
                        o = true;
                        Vector2 orgPos = pos;
                        int c = 128;
                        while (CanStandOn(pos))
                        {
                            c--;
                            pos.Y -= 2;
                            if (c <= 0)
                            {
                                return orgPos;
                            }
                        }
                        pos.Y += 2;
                        return pos;
                    }
                }
                return Player.Center + new Vector2(offset.X, 200).RotatedBy(Player.fullRotation);
            }
        }
        public class Hand
        {
            public float Seg1RotV = 0;
            public float Seg1Length = 0;
            public float Seg1Rot = 0;
            public float Seg2Rot = 0;
            public float Seg1MaxRadians = MathHelper.ToRadians(50);
            public Vector2 offset;
            public Player Player;
            public Hand(Player n, Vector2 offset, float seg1Length, float seg1Rot, float seg2Rot)
            {
                Player = n;
                Seg1Length = seg1Length;
                Seg1Rot = seg1Rot;
                Seg2Rot = seg2Rot;
                this.offset = offset;
            }
            public Vector2 TopPos => seg1end + Seg2Rot.ToRotationVector2() * 60;
            public void PointAPos(Vector2 pos, float r = 0.06f)
            {
                Seg1Rot = CEUtils.RotateTowardsAngle(Seg1Rot, (pos - (Player.Center + (offset * new Vector2(1, 1)).RotatedBy(Player.fullRotation))).ToRotation(), 0.06f, false);
                if (CEUtils.GetAngleBetweenVectors(Seg1Rot.ToRotationVector2(), -Vector2.UnitY) > Seg1MaxRadians * 2)
                {
                    if (Seg1Rot > (MathHelper.PiOver2 + Seg1MaxRadians))
                    {
                        Seg1Rot = (MathHelper.PiOver2 + Seg1MaxRadians);
                    }
                    if (Seg1Rot < (MathHelper.PiOver2 - Seg1MaxRadians))
                    {
                        Seg1Rot = (MathHelper.PiOver2 - Seg1MaxRadians);
                    }
                }
                Seg2Rot = CEUtils.RotateTowardsAngle(Seg2Rot, (pos - seg1end).ToRotation(), r, false);
            }

            public void Update()
            {
                Seg1Rot += Seg1RotV;
                Seg1RotV *= 0.96f;
            }
            public Vector2 seg1end => Player.Center + (offset * new Vector2(1, 1)).RotatedBy(Player.fullRotation) + Seg1Rot.ToRotationVector2() * Seg1Length;
        }
        #endregion
        public bool ArmorSetBonus = false;
        public float durability = 1;
        public int DurabilityRegenDelay = 0;
        public bool DurabilityActive = true;
        public int DeathExplosion = -1;
        public int DeathExplosionCD = 0;
        public bool ExplosionFlag = false;
        public PlayerDeathReason dmgSource = null;
        public LoopSound chargeSnd = null;
        public bool MechTrans = false;
        public void SmokeParticle()
        {
            CEUtils.PlaySound("chainsaw_break", 1.3f, Player.Center, 6, 0.6f);
            for (int i = 0; i < 16; i++)
            {
                //EMediumSmoke走AlphaBlend桶,蒸汽甲那套和Wulfrum枪口烟一样
                PRTLoader.NewParticle<PRT_EMediumSmoke>(Player.Center + CEUtils.randomPointInCircle(12), CEUtils.randomPointInCircle(16), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(1f, 2f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), 120);
            }
        }
        public void DeactiveMech()
        {
            if (MechTrans)
            {
                Player.AddCooldown(AcropolisCooldown.ID, (int)(1f * 60 * 60));
                SmokeParticle();
                DurabilityActive = false;
                durability = 0;
                MechTrans = false;
                Player.velocity.Y = -20;
                Player.velocity.X *= 2;
                Player.Entropy().immune = 90;
                MechSync();
            }
        }
        public int MechFrame = 0;
        public int MechFrameCounter = 0;
        public void MechSync()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)CEMessageType.AcropolisTrans);
                packet.Write(Player.whoAmI);
                packet.Write(MechTrans);
                packet.Write(Reload);
                packet.Write(Bullet);
                packet.Write(SlashP);
                packet.Write(slashDir);
                packet.Write(CannonMode);
                packet.Send();
            }
        }
        public List<AcropolisLeg> legs = null;
        public Hand cannon;
        public Hand harpoon;
        public void UpdateParts()
        {
            if (legs == null || (!Main.dedServ && CEKeybinds.AcropolisMechTransformation.JustPressed))
            {
                legs =
                [
                    new AcropolisLeg(Player, new Vector2(-44, 100), 0.8f),
                    new AcropolisLeg(Player, new Vector2(44, 100), 0.8f),
                    new AcropolisLeg(Player, new Vector2(-66, 100), 1),
                    new AcropolisLeg(Player, new Vector2(66, 100), 1),
                ];
                cannon = new Hand(Player, new Vector2(-30, -2), 38, MathHelper.PiOver2, MathHelper.PiOver2);
                harpoon = new Hand(Player, new Vector2(30, -2), 38, MathHelper.PiOver2, MathHelper.PiOver2);
            }
            cannon.Update();
            harpoon.Update();
            // 脱离灾厄:跨端鼠标坐标改自研 MouseWorld(player-api.md §1),监听开关每帧置位
            Vector2 vec = Player.Entropy().MouseWorld;
            Player.Entropy().MouseWorldListener = true;
            if (SlashP != 0)
            {
                vec = Player.Center + (Player.Entropy().MouseWorld - Player.Center).RotatedBy(3 * slashDir * (SlashP - 0.5f));
            }
            cannon.PointAPos(vec, SlashP == 0 ? 0.06f : 1);
            harpoon.PointAPos(Player.Entropy().MouseWorld);
            foreach (var l in legs)
            {
                if (l.Update())
                {
                    foreach (var l2 in legs)
                    {
                        if (Math.Sign(l2.offset.X) == Math.Sign(l.offset.X))
                        {
                            if (l2.NoMoveTime < 7)
                            {
                                l2.NoMoveTime = 7;
                            }
                        }
                    }
                }
            }
        }
        public bool PlayerVisual => MechFrame < 16;
        public Vector2 CalculateLegJoints(Vector2 Center, Vector2 legStandPoint, float l1, float l2, float l3, out Vector2 P1, out Vector2 P2)
        {
            P1 = Vector2.Zero;
            P2 = Vector2.Zero;

            if (l1 <= 0 || l2 <= 0 || l3 <= 0)
            {
                return Center;
            }

            Vector2 D = legStandPoint - Center;
            float dist = D.Length();

            Vector2 target = legStandPoint;
            if (dist > l1 + l2 + l3)
            {
                target = Center + Vector2.Normalize(D) * (l1 + l2 + l3);
            }

            Vector2 downDirection = new Vector2(0, 1);
            Vector2 targetDirection = D.Length() > 0 ? Vector2.Normalize(D) : downDirection;

            float maxDeflectionAngle = MathHelper.ToRadians(68);
            float angleToTarget = (float)Math.Atan2(targetDirection.Y, targetDirection.X) - (float)Math.PI / 2; // 相对于Y轴正方向
            float deflectionAngle = MathHelper.Clamp(angleToTarget, -maxDeflectionAngle, maxDeflectionAngle);

            float cosAngle = (float)Math.Cos(deflectionAngle);
            float sinAngle = (float)Math.Sin(deflectionAngle);
            Vector2 firstSegmentDirection = new Vector2(
                downDirection.X * cosAngle - downDirection.Y * sinAngle,
                downDirection.X * sinAngle + downDirection.Y * cosAngle
            );

            P1 = Center + l1 * firstSegmentDirection;

            float y2 = target.Y - l3;
            float deltaY = y2 - P1.Y;
            float deltaX;
            try
            {
                deltaX = (float)Math.Sqrt(l2 * l2 - deltaY * deltaY);
            }
            catch
            {
                deltaX = 0;
                y2 = P1.Y - l2;
            }

            float x2_positive = P1.X + deltaX;
            float x2_negative = P1.X - deltaX;
            float x2 = (Math.Abs(x2_positive - target.X) < Math.Abs(x2_negative - target.X)) ? x2_positive : x2_negative;

            P2 = new Vector2(x2, y2);

            float distP2ToTarget = Vector2.Distance(P2, target);
            if (Math.Abs(distP2ToTarget - l3) > 0.001f)
            {
                P2 = new Vector2(P1.X, P1.Y - l2);
                target = new Vector2(P2.X, P2.Y + l3);
            }

            return target;
        }
        public void DrawMech()
        {
            if (MechFrame < 1)
                return;
            string folder = "CalamityEntropy/Content/Items/Armor/AzafureT3/";
            Texture2D body = CEUtils.RequestTex($"{folder}MechBody");
            Texture2D trans = CEUtils.RequestTex($"{folder}MechTrans");
            Texture2D arm = CEUtils.RequestTex($"{folder}Arm");
            Texture2D cannonTex = CEUtils.RequestTex($"{folder}CannonHand");
            Texture2D knife = CEUtils.RequestTex($"{folder}KnifeHand");
            Texture2D chain = CEUtils.RequestTex($"{folder}Chain");
            Texture2D gear = CEUtils.RequestTex($"{folder}Gear");
            Texture2D harpoonTex = CEUtils.RequestTex($"{folder}Harpoon");
            Texture2D harpoonCannon = CEUtils.RequestTex($"{folder}HarpoonCannon");
            Texture2D leg1 = CEUtils.RequestTex($"{folder}Leg1");
            Texture2D leg2 = CEUtils.RequestTex($"{folder}Leg2");
            Texture2D leg3 = CEUtils.RequestTex($"{folder}Leg3");
            Color drawColor = Lighting.GetColor((Player.Center / 16).ToPoint());

            int drawDir = (Player.Entropy().MouseWorld.X - Player.Center.X) > 0 ? 1 : -1;

            if (legs != null)
            {
                if (MechFrame == 19)
                {
                    foreach (var leg in legs)
                    {
                        float l1 = 40 * leg.Scale;
                        float l2 = 36 * leg.Scale;
                        float l3 = 66 * leg.Scale;
                        List<Vector2> points = new List<Vector2>();
                        points.Add(Player.Center + (new Vector2(Math.Sign(leg.offset.X) * 10, 26)).RotatedBy(Player.fullRotation));
                        Vector2 e = CalculateLegJoints(points[0], leg.StandPoint, l1, l2, l3, out var p1, out var p2);
                        points.Add(p1);
                        points.Add(CEUtils.GetCircleIntersection(p1, l2, leg.StandPoint, l3, leg.offset.X > 0, true));
                        points.Add(points[points.Count - 1] + (leg.StandPoint - points[points.Count - 1]).normalize() * l3);
                        SpriteEffects ef = leg.offset.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
                        Main.EntitySpriteDraw(leg1, points[0] - Main.screenPosition, null, drawColor, (points[1] - points[0]).ToRotation(), new Vector2(4, 13), 1 * leg.Scale, ef);
                        Main.EntitySpriteDraw(leg2, points[1] - Main.screenPosition, null, drawColor, (points[2] - points[1]).ToRotation(), new Vector2(6, 9), 1 * leg.Scale, ef);
                        Main.EntitySpriteDraw(leg3, points[2] - Main.screenPosition, null, drawColor, (points[3] - points[2]).ToRotation() + ((leg.offset.X > 0 ? 1 : -1) * MathHelper.ToRadians(34)), new Vector2(10, leg3.Height / 2f), 1 * leg.Scale, leg.offset.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                        //CEUtils.DrawLines(points, Color.Blue, 4);
                    }
                    Main.EntitySpriteDraw(gear, (harpoon.offset * new Vector2(drawDir, 1)).RotatedBy(drawDir > 0 ? Player.fullRotation : (Player.fullRotation + MathHelper.Pi)) + Player.Center - Main.screenPosition, null, drawColor, Main.GameUpdateCount * 0.15f, gear.Size() / 2f, 1, SpriteEffects.None);
                    Main.EntitySpriteDraw(gear, (cannon.offset * new Vector2(drawDir, 1)).RotatedBy(drawDir > 0 ? Player.fullRotation : (Player.fullRotation + MathHelper.Pi)) + Player.Center - Main.screenPosition, null, drawColor, -Main.GameUpdateCount * 0.15f, gear.Size() / 2f, 1, SpriteEffects.None);

                    Main.EntitySpriteDraw(arm, (harpoon.offset * new Vector2(drawDir, 1)).RotatedBy(drawDir > 0 ? Player.fullRotation : (Player.fullRotation + MathHelper.Pi)) + Player.Center - Main.screenPosition, null, drawColor, harpoon.Seg1Rot, new Vector2(6, arm.Height / 2f), 1, SpriteEffects.None);
                    Main.EntitySpriteDraw(arm, (cannon.offset * new Vector2(drawDir, 1)).RotatedBy(drawDir > 0 ? Player.fullRotation : (Player.fullRotation + MathHelper.Pi)) + Player.Center - Main.screenPosition, null, drawColor, cannon.Seg1Rot, new Vector2(6, arm.Height / 2f), 1, SpriteEffects.None);

                }
            }
            if (MechFrame == 19)
            {
                Main.EntitySpriteDraw(body, Player.Center - Main.screenPosition, null, drawColor, Player.fullRotation, body.Size() / 2f, 1, drawDir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<AcropolisHarpoon>()] == 0)
                {
                    Main.EntitySpriteDraw(harpoonTex, harpoon.TopPos + harpoon.Seg2Rot.ToRotationVector2() * 38 + new Vector2(0, 0 * drawDir).RotatedBy(harpoon.Seg2Rot) - Main.screenPosition, null, drawColor, harpoon.Seg2Rot, new Vector2(70, harpoonTex.Height / 2f), 1, drawDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                }
                var ht = CannonMode ? cannonTex : knife;
                int CannonFrameTotal = CannonMode ? 9 : 8;
                int frame = CannonFrame;

                int of = ht.Height / CannonFrameTotal;
                Rectangle rect = new Rectangle(0, of * frame, ht.Width, of - 2);
                Main.EntitySpriteDraw(harpoonCannon, harpoon.seg1end - Main.screenPosition, null, drawColor, harpoon.Seg2Rot, new Vector2(6, harpoonCannon.Height / 2f), 1, drawDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                int wo = CannonMode ? 84 : 110;

                Main.EntitySpriteDraw(ht, cannon.seg1end - Main.screenPosition, rect, drawColor, cannon.Seg2Rot + CannonRot, new Vector2(wo, cannonTex.Height / CannonFrameTotal / 2 - 1), 1, drawDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);

            }
            else
            {
                Main.EntitySpriteDraw(trans, Player.Center - Main.screenPosition, new Rectangle(0, (trans.Height / 19) * MechFrame, trans.Width, trans.Height / 19 - 2), drawColor, Player.fullRotation, new Vector2(trans.Width / 2f, (trans.Height / 19 - 2) / 2f), 1, drawDir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            }

        }
        public int CannonFrame = 0;
        public int LandTime = 0;
        public bool ControlHook = false;
        public int HarpoonDelay = 0;
        public override void SetControls()
        {
            ControlHook = Player.controlHook;
            if (MechTrans)
            {
                Player.controlHook = false;
                Player.controlMount = false;
            }
        }
        public Item DummyCannon = null;
        public void MechUpdate()
        {
            if (MechTrans)
            {
                if (Player.mount.Active)
                    Player.mount.Dismount(Player);
            }
            Player.noFallDmg = true;

            int MaxFrame = 19;
            if (MechTrans)
            {
                Player.gfxOffY = 0;
                if (!Main.dedServ)
                {
                    if (Main.myPlayer == Player.whoAmI && CEKeybinds.AcropolisMechTransformation.JustPressed)
                    {
                        DeactiveMech();
                    }
                }
                UpdateParts();
                HarpoonDelay--;
                if (MechFrame < MaxFrame)
                {
                    MechFrameCounter++;
                    if (MechFrameCounter > 4)
                    {
                        MechFrameCounter = 0;
                        MechFrame++;
                    }
                    Player.velocity *= 0;
                    Player.immuneNoBlink = true;
                    if (Player.Entropy().immune < 5)
                        Player.Entropy().immune = 5;
                }
                else
                {
                    Player.gravity = 0;
                    Player.direction = (Player.Entropy().MouseWorld.X - Player.Center.X) > 0 ? 1 : -1;
                    int s = 0;
                    float y = 0;
                    foreach (var leg in legs)
                    {
                        y += leg.StandPoint.Y;
                        if (leg.OnTile)
                            s++;
                    }
                    if (CEUtils.CheckSolidTile((Player.Center + new Vector2(0, 100)).getRectCentered(32, 32)))
                        s = 4;
                    y /= legs.Count;
                    if (s < 3)
                    {
                        Player.velocity.Y += (Player.wingTime > 0 && Player.controlJump) ? 0.2f : 0.5f;
                        LandTime = 0;
                    }
                    else
                    {
                        if (!Player.controlDown)
                        {
                            Player.position.Y += 4;
                            if (CEUtils.CheckSolidTile(Player.getRect()))
                            {
                                Player.position.Y -= 15;
                                if (CEUtils.CheckSolidTile(Player.getRect()))
                                    Player.position.Y += 15;
                            }
                            Player.position.Y -= 4;
                            Player.Center = new Vector2(Player.Center.X, float.Lerp(Player.Center.Y, y - 100, 0.1f));

                            Player.velocity.Y *= 0.9f;
                        }
                        else
                        {
                            Player.velocity.Y *= 0.96f;
                        }
                    }
                    if (Player.controlDown)
                    {
                        Player.velocity.Y += 0.65f;
                        if (Player.Entropy().NoPlatformCollide < 4)
                            Player.Entropy().NoPlatformCollide = 4;
                    }
                    if (s > 2)
                    {
                        if (Math.Abs(Player.velocity.X) > 9)
                            Player.velocity.X *= 0.95f;
                        if (LandTime == 2 && Player.velocity.Y > 5)
                        {
                            CEUtils.PlaySound("mechStepHeavy", Main.rand.NextFloat(1, 1.4f), Player.Center + new Vector2(0, 60), 6, Utils.Remap(Player.velocity.Y, 0, 10, 0, 1));
                        }
                        LandTime++;
                        if (LandTime > 6)
                        {
                            Player.wingTime = Player.wingTimeMax;
                        }
                        if (Player.controlUp)
                        {
                            Player.velocity.Y -= 0.3f;
                        }

                        if (Player.controlJump && LandTime > 8)
                        {
                            Player.velocity.Y = -24;
                        }
                    }
                    if (CannonMode && Bullet <= 0)
                    {
                        switchDelay = 4;
                        if (Reload-- == 0)
                        {
                            Bullet = 6;
                        }
                    }
                    if (!CannonMode && SlashP > 0)
                    {
                        SlashP += 0.15f * Player.GetTotalAttackSpeed(Player.GetBestClass());
                        if (SlashP > 1.25f)
                        {
                            ShootDelay = (int)(16f / Player.GetTotalAttackSpeed(Player.GetBestClass()));
                            SlashP = 0;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        if (ControlHook && !LastHook)
                        {
                            if (HarpoonDelay <= 0 && Player.ownedProjectileCounts[ModContent.ProjectileType<AcropolisHarpoon>()] == 0)
                            {
                                HarpoonDelay = 32;
                                harpoon.PointAPos(Player.Entropy().MouseWorld, 1);
                                int damage = ((int)(Player.GetTotalDamage(Player.GetBestClass()).ApplyTo(1500))).ApplyAccArmorDamageBonus(Player);
                                Projectile.NewProjectile(Player.GetSource_FromThis(), harpoon.TopPos, harpoon.Seg2Rot.ToRotationVector2() * 48, ModContent.ProjectileType<AcropolisHarpoon>(), damage, 12, Player.whoAmI);
                            }
                        }
                        if (!Player.mouseInterface && switchDelay-- <= 0)
                        {
                            if (Main.mouseLeft)
                            {
                                if (CannonMode)
                                {
                                    if (ShootDelay <= 0)
                                    {
                                        Bullet--;
                                        if (Bullet < 1)
                                            Reload = (int)(30f / Player.GetTotalAttackSpeed(Player.GetBestClass()));
                                        for (int i = 0; i < 12; i++)
                                            PRTLoader.NewParticle<PRT_LineCal>(cannon.TopPos, cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.3f) * 46 * Main.rand.NextFloat(), new Color(255, 100, 100), Main.rand.NextFloat(0.4f, 1)).Configure(false, 12);
                                        MechSync();
                                        cannon.PointAPos(Player.Entropy().MouseWorld, 1);
                                        CEUtils.PlaySound("AcropolisShoot", Main.rand.NextFloat(0.8f, 1.2f), cannon.TopPos);
                                        ShootDelay = (int)(5f / Player.GetTotalAttackSpeed(Player.GetBestClass()));
                                        int damage = ((int)(Player.GetTotalDamage(Player.GetBestClass()).ApplyTo(400))).ApplyAccArmorDamageBonus(Player);
                                        if (!Main.dedServ)
                                        {
                                            Gore.NewGoreDirect(Player.GetSource_FromThis(), cannon.TopPos - cannon.Seg2Rot.ToRotationVector2() * 46, Player.velocity + (cannon.Seg2Rot + Player.direction * -2.4f).ToRotationVector2() * 12 + CEUtils.randomPointInCircle(3), Mod.Find<ModGore>("AcropolisShell").Type).timeLeft = 0;
                                        }
                                        Projectile.NewProjectile(Player.GetSource_FromThis(), cannon.TopPos, cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.08f) * 43, ModContent.ProjectileType<AcropolisBullet>(), damage, 10, Player.whoAmI);
                                    }
                                }
                                else
                                {
                                    if (SlashP == 0 && ShootDelay <= 0)
                                    {
                                        SlashP += 0.01f;
                                        int damage = ((int)(Player.GetTotalDamage(Player.GetBestClass()).ApplyTo(1500))).ApplyAccArmorDamageBonus(Player);
                                        Projectile.NewProjectile(Player.GetSource_FromThis(), cannon.TopPos, cannon.Seg2Rot.ToRotationVector2() * 8, ModContent.ProjectileType<AcropolisSlash>(), damage, 10, Player.whoAmI);

                                        CEUtils.PlaySound("throw", Main.rand.NextFloat(1.2f, 1.5f), Player.Center, 10, 0.4f);
                                        if (slashDir == 0)
                                            slashDir = -1;
                                        slashDir *= -1;

                                        MechSync();
                                    }
                                }
                            }
                            if (Main.mouseRight)
                            {
                                switchDelay = (int)(30f / Player.GetTotalAttackSpeed(Player.GetBestClass()));
                                CannonFrame = 0;
                                CannonMode = !CannonMode;
                                MechSync();
                            }
                        }
                    }

                    CannonRot = float.Lerp(CannonRot, CannonMode ? 0 : MathHelper.Pi, 0.1f);
                }
                if (CannonMode)
                {
                    SlashP = 0;
                    CannonFrame = 0;
                    if (Bullet > 0)
                    {
                        CannonFrame = 2;
                    }
                    if (Bullet > 2)
                    {
                        CannonFrame = 1;
                    }
                    if (Bullet > 4)
                    {
                        CannonFrame = 0;
                    }
                    if (Reload > 0)
                    {
                        CannonFrame = 3 + (int)((1 - (Reload / 30f)) * 5);
                    }
                }
                else
                {
                    if (CannonFrame < 7)
                    {
                        if (Main.GameUpdateCount % 4 == 0)
                            CannonFrame++;
                    }
                }
            }
            else
            {
                if (!Main.dedServ)
                {
                    if (Main.myPlayer == Player.whoAmI && CEKeybinds.AcropolisMechTransformation.JustPressed)
                    {
                        if (!Player.HasCooldown(AcropolisCooldown.ID))
                        {
                            MechTrans = true;
                            CEUtils.PlaySound("WulfrumBastionActivate", 1, Player.Center);
                            DurabilityActive = true;
                            durability = 1;
                            MechSync();
                        }
                    }
                }
                MechFrame = MechFrameCounter = 0;
            }
            ShootDelay--;
            if (MechTrans && MechFrame < 16)
            {
                Player.gravity = 0;
                Player.velocity.Y = -0.6f;
            }
            LastHook = ControlHook;
            lastJump = Player.controlJump;
        }
        public bool LastHook = false;
        public bool lastJump = false;
        public bool CannonMode = true;
        public float CannonRot = 0;
        public int switchDelay = 0;
        public int Reload = 0;
        public int slashDir = 1;
        public int Bullet = 6;
        public float SlashP = 0;
        public override void PostUpdate()
        {
            if (!Main.dedServ)
            {
                if (chargeSnd != null && chargeSnd.timeleft <= 0)
                    chargeSnd = null;
                if (chargeSnd != null)
                {
                    chargeSnd.setVolume_Dist(Player.Center, 100, 1600, 1);
                    chargeSnd.instance.Pitch = (1 - (DeathExplosion / 80f)) * 2f + 1.9f;
                }
            }
        }
        public int ShootDelay = 0;
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            DeactiveMech();
            if (DeathExplosionCD <= 0 && !ExplosionFlag)
            {
                playSound = false;
            }
            if (ExplosionFlag && DeathExplosion > 0)
            {
                playSound = false;
                return false;
            }
            if (ArmorSetBonus && !ExplosionFlag && DeathExplosionCD <= 0)
            {
                //damageSource = PlayerDeathReason.ByCustomReason(Mod.GetLocalization("Death").ToNetworkText(Player.name));
            }
            return true;
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            DeactiveMech();
            if (ArmorSetBonus)
            {
                if (DeathExplosionCD <= 0 && !ExplosionFlag)
                {
                    ExplosionFlag = true;
                    DeathExplosionCD = 10 * 60;
                    DeathExplosion = 80;
                    dmgSource = damageSource;
                    Player.dead = false;
                    if (!Main.dedServ)
                    {
                        chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                        chargeSnd.instance.Pitch = 0;
                        chargeSnd.instance.Volume = 0;
                        chargeSnd.play();
                        chargeSnd.timeleft = 80;
                    }
                }
                else
                    ExplosionFlag = false;
                if (Main.myPlayer == Player.whoAmI && Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var mp = Mod.GetPacket();
                    mp.Write((byte)CEMessageType.SyncPlayerDead);
                    mp.Write(Player.whoAmI);
                    mp.Write(Player.dead);
                    mp.WriteVector2(Player.position);
                    mp.Send();
                }
            }
        }
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            return DeathExplosion < 0;
        }
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            return DeathExplosion < 0;
        }
        public override void ResetEffects()
        {
            ArmorSetBonus = false;
            if (DeathExplosion > 0 && ExplosionFlag)
            {
                DeathExplosion--;
                Player.velocity *= 0;
                Player.Entropy().noItemTime = 5;
                if (DeathExplosion < 70 && DeathExplosion % 2 == 0)
                {
                    if (DeathExplosion % 6 == 0)
                        ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, Utils.Remap(Main.LocalPlayer.Center.Distance(Player.Center), 4000, 1000, 0, 12)));

                    //T3自爆用ShockParticle2(Additive),跟T1 Heavy的NonPremultiplied ShockParticle不是一回事
                    PRTLoader.NewParticle<PRT_ShockParticle2>(Player.Center, Vector2.Zero, Color.White, 0.1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
                }
                if (DeathExplosion == 0 || DeathExplosion == 3 || DeathExplosion == 6 || DeathExplosion == 9 || DeathExplosion == 12 || DeathExplosion == 15 || DeathExplosion == 18 || DeathExplosion == 21)
                {
                    CEUtils.PlaySound("pulseBlast", 0.6f, Player.Center, 6, 1f);
                    CEUtils.PlaySound("blackholeEnd", 0.6f, Player.Center, 6, 1f);

                    PRTLoader.NewParticle<PRT_PulseRing>(Player.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(12f, 8);
                    PRTLoader.NewParticle<PRT_ShineParticle>(Player.Center, Vector2.Zero, Color.Firebrick, 20f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                    PRTLoader.NewParticle<PRT_ShineParticle>(Player.Center, Vector2.Zero, Color.White, 16f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                    ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 100), 1200);
                    var proj = CEUtils.SpawnExplotionFriendly(Player.GetSource_FromThis(), Player, Player.Center, ((int)(Player.GetBestClassDamage().ApplyTo(3500))).ApplyAccArmorDamageBonus(Player), 1200, DamageClass.Generic);
                    proj.ArmorPenetration = 60;
                    if (proj.ModProjectile is CommonExplotionFriendly cef)
                    {
                        cef.DamageMulToWormSegs = 0.16f;
                    }
                }
                if (DeathExplosion == 0)
                {
                    DeathExplosion = -1;
                    ExplosionFlag = false;
                    Player.KillMe(PlayerDeathReason.ByCustomReason(Mod.GetLocalization("DeathExplode").ToNetworkText(Player.name)), 10000, 0);
                    if (Main.myPlayer == Player.whoAmI && Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        var mp = Mod.GetPacket();
                        mp.Write((byte)CEMessageType.SyncPlayerDead);
                        mp.Write(Player.whoAmI);
                        mp.Write(Player.dead);
                        mp.WriteVector2(Player.position);
                        mp.Send();
                    }
                }
            }
        }
        public override void PreUpdate()
        {
            if (ArmorSetBonus)
                MechUpdate();
            else
                MechFrame = 0;
        }
        public override void PostUpdateRunSpeeds()
        {
            if (ArmorSetBonus && MechTrans)
            {
                if (Player.moveSpeed > 1)
                {
                    Player.moveSpeed = 1 + (Player.moveSpeed - 1) * 0.2f;
                }
            }
        }
        public override void PostUpdateEquips()
        {
            if (!ExplosionFlag || DeathExplosion == 0)
                DeathExplosion = -1;
            DeathExplosionCD--;
            if (ArmorSetBonus)
            {
                Player.Entropy().moveSpeed += 0.1f;
                DurabilityRegenDelay--;
                if (DurabilityActive)
                {
                    Player.Entropy().EDamageReduce += durability * 0.24f;
                    Player.statDefense += (int)(durability * 36);
                    Player.noKnockback = true;
                }
                else
                {
                    DurabilityRegenDelay = -1;
                }
                if (DurabilityRegenDelay <= 0)
                {
                    durability += 0.001f;
                    if (durability >= 1)
                    {
                        durability = 1;
                        if (!DurabilityActive)
                            CEUtils.PlaySound("AuricQuantumCoolingCellInstallNew", 0.7f, Player.Center);
                        DurabilityActive = true;
                    }
                }
                if (MechTrans)
                {
                    Player.Entropy().EDamageReduce += 0.10f;
                    Player.statDefense += 15;
                    Player.Entropy().FallSpeed += 0.5f;
                }
            }
            else
            {
                DeactiveMech();
            }
        }
        public override void OnHurt(Player.HurtInfo info)
        {
            if (ArmorSetBonus)
            {
                if (DurabilityActive)
                {
                    CEUtils.PlaySound($"ExoHit{Main.rand.Next(1, 5)}", Main.rand.NextFloat(0.6f, 0.8f), Player.Center, 6, 0.45f);
                    if (DurabilityRegenDelay < 5 * 60)
                        DurabilityRegenDelay = 5 * 60;
                    durability -= float.Min(0.36f, info.SourceDamage / (MechTrans ? 2800f : 1200f));

                    //耐久没了暂时失效
                    if (durability <= 0)
                    {
                        durability = 0;
                        DurabilityActive = false;
                        SmokeParticle();
                        DeactiveMech();
                    }
                }
            }
        }
        public static void DrawDuraBar(float dura)
        {
            Main.spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
            // 脱离灾厄:灾厄客户端配置不可用,耐久条固定挂在原肾上腺素条默认屏幕位
            Vector2 pos = new Vector2(35.77f, 8.85f);
            pos.X = (int)(pos.X * 0.01f * Main.screenWidth);
            pos.Y = (int)(pos.Y * 0.01f * Main.screenHeight);

            var mplayer = Main.LocalPlayer.GetModPlayer<AcropolisArmorPlayer>();
            Color color = mplayer.DurabilityActive ? Color.White : new Color(255, 80, 80) * 0.5f;
            Color color2 = mplayer.DurabilityActive ? Color.White : new Color(255, 142, 142) * 0.7f;
            Vector2 Center = pos;// Main.ScreenSize.ToVector2() * 0.5f + new Vector2(0, -60);
            if (dura < 0.32f && mplayer.DurabilityActive)
            {
                Center += new Vector2(Main.rand.NextFloat() * ((0.32f - dura) * 20), Main.rand.NextFloat() * ((0.32f - dura) * 20));
            }
            string folder = "CalamityEntropy/Content/Items/Armor/AzafureT3/";
            Texture2D tex1 = AzafureHeavyArmorPlayer.DuraBarFrontTex.Value;
            if (Main.LocalPlayer.TryGetModPlayer<AcropolisArmorPlayer>(out var mp) && mp.MechTrans)
            {
                tex1 = CEUtils.RequestTex($"{folder}MechDura");
            }
            Texture2D tex2 = AzafureHeavyArmorPlayer.DuraBarBackTex.Value;
            Main.spriteBatch.Draw(tex2, Center, null, color, 0, tex2.Size() / 2f, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex1, Center, new Rectangle(0, 0, (int)(tex1.Width * dura), tex1.Height), color2, 0, tex1.Size() / 2f, 1, SpriteEffects.None, 0);
            bool hover = Center.getRectCentered(100, 40).Intersects(Main.MouseScreen.getRectCentered(2, 2));
            if (hover)
                Main.instance.MouseText(CalamityEntropy.Instance.GetLocalization("DuraBar").Value + $": {dura.ToPercent()}%");
        }
    }
}
