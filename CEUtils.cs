using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content;
using CalamityEntropy.Content.ArmorPrefixes;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Armor.AzafureT3;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.PrefixItem;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using static System.Net.Mime.MediaTypeNames;

namespace CalamityEntropy
{
    public struct Circle
    {
        public Vector2 Center;
        public float Radius;
        public Circle(Vector2 center, float rad)
        {
            Center = center;
            Radius = rad;
        }
        public bool Intersects(Rectangle rectangle)
        {
            Vector2 closestPoint = new Vector2(
                MathHelper.Clamp(Center.X, rectangle.Left, rectangle.Right),
                MathHelper.Clamp(Center.Y, rectangle.Top, rectangle.Bottom)
            );

            Vector2 distance = Center - closestPoint;

            return distance.LengthSquared() < Radius * Radius;
        }
        public bool Intersects(Circle circle)
        {
            return Vector2.Distance(this.Center, circle.Center) <= this.Radius + circle.Radius;
        }
        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is Circle c)
                return this == c;
            return false;
        }
        public override int GetHashCode()
        {
            return Center.GetHashCode() + Radius.GetHashCode();
        }
        public static bool operator ==(Circle value1, Circle value2)
        {
            return (value1.Center == value2.Center &&
                    value1.Radius == value2.Radius);
        }
        public static bool operator !=(Circle value1, Circle value2)
        {
            return !(value1 == value2);
        }
        public static Circle operator *(Circle value, float scaleFactor)
        {
            value.Radius *= scaleFactor;
            return value;
        }
        public static Circle operator /(Circle value, float d)
        {
            value.Radius /= d;
            return value;
        }
    }
    public static class CEUtils
    {
        //绘制助手用到的贴图在加载期就位;这些字段只在客户端绘制路径读,专用服务器上恒为 null
        [VaultLoaden("CalamityEntropy/Assets/GenericBarBack")]
        private static Asset<Texture2D> GenericBarBackTex;
        [VaultLoaden("CalamityEntropy/Assets/GenericBarFront")]
        private static Asset<Texture2D> GenericBarFrontTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/BasicTrailThin")]
        private static Asset<Texture2D> BasicTrailThinTex;
        public static Vector2 CalculateSourceVel(Vector2 shootPos, Vector2 target, int frame, float gravity)
        {
            Vector2 displacement = target - shootPos;
            Vector2 velocity = new Vector2();
            velocity.X = displacement.X / frame;
            velocity.Y = (displacement.Y - 0.5f * gravity * frame * frame) / frame;
            return velocity;
        }
        public static Color Mult(this Color c1, Color c2)
        {
            return new Color((byte)(((c1.R / 255f) * (c2.R / 255f)) * 255), (byte)(((c1.G / 255f) * (c2.G / 255f)) * 255), (byte)(((c1.B / 255f) * (c2.B / 255f)) * 255), (byte)(((c1.A / 255f) * (c2.A / 255f)) * 255));
        }
        public static Color GetLight(Vector2 pos) => Lighting.GetColor((pos / 16).ToPoint());
        public static string ItemTexPath<T>() where T : ModItem
        {
            return (typeof(T).Namespace + "." + typeof(T).Name).Replace('.', '/');
        }
        public static Recipe NearShimmer(this Recipe r) => r.AddCondition(CalamityEntropy.Instance.GetLocalization("NearShimmer"), () => (Main.LocalPlayer.ZoneShimmer));
        public static Vector3 RotatedBy(this Vector3 vector, float rotation, Vector3 axis)
        {
            axis.Normalize();
            Quaternion quaternion = Quaternion.CreateFromAxisAngle(axis, rotation);
            return Vector3.Transform(vector, quaternion);
        }
        public static LocalizedText GetNPCName(int npc)
        {
            if (npc < NPCID.Count)
                return Language.GetText("NPCName." + NPCID.Search.GetName(npc));
            return NPCLoader.GetNPC(npc).GetLocalization("DisplayName");
        }
        public static LocalizedText GetNPCName<T>() where T : ModNPC
        {
            return GetNPCName(ModContent.NPCType<T>());
        }
        public class VertexPointSets
        {
            public Vector2 Position;
            public Color Color;
            public float Width;
            public float TexCoordsX;
            public VertexPointSets(Vector2 position, Color color, float width, float texCoordsX)
            {
                Position = position;
                Color = color;
                Width = width;
                TexCoordsX = texCoordsX;
            }
        }
        public static List<ColoredVertex> GetVertexesList(this List<VertexPointSets> sets, bool fade = true, bool worldPos = true)
        {
            List<ColoredVertex> vertexes = new List<ColoredVertex>();
            for (int i = 0; i < sets.Count; i++)
            {
                Vector2 va = i == 0 ? Vector2.Zero : (sets[i].Position - sets[i - 1].Position).normalize().RotatedBy(MathHelper.PiOver2);
                float w = sets[i].Width;
                float a = fade ? (i / (sets.Count - 1f)) : 1;
                vertexes.Add(new ColoredVertex(sets[i].Position - va * w - (worldPos ? Main.screenPosition : Vector2.Zero), sets[i].Color * a, new Vector3(sets[i].TexCoordsX, 0, 1)));
                vertexes.Add(new ColoredVertex(sets[i].Position + va * w - (worldPos ? Main.screenPosition : Vector2.Zero), sets[i].Color * a, new Vector3(sets[i].TexCoordsX, 1, 1)));
            }
            return vertexes;
        }
        public static float GetDistanceToEllipseEdge(float semiMajorAxis, float semiMinorAxis, float angleRadians)
        {
            float cos = (float)Math.Cos(angleRadians);
            float sin = (float)Math.Sin(angleRadians);

            float denominator = (cos * cos) / (semiMajorAxis * semiMajorAxis) +
                                 (sin * sin) / (semiMinorAxis * semiMinorAxis);

            if (denominator <= 0)
                return 0;

            return (float)(1.0 / Math.Sqrt(denominator));
        }
        public static Vector2 xy(this Vector3 v) => new Vector2(v.X, v.Y);
        public static Vector2 Half(this Vector2 v) => v * 0.5f;
        public static List<NPC> FindSomeNearEnemies(Vector2 center, int maxCount, float distance = 1600, Func<int, bool> filter = null)
        {
            if (maxCount <= 0)
                return new List<NPC>();
            var list = new List<NPC>();
            foreach (var npC in Main.ActiveNPCs)
                list.Add(npC);
            bool filterCheck(NPC npc)
            {
                if (filter != null)
                    return filter.Invoke(npc.whoAmI);
                return true;
            }
            var result = list
                .Where(npc => npc.active
                           && !npc.friendly
                           && !npc.dontTakeDamage
                           && npc.life > 0
                           && npc.Distance(center) < distance
                           && filterCheck(npc))

                .Select(npc => new
                {
                    NPC = npc,
                    DistanceSq = Vector2.DistanceSquared(center, npc.Center)
                })
                .OrderBy(x => x.DistanceSq)
                .Take(maxCount)
                .Select(x => x.NPC)
                .ToList();

            return result;
        }
        public static float Frac(float x)
        {
            if (float.IsInfinity(x) || float.IsNaN(x))
                return x;
            return x - (float)Math.Floor(x);
        }
        public static void ResetMenuButton()
        {
            var ff = typeof(Main).GetField("selectedMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ff2 = typeof(Main).GetField("focusMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ff3 = typeof(Main).GetField("menuItemScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ff.SetValue(Main.instance, -1);
            ff2.SetValue(Main.instance, -1);
            float[] mis = (float[])ff3.GetValue(Main.instance);
            for (int i = 0; i < mis.Length; i++)
            {
                mis[i] = 0.8f;
            }
            ff3.SetValue(Main.instance, mis);
        }
        public static void HoldShiftTooltip(List<TooltipLine> tooltips, TooltipLine[] holdShiftTooltips, bool hideNormalTooltip = false)
        {
            // Only perform any changes while holding SHIFT.
            if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                return;

            // Get the first index, last index and total count of standard vanilla tooltip lines.
            // The first index and count are used to delete all vanilla tooltips when holding SHIFT, if requested.
            // The last index is used to insert the "Hold SHIFT" tooltips in the right position.
            int firstTooltipIndex = -1;
            int lastTooltipIndex = -1;
            int standardTooltipCount = 0;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                {
                    if (firstTooltipIndex == -1)
                        firstTooltipIndex = i;
                    lastTooltipIndex = i;
                    standardTooltipCount++;
                }
            }

            if (firstTooltipIndex != -1)
            {
                // If asked to, remove all standard tooltip lines. This moves the last tooltip index.
                if (hideNormalTooltip)
                {
                    tooltips.RemoveRange(firstTooltipIndex, standardTooltipCount);
                    lastTooltipIndex -= standardTooltipCount;
                }

                // Append every "Hold SHIFT" tooltip at the end of standard tooltips.
                tooltips.InsertRange(lastTooltipIndex + 1, holdShiftTooltips);
            }
        }
        public static bool AnyActiveProj<T>() where T : ModProjectile
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile != null && p.ModProjectile is T)
                    return true;
            }
            return false;
        }
        public static int SecondToFrames(this float second) => (int)(second * 60);
        public static bool HomingToNPCNearby(this Projectile projectile, float vel = 2f, float velMult = 0.97f, float maxRadius = 600, Func<int, bool> filter = null)
        {
            NPC target = FindTarget_HomingProj(projectile, projectile.Center, maxRadius, filter);
            if (target == null) return false;
            projectile.velocity *= velMult;
            projectile.velocity += (target.Center - projectile.Center).normalize() * vel;
            return true;
        }
        public static int GetProjectileDamage(this NPC npc, int type)
        {
            return npc.damage == 0 ? 60 : npc.damage / 3;
        }
        //脱离灾厄:OldFashioned(灾厄酒)乘区已删,恒等返回;方法保留避免炸调用点,后续可整体删调用
        public static int ApplyAccArmorDamageBonus(this int origDmg, Player player = null)
        {
            return origDmg;
        }
        public static int GetPriceFromRecipe(this ModItem item, Recipe recipe)
        {
            if (recipe == null)
            {
                return 0;
            }
            int total = 0;
            foreach (var i in recipe.requiredItem)
            {
                total += i.value * i.stack;
            }
            return total;
        }
        public static Recipe FindRecipe(int type)
        {
            foreach (Recipe r in Main.recipe)
            {
                if (r.createItem.type == type)
                    return r;
            }
            return null;
        }
        public static bool AzafureEnhance(this Player player)
        {
            return player.GetModPlayer<AzafureHeavyArmorPlayer>().ArmorSetBonus || player.GetModPlayer<AzafureSteamKnightArmorPlayer>().ArmorSetBonus || player.GetModPlayer<AcropolisArmorPlayer>().ArmorSetBonus;
        }
        public static float AzafureDurability(this Player player)
        {
            if (!player.AzafureEnhance())
                return 0;
            if (player.GetModPlayer<AcropolisArmorPlayer>().ArmorSetBonus)
                return player.GetModPlayer<AcropolisArmorPlayer>().durability;
            if (player.GetModPlayer<AzafureSteamKnightArmorPlayer>().ArmorSetBonus)
                return player.GetModPlayer<AzafureSteamKnightArmorPlayer>().durability;
            return player.GetModPlayer<AzafureHeavyArmorPlayer>().durability;
        }
        public static string MouseText
        {
            get
            {
                var fInfo = typeof(Main).GetField("_mouseTextCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Instance);
                if (fInfo != null)
                {
                    object obj = fInfo.GetValue(Main.instance);
                    var strInfo = obj.GetType().GetField("cursorText");
                    if (strInfo != null)
                        return (string)(strInfo.GetValue(obj));
                    else
                        return "";
                }
                else
                {
                    return "";
                }
            }
            set
            {
                var fInfo = typeof(Main).GetField("_mouseTextCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Instance);
                if (fInfo != null)
                {
                    object obj = fInfo.GetValue(Main.instance);
                    var strInfo = obj.GetType().GetField("cursorText");
                    if (strInfo != null)
                        strInfo.SetValue(obj, value);
                }
            }
        }
        public static void ProjTrailData(this Projectile proj, int length, int mode)
        {
            ProjectileID.Sets.TrailCacheLength[proj.type] = length;
            ProjectileID.Sets.TrailingMode[proj.type] = mode;
        }
        public static float CustomLerp2(float p)
        {
            return float.Lerp(1, 0, (1 - p) * (1 - p) * (1 - p));
        }
        public static void StickToPlayer(this Projectile proj)
        {
            Player player = proj.GetOwner();
            player.Entropy().MouseWorldListener = true;
            proj.Center = player.GetDrawCenter();
            proj.rotation = (player.mouseWorld() - proj.Center).ToRotation();
            proj.velocity = proj.rotation.ToRotationVector2() * player.HeldItem.shootSpeed;
            player.heldProj = proj.whoAmI;
        }
        public static void StickToPlayer(this Projectile proj, float RotLerp = 0.1f)
        {
            Player player = proj.GetOwner();
            player.Entropy().MouseWorldListener = true;
            proj.Center = player.GetDrawCenter();
            proj.rotation = CEUtils.RotateTowardsAngle(proj.rotation, (player.mouseWorld() - proj.Center).ToRotation(), RotLerp, false);
            proj.velocity = proj.rotation.ToRotationVector2() * player.HeldItem.shootSpeed;
            player.heldProj = proj.whoAmI;
        }
        /// <summary>
        /// 各端可见的玩家鼠标世界坐标(自研,替代灾厄mouseWorld)。调用即开启本帧联机同步监听。
        /// </summary>
        public static Vector2 mouseWorld(this Player player)
        {
            player.Entropy().MouseWorldListener = true;
            return player.Entropy().MouseWorld;
        }
        /// <summary>
        /// 同 <see cref="mouseWorld(Player)"/> 的静态写法。
        /// </summary>
        public static Vector2 MouseWorld(Player player) => player.mouseWorld();
        public static void CheckAndSpawnHeldProj(this Player player, int type)
        {
            if (player.ownedProjectileCounts[type] < 1 && Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.Center, Vector2.Zero, type, player.GetWeaponDamage(player.HeldItem), 0, player.whoAmI);
            }
        }
        public static bool TryKillTileAndChest(int x, int y, Player player)
        {
            bool t = TryKillTile(x, y, player);
            bool c = CheckChestDestroy(player, x, y);
            return t || c;
        }
        public static bool TryKillTile(int x, int y, Player player)
        {
            Tile tile = Main.tile[x, y];
            if (tile.HasTile && !Main.tileHammer[Main.tile[x, y].TileType])
            {
                if (player.HasEnoughPickPowerToHurtTile(x, y))
                {
                    if (TileID.Sets.Grass[tile.TileType] || TileID.Sets.GrassSpecial[tile.TileType] || Main.tileMoss[tile.TileType] || TileID.Sets.tileMossBrick[tile.TileType])
                    {
                        player.PickTile(x, y, 10000);
                    }
                    player.PickTile(x, y, 10000);
                }
            }
            return !Main.tile[x, y].HasTile;
        }
        public static bool CheckChestDestroy(Player player, int i, int j)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket mp = CalamityEntropy.Instance.GetPacket();
                mp.Write((byte)CEMessageType.DestroyChest);
                mp.Write(player.whoAmI);
                mp.Write(i);
                mp.Write(j);
                mp.Send();
                return true;
            }
            var tile = Main.tile[i, j];
            if (!TileID.Sets.IsAContainer[tile.TileType])
                return false;

            var origin = GetTileOrigin(i, j);
            int chestIndex = Chest.FindChest(origin.X, origin.Y);
            if (chestIndex == -1 || !Main.chest.IndexInRange(chestIndex))
                return false;

            var chest = Main.chest[chestIndex];
            if (Chest.IsLocked(chest.x, chest.y) || chest?.item is null)
            {
                return false;
            }

            for (int k = 0; k < chest.item.Length; k++)
                if (!chest.item[k].IsAir)
                    SpawnTileBreakItem(i, j, ref chest.item[k], "ChestBroken");
            TryKillTile(i, j, player);
            if (Main.dedServ)
            {
                NetMessage.SendTileSquare(-1, i, j);
            }
            return true;
        }
        public static Point16 GetTileOrigin(int i, int j)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            TileObjectData tileData = TileObjectData.GetTileData(tile.TileType, 0);
            if (tileData == null)
            {
                return Point16.NegativeOne;
            }
            int frameX = tile.TileFrameX;
            int frameY = tile.TileFrameY;
            int subX = frameX % tileData.CoordinateFullWidth;
            int subY = frameY % tileData.CoordinateFullHeight;

            Point16 coord = new(i, j);
            Point16 frame = new(subX / 18, subY / 18);

            return coord - frame;
        }
        public static void SpawnTileBreakItem(int x, int y, ref Item item, string context = null) =>
            SpawnTileBreakItem(new Point16(x, y), ref item, context);

        public static void SpawnTileBreakItem(Point16 tileCoords, ref Item item, string context = null)
        {
            var position = tileCoords.ToWorldCoordinates();
            int i = Item.NewItem(new EntitySource_TileBreak(tileCoords.X, tileCoords.Y, context), (int)position.X, (int)position.Y, 32, 32,
                item.type);
            item.position = Main.item[i].position;
            Main.item[i] = item;
            var drop = Main.item[i];
            item = new Item();
            drop.velocity.Y = -2f;
            drop.velocity.X = Main.rand.NextFloat(-4f, 4f);
            drop.favorited = false;
            drop.newAndShiny = false;
        }

        public static void MinionCheck<T>(this Projectile proj) where T : ModBuff
        {
            Player player = proj.GetOwner();

            if (player.HasBuff<T>())
            {
                proj.timeLeft = 6;
            }
        }
        public static NPC FindMinionTarget(this Projectile projectile, int radians = 3000, bool CheckTile = false)
        {
            Player player = projectile.GetOwner();
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active)
            {
                return player.MinionAttackTargetNPC.ToNPC();
            }
            NPC npc = FindTarget_HomingProj(projectile, projectile.Center, radians, CheckTile ? CEUtils.HomingWithTileBlockingFilter : null);
            return npc;
        }
        public static float WeapSound => ModContent.GetInstance<Config>().EntropyMeleeWeaponSoundVolume;
        public static T random<T>(this List<T> list)
        {
            return list[Main.rand.Next(list.Count)];
        }
        public static float GetCritDamage(this Player player, DamageClass dmgClass)
        {
            if (!player.Entropy().CritDamage.ContainsKey(dmgClass))
            {
                player.Entropy().CritDamage.Add(dmgClass, 1);

            }

            return player.Entropy().CritDamage[dmgClass];
        }
        public static void AddCritDamage(this Player player, DamageClass dmgClass, float value)
        {
            if (!player.Entropy().CritDamage.ContainsKey(dmgClass))
            {
                player.Entropy().CritDamage.Add(dmgClass, 1);

            }
            player.Entropy().CritDamage[dmgClass] += value;
        }
        public static void HealMana(this Player player, int amount)
        {
            player.statMana += amount;
            if (player.statMana > player.statManaMax2)
                player.statMana = player.statManaMax2;
        }
        public static void ExplotionParticleLOL(Vector2 pos)
        {
            PRTLoader.NewParticle<PRT_RealisticExplosion>(pos, Vector2.Zero, Color.White, 2).Configure(1, true, PRTDrawModeEnum.AlphaBlend);
        }
        public static void AddBuff<T>(this NPC npc, int time, bool quiet = false) where T : ModBuff
        {
            npc.AddBuff(ModContent.BuffType<T>(), time, quiet);
        }
        public static Tile PlaceTile(int x, int y, ushort type)
        {
            int si = x;
            int sj = y;
            if (CEUtils.inWorld(si, sj))
            {
                Tile t = Main.tile[si, sj];
                if (t.HasTile)
                {
                    return new Tile();
                }
                t.TileType = type;
                t.HasTile = true;
                WorldGen.SquareTileFrame(si, sj);

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendTileSquare(-1, si, sj);
                return Main.tile[si, sj];

            }
            return new Tile();
        }
        public static void SetHandRot(this Player owner, float r)
        {
            if (r.ToRotationVector2().X > 0)
            {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
            }
        }
        public static void SetHandRot(this Player owner, float r, Player.CompositeArmStretchAmount stretch)
        {
            if (r.ToRotationVector2().X > 0)
            {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, stretch, r - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, stretch, r - (float)(Math.PI * 0.5f));
            }
        }
        public static void SetHandRotWithDir(this Player owner, float r, int dir)
        {
            int stretch = 0;
            owner.direction = dir;
            if (r.ToRotationVector2().X > 0)
            {
                owner.SetCompositeArmFront(true, (Player.CompositeArmStretchAmount)stretch, r - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.SetCompositeArmFront(true, (Player.CompositeArmStretchAmount)stretch, r - (float)(Math.PI * 0.5f));
            }
        }
        public static void SetHandRotWithDir(this Player owner, float r, int dir, int stretch = 0)
        {
            owner.direction = dir;
            if (r.ToRotationVector2().X > 0)
            {
                owner.SetCompositeArmFront(true, (Player.CompositeArmStretchAmount)stretch, r - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.SetCompositeArmFront(true, (Player.CompositeArmStretchAmount)stretch, r - (float)(Math.PI * 0.5f));
            }
        }
        public static Vector2 GetDrawCenter(this Player player)
        {
            return player.MountedCenter + player.gfxOffY * Vector2.UnitY;
        }
        public static Vector2 GetCircleIntersection(Vector2 vec1, float a, Vector2 vec2, float b, bool flag = false, bool flag2 = false)
        {
            float distance = Vector2.Distance(vec1, vec2);

            if (distance > a + b || distance < Math.Abs(a - b))
            {
                Vector2 direction = Vector2.Normalize(vec2 - vec1);
                return vec1 + direction * a;
            }

            float d = distance;
            float l = (a * a - b * b + d * d) / (2 * d);
            float h = (float)Math.Sqrt(a * a - l * l);

            Vector2 p0 = vec1 + (l / d) * (vec2 - vec1);

            Vector2 intersection1 = new Vector2(
                p0.X + (h / d) * (vec2.Y - vec1.Y),
                p0.Y - (h / d) * (vec2.X - vec1.X));

            Vector2 intersection2 = new Vector2(
                p0.X - (h / d) * (vec2.Y - vec1.Y),
                p0.Y + (h / d) * (vec2.X - vec1.X));
            if (flag2)
            {
                return flag ? intersection1 : intersection2;
            }
            return (intersection1.Y < intersection2.Y) ? intersection1 : intersection2;
        }
        public static void AddLight(Vector2 position, Color lightColor, float mult = 1)
        {
            Lighting.AddLight(position, lightColor.R / 255f * mult, lightColor.G / 255f * mult, lightColor.B / 255f * mult);
        }
        public static T[] Combine<T>(this T[] a, T[] b)
        {
            T[] ls = new T[a.Length + b.Length];
            int c = 0;
            foreach (var i in a) { ls[c] = i; c++; }
            foreach (var i in b) { ls[c] = i; c++; }
            return ls;
        }
        public static bool IsPlayerStuck(Player player)
        {
            Rectangle playerHitbox = player.getRect();

            for (int i = 0; i < 4; i++)
            {
                Point checkPoint = new Point(
                    i < 2 ? playerHitbox.Left : playerHitbox.Right - 1,
                    i % 2 == 0 ? playerHitbox.Top : playerHitbox.Bottom - 1);

                if (WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(checkPoint.X / 16, checkPoint.Y / 16)))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool CheckSolidTile(Rectangle rect)
        {
            if (rect.Y + rect.Height > Main.maxTilesY * 16)
                return true;
            return Collision.SolidCollision(rect.TopLeft(), rect.Width, rect.Height);
        }
        public static bool CheckSolidTileOrPlatform(Rectangle rect)
        {
            if (rect.Y + rect.Height > Main.maxTilesY * 16)
                return true;
            return SolidOrPlatCollision(rect.TopLeft(), rect.Width, rect.Height);
        }
        public static bool SolidOrPlatCollision(Vector2 Position, int Width, int Height)
        {
            int value = (int)(Position.X / 16f) - 1;
            int value2 = (int)((Position.X + (float)Width) / 16f) + 2;
            int value3 = (int)(Position.Y / 16f) - 1;
            int value4 = (int)((Position.Y + (float)Height) / 16f) + 2;
            int num = Utils.Clamp(value, 0, Main.maxTilesX - 1);
            value2 = Utils.Clamp(value2, 0, Main.maxTilesX - 1);
            value3 = Utils.Clamp(value3, 0, Main.maxTilesY - 1);
            value4 = Utils.Clamp(value4, 0, Main.maxTilesY - 1);
            Vector2 vector = default(Vector2);
            for (int i = num; i < value2; i++)
            {
                for (int j = value3; j < value4; j++)
                {
                    if (Main.tile[i, j] != null && Main.tile[i, j].HasTile && (Main.tileSolid[Main.tile[i, j].TileType] || Main.tileSolidTop[Main.tile[i, j].TileType]) && !Main.tile[i, j].IsActuated)
                    {
                        vector.X = i * 16;
                        vector.Y = j * 16;
                        int num2 = 16;

                        if (Position.X + (float)Width > vector.X && Position.X < vector.X + 16f && Position.Y + (float)Height > vector.Y && Position.Y < vector.Y + (float)num2)
                            return true;
                    }
                }
            }

            return false;
        }
        public static void FriendlySetDefaults(this Projectile Projectile, DamageClass dmgClass, bool tileCollide = false, int penetrate = 1)
        {
            Projectile.DamageType = dmgClass;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = penetrate;
            Projectile.tileCollide = tileCollide;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }
        public static void HeldProjSetDefaults(this Projectile Projectile, DamageClass dmgClass)
        {
            Projectile.DamageType = dmgClass;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public static int Softlimitation(this int num, int limit)
        {
            if (num <= limit)
                return num;
            return (int)Math.Round(limit + Math.Sqrt(num - limit));
        }
        public static void SpawnExplotionHostile(IEntitySource source, Vector2 position, int damage, float r, bool alsoFriendly = false)
        {
            int p = Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<CommonExplotion>(), damage, 0, 0, r, alsoFriendly ? 1 : 0);
            if (Main.dedServ)
                CEUtils.SyncProj(p);
        }
        public static Projectile SpawnExplotionFriendly(IEntitySource source, Player player, Vector2 position, int damage, float r, DamageClass damageClass)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                var p = Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<CommonExplotionFriendly>(), damage, 0, player.whoAmI, r).ToProj();
                p.DamageType = damageClass;
                return p;
            }
            else
            {
                return Main.projectile[0];
            }
        }
        public static void SetShake(Vector2 center, float strength, float MaxDist = 4000)
        {
            float s = Utils.Remap(Main.LocalPlayer.Distance(center), MaxDist, 800, 0f, strength * 1);
            ScreenShaker.AddShake((Main.LocalPlayer.Center - center).normalize(), s);
        }
        public static List<Vector2> WrapPoints(List<Vector2> points, int d)
        {
            var ptd = new List<Vector2>();
            for (int i = 1; i < points.Count; i++)
            {
                for (int j = 0; j < d; j++)
                {
                    ptd.Add(Vector2.Lerp(points[i - 1], points[i], (float)j / d));
                }
            }
            return ptd;
        }
        public static int ApplyCdDec(this int orig, Player plr)
        {
            return (int)(orig * plr.Entropy().CooldownTimeMult);
        }
        public static bool HasEBookEffect<T>(this Projectile p) where T : EBookProjectileEffect
        {
            if (p.ModProjectile is EBookBaseProjectile ep)
            {
                foreach (var ef in ep.ProjectileEffects)
                {
                    if (ef.GetType() == typeof(T))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static NMSGItem TFAW(this Item item) => item.GetGlobalItem<NMSGItem>();
        public static NMSPLayer TFAW(this Player player) => player.GetModPlayer<NMSPLayer>();

        /// <summary>
        /// 用于将一个武器设置为手持刀剑类，这个函数若要正确设置物品的近战属性，需要让其在初始化函数中最后调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public static void SetKnifeHeld<T>(this Item item) where T : ModProjectile
        {
            item.noMelee = true;
            item.noUseGraphic = true;
            item.TFAW().IsShootCountCorlUse = true;
            item.shoot = ModContent.ProjectileType<T>();
        }
        public static Vector2 randVr(int min, int max)
        {
            return Main.rand.NextVector2Unit() * Main.rand.Next(min, max);
        }
        public static float GetCorrectRadian(float minusRadian)
        {
            return minusRadian < 0 ? (MathHelper.TwoPi + minusRadian) / MathHelper.TwoPi : minusRadian / MathHelper.TwoPi;
        }

        /// <summary>
        /// 获取纹理实例，类型为 Texture2D
        /// </summary>
        /// <param name="texture">纹理路径</param>
        /// <returns></returns>
        public static Texture2D GetT2DValue(string texture, bool immediateLoad = false)
        {
            return ModContent.Request<Texture2D>(texture, immediateLoad ? AssetRequestMode.AsyncLoad : AssetRequestMode.ImmediateLoad).Value;
        }
        /// <summary>
        /// 获取纹理实例，类型为 AssetTexture2D
        /// </summary>
        /// <param name="texture">纹理路径</param>
        /// <returns></returns>
        public static Asset<Texture2D> GetT2DAsset(string texture, bool immediateLoad = false)
        {
            return ModContent.Request<Texture2D>(texture, immediateLoad ? AssetRequestMode.AsyncLoad : AssetRequestMode.ImmediateLoad);
        }

        public static float RotTowards(this float curAngle, float targetAngle, float maxChange)
        {
            curAngle = MathHelper.WrapAngle(curAngle);
            targetAngle = MathHelper.WrapAngle(targetAngle);
            if (curAngle < targetAngle)
            {
                if (targetAngle - curAngle > (float)Math.PI)
                {
                    curAngle += (float)Math.PI * 2f;
                }
            }
            else if (curAngle - targetAngle > (float)Math.PI)
            {
                curAngle -= (float)Math.PI * 2f;
            }

            curAngle += MathHelper.Clamp(targetAngle - curAngle, 0f - maxChange, maxChange);
            return MathHelper.WrapAngle(curAngle);
        }
        public static Vector2 SmoothHomingBehavior(this Entity entity, Vector2 TargetCenter, float SpeedUpdates = 1, float HomingStrenght = 0.1f)
        {
            float targetAngle = (TargetCenter - entity.Center).ToRotation();
            float f = entity.velocity.ToRotation().RotTowards(targetAngle, HomingStrenght);
            Vector2 speed = f.ToRotationVector2() * entity.velocity.Length() * SpeedUpdates;
            entity.velocity = speed;
            return speed;
        }
        public static float Parabola(float t, float height)
        {
            return 4 * height * t * (1 - t);
        }
        public static bool CheckAirLine(Vector2 v1, Vector2 v2)
        {
            for (float i = 0; i < 1; i += 1f / (getDistance(v1, v2) / 8))
            {
                if (!isAir(Vector2.Lerp(v1, v2, i)))
                    return false;
            }
            return true;
        }
        public static bool HomingWithTileBlockingFilter(Projectile proj, int npc)
        {
            return CheckAirLine(proj.Center, npc.ToNPC().Center);
        }
        public static NPC FindTarget_HomingProj(Projectile proj, Vector2 center, float radians, Func<Projectile, int, bool> filter = null)
        {
            NPC npc = null;
            float dist = radians;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.CanBeChasedBy(proj) && !n.friendly)
                {
                    if (getDistance(n.Center, center) <= dist && (filter == null || filter.Invoke(proj, n.whoAmI)))
                    {
                        dist = getDistance(n.Center, center);
                        npc = n;
                    }
                }
            }
            return npc;
        }
        public static NPC FindTarget_HomingProj(object atker, Vector2 center, float radians, Func<int, bool> filter = null)
        {
            NPC npc = null;
            float dist = radians;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.CanBeChasedBy(atker) && !n.friendly)
                {
                    if (getDistance(n.Center, center) <= dist && (filter == null || filter.Invoke(n.whoAmI)))
                    {
                        dist = getDistance(n.Center, center);
                        npc = n;
                    }
                }
            }
            return npc;
        }
        public static void SetSyncValue(this Projectile proj, string name, object value)
        {
            proj.Entropy().DataSynchronous[name].Value = value;
        }
        public static void DefineSynchronousData(this Projectile proj, SyncDataType type, string name, object defaultValue)
        {
            proj.Entropy().DataSynchronous[name] = new SynchronousData(type, name, defaultValue);
        }
        public static T GetSyncValue<T>(this Projectile proj, string name)
        {
            return proj.Entropy().DataSynchronous[name].GetValue<T>();
        }
        public static void SetSizeFormTexture(this Item item, float scale = 1)
        {
            Texture2D tex = ModContent.Request<Texture2D>(item.ModItem.Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int w = (int)(scale * tex.Width);
            int h = (int)(scale * tex.Height);
            item.width = w; item.height = h;
        }
        public static bool IsArmorReforgeItem(this Item item, out ArmorPrefix prefix)
        {
            prefix = null;
            if (item.ModItem is PrefixClearKnife)
            {
                return true;
            }
            if (item.ModItem is BasePrefixItem bpi)
            {
                prefix = ArmorPrefix.findByName(bpi.PrefixName);
                return true;
            }
            return false;
        }
        public static void Shrink(this Item item, int count = 1)
        {
            item.stack -= count;
            if (item.stack <= 0)
            {
                item.TurnToAir();
            }
        }
        public static float CustomLerp1(float v)
        {
            float j = 0.6f;
            return (float)((Math.Cos(v * (MathHelper.Pi + j) - MathHelper.Pi) * 0.5f + 0.5f) / Math.Cos(j));
        }
        public static float GetRepeatedParaFromZeroToOne(float v, int repeat)
        {
            v = float.Clamp(v, 0, 1);
            if (repeat <= 1)
            {
                return Parabola(v * 0.5f, 1);
            }
            return GetRepeatedParaFromZeroToOne(Parabola(v * 0.5f, 1), repeat - 1);
        }
        public static float GetRepeatedCosFromZeroToOne(float v, int repeat)
        {
            if (repeat <= 1)
            {
                return (float)(Math.Cos(v * MathHelper.Pi - MathHelper.Pi)) * 0.5f + 0.5f;
            }
            return (float)(Math.Cos(GetRepeatedCosFromZeroToOne(v, repeat - 1) * MathHelper.Pi - MathHelper.Pi)) * 0.5f + 0.5f;
        }
        public static void Replace(this List<TooltipLine> tooltips, string targetStr, string to)
        {
            if (!Main.dedServ)
            {
                tooltips.FindAndReplace(targetStr, to);
            }
        }
        public static void Replace(this List<TooltipLine> tooltips, string targetStr, int to)
        {
            if (!Main.dedServ)
            {
                tooltips.FindAndReplace(targetStr, to.ToString());
            }
        }
        public static void Replace(this List<TooltipLine> tooltips, string targetStr, float to)
        {
            if (!Main.dedServ)
            {
                tooltips.FindAndReplace(targetStr, to.ToString());
            }
        }
        public static float ToPercent(this float f)
        {
            return (float)(Math.Round(f, 3) * 100f);
        }
        //占位符替换。原实现只认 Mod == "Terraria" 的行,而 hjson 自动加载的本模组 Tooltip 挂的是 "CalamityEntropy",
        //于是全模组的 tooltips.Replace 从脱灾起一直空转。这里不再挑行,同一个占位符也允许跨多行出现
        public static void FindAndReplace(this List<TooltipLine> tooltips, string replacedKey, string newKey)
        {
            if (tooltips == null || string.IsNullOrEmpty(replacedKey))
            {
                return;
            }
            newKey ??= string.Empty;
            foreach (TooltipLine line in tooltips)
            {
                if (line?.Text != null && line.Text.Contains(replacedKey))
                {
                    line.Text = line.Text.Replace(replacedKey, newKey);
                }
            }
        }
        public static Vector2 GetFrameOrigin(this PlayerDrawSet drawInfo)
        {
            return new Vector2(
            (int)(drawInfo.Position.X - Main.screenPosition.X - (drawInfo.drawPlayer.bodyFrame.Width / 2) + (float)(drawInfo.drawPlayer.width / 2)),
            (int)(drawInfo.Position.Y - Main.screenPosition.Y + (drawInfo.drawPlayer.height - (drawInfo.drawPlayer.mount.Active ? drawInfo.drawPlayer.mount.HeightBoost : 0)) - (float)drawInfo.drawPlayer.bodyFrame.Height + 4f));
        }
        public static Vector2 HeadPosition(this PlayerDrawSet drawInfo, bool addBob = false, bool vanillaStyle = false)
        {
            Vector2 drawPosition = GetFrameOrigin(drawInfo);

            if (vanillaStyle)
                drawPosition += drawInfo.drawPlayer.headPosition + drawInfo.headVect;
            else
            {
                if (drawInfo.drawPlayer.gravDir == -1)
                    drawPosition.Y = (int)drawInfo.Position.Y - Main.screenPosition.Y + (float)drawInfo.drawPlayer.bodyFrame.Height - 4f;

                Vector2 headOffset = drawInfo.drawPlayer.headPosition + drawInfo.headVect;

                if (!drawInfo.drawPlayer.dead && drawInfo.drawPlayer.gravDir == -1)
                    headOffset.Y -= 6;

                headOffset.Y *= drawInfo.drawPlayer.gravDir;
                drawPosition += headOffset;
            }

            if (addBob)
                drawPosition += Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height] * drawInfo.drawPlayer.gravDir;

            return drawPosition + new Vector2(0, drawInfo.drawPlayer.height - 42);
        }
        public static Vector2 randomPointInCircle(float r)
        {
            return randomRot().ToRotationVector2() * Main.rand.NextFloat(-r, r);
        }
        public static void DrawChargeBar(float barScale, Vector2 position, float progress, Color color)
        {
            if (float.IsNaN(barScale) || float.IsInfinity(barScale) || barScale <= 0f)
                return; 
            if (float.IsNaN(progress) || float.IsInfinity(progress))
                progress = 0f;
            var barBG = GenericBarBackTex.Value;
            var barFG = GenericBarFrontTex.Value;
            if (barBG == null || barFG == null || barBG.Width <= 0 || barFG.Width <= 0)
                return;
            Vector2 barOrigin = barBG.Size() * 0.5f;
            Vector2 drawPos = position;

            progress = float.Clamp(progress, 0, 1);
            float bw = barFG.Width;
            int bh = barFG.Height;
            SpriteBatch spriteBatch = Main.spriteBatch;
            spriteBatch.Draw(barBG, drawPos, null, color, 0f, barOrigin, barScale, 0f, 0f);
            if (progress >= 0)
            {
                Rectangle frameCrop = new Rectangle(0, 0, (int)(progress * bw), bh);
                spriteBatch.Draw(barFG, drawPos, frameCrop, color * 0.8f, 0f, barOrigin, barScale, 0f, 0f);
            }
        }
        public static void ApplyGameShaderForPlayer(int id, Player player)
        {
            GameShaders.Armor.Apply(id, player);
        }
        public static string WhiteTexPath = "CalamityEntropy/Assets/Extra/white";
        //优先读共享基座;基座字段在 PostSetupContent 才赋值,加载期访问走 Request 兜底
        public static Texture2D pixelTex => CEExtraAssets.white ?? ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white").Value;
        public static Texture2D GetTexture(this Projectile p)
        {
            return TextureAssets.Projectile[p.type].Value;
        }
        public static Texture2D getTextureAlt(this ModProjectile p, string n = "Alt")
        {
            return RequestTex(p.Texture + n);
        }
        public static Texture2D getTextureGlow(this ModProjectile p)
        {
            return RequestTex(p.Texture + "Glow");
        }
        public static Dictionary<string, Texture2D> TexCache;
        public static Texture2D RequestTex(string path)
        {
            if (!TexCache.ContainsKey(path))
            {
                TexCache[path] = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
            }
            return TexCache[path];
        }
        public static Vector2 normalize(this Vector2 v)
        {
            return v.SafeNormalize(Vector2.Zero);
        }
        public static Vector2 randomPoint(this Rectangle rect)
        {
            return new Vector2(Main.rand.NextFloat(rect.X, rect.X + rect.Width), Main.rand.NextFloat(rect.Y, rect.Y + rect.Height));
        }
        public static bool Is<T>(this Item item) where T : ModItem
        {
            return item.type == ModContent.ItemType<T>();
        }
        public static void DrawRotatedGlow(Vector2 worldPos, Color color, float scale, float rot, bool additive = true, Texture2D tex = null, bool setState = true)
        {
            Texture2D glow = tex == null ? CEExtraAssets.Glow2 : tex;
            SpriteBatch sb = Main.spriteBatch;
            var blend = BlendState.AlphaBlend;
            var sample = sb.GraphicsDevice.SamplerStates[0];
            var depth = sb.GraphicsDevice.DepthStencilState;
            var rasterizer = sb.GraphicsDevice.RasterizerState;
            if (setState)
            {
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, additive ? BlendState.Additive : BlendState.NonPremultiplied, sample, depth, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            sb.Draw(glow, worldPos - Main.screenPosition, null, color, rot, glow.Size() * 0.5f, scale * 0.4f, SpriteEffects.None, 0);
            if (setState)
            {
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, blend, sample, depth, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        //PRT PostDraw里常调这个,setState默认true会End→Immediate画光晕→End→Deferred+AlphaBlend
        //收尾批次跟PRT桶对不上,调用方还得sb.End()+BeginDrawingWithMode接回去
        public static void DrawGlow(Vector2 worldPos, Color color, float scale, bool additive = true, Texture2D tex = null, bool setState = true)
        {
            Texture2D glow = tex == null ? CEExtraAssets.Glow2 : tex;
            SpriteBatch sb = Main.spriteBatch;
            var blend = BlendState.AlphaBlend;
            var sample = sb.GraphicsDevice.SamplerStates[0];
            var depth = sb.GraphicsDevice.DepthStencilState;
            var rasterizer = sb.GraphicsDevice.RasterizerState;
            if (setState)
            {
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, additive ? BlendState.Additive : BlendState.NonPremultiplied, sample, depth, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            sb.Draw(glow, worldPos - Main.screenPosition, null, color, 0, glow.Size() * 0.5f, scale * 0.4f, SpriteEffects.None, 0);
            if (setState)
            {
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, blend, sample, depth, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        public static Terraria.DataStructures.DrawData getDrawData(this Projectile projectile, Color color, float overrideRotation, Texture2D texOverride = null, Vector2 overridePos = default)
        {
            Texture2D tx = projectile.GetTexture();
            if (texOverride != null)
            {
                tx = texOverride;
            }
            return new Terraria.DataStructures.DrawData(tx, (overridePos == default ? projectile.Center : overridePos) - Main.screenPosition, Main.projFrames[projectile.type] <= 1 ? null : new Rectangle(0, (tx.Height / Main.projFrames[projectile.type]) * projectile.frame, tx.Width, (tx.Height / Main.projFrames[projectile.type]) - 2), color * projectile.Opacity, overrideRotation, new Vector2(tx.Width, Main.projFrames[projectile.type] > 1 ? (tx.Height / Main.projFrames[projectile.type]) - 2 : tx.Height) / 2, projectile.scale, projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
        }
        public static Terraria.DataStructures.DrawData getDrawData(this Projectile projectile, Color color, Texture2D texOverride = null, Vector2 overridePos = default)
        {
            Texture2D tx = projectile.GetTexture();
            if (texOverride != null)
            {
                tx = texOverride;
            }
            return new Terraria.DataStructures.DrawData(tx, (overridePos == default ? projectile.Center : overridePos) - Main.screenPosition, Main.projFrames[projectile.type] <= 1 ? null : new Rectangle(0, (tx.Height / Main.projFrames[projectile.type]) * projectile.frame, tx.Width, (tx.Height / Main.projFrames[projectile.type]) - 2), color * projectile.Opacity, projectile.rotation, new Vector2(tx.Width, Main.projFrames[projectile.type] > 1 ? (tx.Height / Main.projFrames[projectile.type]) - 2 : tx.Height) / 2, projectile.scale, projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
        }
        public static void showItemTooltip(Item item)
        {
            Main.HoverItem = item.Clone();
            Main.hoverItemName = item.HoverName;
        }
        public static void SyncItem(int i)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i);
            }
        }
        public static void SyncProj(int proj)
        {
            if (Main.netMode != NetmodeID.SinglePlayer && (!proj.ToProj().friendly || Main.myPlayer == proj.ToProj().owner))
            {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
            }
        }
        public static void SyncProj(Projectile proj) => SyncProj(proj.whoAmI);
        public static void pushByOther(this Projectile proj, float strength)
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == proj.type && p.owner == proj.owner && p.Colliding(p.Center.getRectCentered(p.width * p.scale, p.height * p.scale), proj.Center.getRectCentered(proj.width * proj.scale, proj.height * proj.scale)) && !(p.whoAmI == proj.whoAmI))
                {
                    proj.velocity += (proj.Center - p.Center).SafeNormalize(randomRot().ToRotationVector2()) * strength;
                }
            }
        }
        public static Vector2 randomVec(float max)
        {
            return new Vector2(Main.rand.NextFloat(-max, max), Main.rand.NextFloat(-max, max));
        }
        public static Vector2 Bezier(List<Vector2> points, float lerp)
        {
            if (points == null || points.Count == 0)
                return Vector2.Zero;

            if (points.Count == 1)
                return points[0];

            List<Vector2> newPoints = new List<Vector2>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                newPoints.Add(Vector2.Lerp(points[i], points[i + 1], lerp));
            }

            return Bezier(newPoints, lerp);
        }

        public static Texture2D getTexture(this NPC npc)
        {
            return TextureAssets.Npc[npc.type].Value;
        }
        public static float GetAngleBetweenVectors(Vector2 vector1, Vector2 vector2)
        {
            float dotProduct = Vector2.Dot(vector1, vector2);

            float magnitude1 = vector1.Length();
            float magnitude2 = vector2.Length();

            float cosTheta = dotProduct / (magnitude1 * magnitude2);

            float angleInRadians = (float)Math.Acos(cosTheta);


            return angleInRadians;
        }
        public static Vector2 GetSymmetryPoint(this Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 lineVector = linePoint2 - linePoint1;
            if (lineVector == Vector2.Zero)
            {
                return point;
            }
            Vector2 aToPoint = point - linePoint1;
            float t = Vector2.Dot(aToPoint, lineVector) / lineVector.LengthSquared();
            Vector2 projection = linePoint1 + t * lineVector;
            Vector2 symmetryPoint = 2 * projection - point;
            return symmetryPoint;
        }
        public static Rectangle getRectCentered(this Vector2 center, float w, float h)
        {
            return new Rectangle((int)(center.X - w / 2), (int)(center.Y - h / 2), (int)w, (int)h);
        }
        public static Rectangle getRectCentered(this Vector2 center, float s)
        {
            return center.getRectCentered(s, s);
        }
        public static void DrawLines(List<Vector2> points, Color color, float width, int wa = 2)
        {
            for (int i = 1; i < points.Count; i++)
            {
                drawLine(Main.spriteBatch, CEExtraAssets.white, points[i - 1], points[i], color, width, wa, true);
            }
        }
        public static void DrawLinesBetter(List<Vector2> points, Color color, float width, int wa = 2)
        {
            for (int i = 1; i < points.Count; i++)
            {
                drawLineBetter(points[i - 1], points[i], color, width, wa, true);
            }
        }
        public static SoundStyle GetSound(string name, float pitch = 1, int maxIns = 4, float volume = 1)
        {
            SoundStyle s = new SoundStyle("CalamityEntropy/Assets/Sounds/" + name);
            s.Pitch = pitch - 1;
            s.Volume = volume;
            s.MaxInstances = maxIns;
            return s;
        }
        public static Dictionary<string, SoundStyle> SoundStyles;
        public static void Update()
        {
        }
        public static void PlaySound(string name, float pitch = 1, Vector2? pos = null, int maxIns = 6, float volume = 1, string path = "CalamityEntropy/Assets/Sounds/")
        {
            if (!Main.dedServ)
            {
                if (!SoundStyles.ContainsKey(path + name))
                {
                    SoundStyles[path + name] = new SoundStyle(path + name);
                }
                SoundStyle s = SoundStyles[path + name];
                s.Pitch = pitch - 1;
                s.Volume = volume;
                s.MaxInstances = maxIns;
                s.LimitsArePerVariant = true;
                SoundEngine.PlaySound(in s, pos);
            }
        }

        public static void UseSampleState_UI(this SpriteBatch sb, SamplerState sampler)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sampler, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }
        public static void UseBlendState_UI(this SpriteBatch sb, BlendState blend)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }
        public static void UseBlendState_UI(this SpriteBatch sb, BlendState blend, SamplerState sample)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, blend, sample, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }
        public static void UseBlendState(this SpriteBatch sb, BlendState blend, SamplerState s = null)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, blend, s == null ? Main.DefaultSamplerState : s, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void UseBlendState(this SpriteBatch sb, BlendState blend, SamplerState s, Effect shader)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, blend, s == null ? Main.DefaultSamplerState : s, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void UseAdditiveClamp(this SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void UseAdditive(this SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void UseSampleState(this SpriteBatch sb, SamplerState s)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, Main.graphics.GraphicsDevice.BlendState, s, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void UseState_UI(this SpriteBatch sb, BlendState blend, SamplerState sampler)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, blend, sampler, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }
        public static void begin_(this SpriteBatch sb)
        {
            sb.Begin(0, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void DrawRectAlt(Rectangle rect, Color color, float width, int num = 16)
        {
            int wa = num > 2 ? 2 : 0;

            drawLine(new Vector2(rect.X + num, rect.Y), new Vector2(rect.X + rect.Width - num, rect.Y), color, width, wa);
            drawLine(new Vector2(rect.X + rect.Width - num, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + num), color, width, wa);
            drawLine(new Vector2(rect.X + rect.Width, rect.Y + num), new Vector2(rect.X + rect.Width, rect.Y + rect.Height - num), color, width, wa);
            drawLine(new Vector2(rect.X + rect.Width, rect.Y + rect.Height - num), new Vector2(rect.X + rect.Width - num, rect.Y + rect.Height), color, width, wa);
            drawLine(new Vector2(rect.X + num, rect.Y + rect.Height), new Vector2(rect.X + rect.Width - num, rect.Y + rect.Height), color, width, wa);
            drawLine(new Vector2(rect.X + num, rect.Y + rect.Height), new Vector2(rect.X, rect.Y + rect.Height - num), color, width, wa);
            drawLine(new Vector2(rect.X, rect.Y + num), new Vector2(rect.X, rect.Y + rect.Height - num), color, width, wa);
            drawLine(new Vector2(rect.X, rect.Y + num), new Vector2(rect.X + num, rect.Y), color, width, wa);
        }
        public static void recordOldPosAndRots(Projectile p, ref List<Vector2> odp, ref List<float> odr, int maxLength = 12)
        {
            odp.Add(p.Center);
            odr.Add(p.rotation);
            if (odp.Count > maxLength)
            {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public static float randomRot()
        {
            return (float)(Main.rand.NextDouble() * MathHelper.Pi * 2);
        }
        public static bool inWorld(int i, int j)
        {
            return !(i < 0 || j < 0 || i >= Main.tile.Width || j >= Main.tile.Height);
        }
        public static bool inWorld(Vector2 v)
        {
            return inWorld((int)(v.X / 16), (int)(v.Y / 16));
        }
        public static Projectile ToProj_Identity(this int id)
        {
            return Main.projectile.FirstOrDefault(x => x.identity == id);
        }
        public static bool isAir(int i, int j, bool plat = false)
        {
            return isAir(new Vector2(i * 16, j * 16), plat);
        }
        public static bool HasTile(Vector2 dp, bool containsPlatform)
        {
            return !isAir(dp, containsPlatform);
        }
        public static bool isAir(Vector2 dp, bool platBlock = false)
        {
            if (dp.X < 0 || dp.Y < 0)
            {
                return true;
            }
            if ((int)(dp.X / 16) >= Main.tile.Width || (int)(dp.Y / 16) >= Main.tile.Height)
                return false;
            Tile tile = Main.tile[(int)(dp.X / 16), (int)(dp.Y / 16)];
            if (tile.IsActuated)
                return true;
            if (platBlock)
            {
                if (tile != null && tile.HasTile)
                {
                    if (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (tile != null && tile.HasTile)
                {
                    if (Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static NPC findTarget(Player player, Projectile proj, int maxDistance, bool check = false)
        {
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active)
            {
                return player.MinionAttackTargetNPC.ToNPC();
            }
            return proj.FindTargetWithinRange(maxDistance, check);
        }
        public static Texture2D getExtraTex(string name)
        {
            return RequestTex("CalamityEntropy/Assets/Extra/" + name);
        }
        public static Asset<Texture2D> getExtraTexAsset(string name)
        {
            return ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/" + name, ReLogic.Content.AssetRequestMode.ImmediateLoad);
        }
        public static Rectangle GetCutTexRect(Texture2D tex, int count, int index, bool hor = true)
        {
            if (hor)
            {
                return new Rectangle(tex.Width / count * index, 0, tex.Width / count, tex.Height);
            }
            return new Rectangle(0, tex.Height / count * index, tex.Width, tex.Height / count);
        }
        public static void DrawAfterimage(Texture2D tx, List<Vector2> odp, List<float> odr, float scale = 1)
        {
            float ap = 1f / (float)odp.Count;
            for (int i = 0; i < odp.Count; i++)
            {
                Main.spriteBatch.Draw(tx, odp[i] - Main.screenPosition, null, Color.White * ap * 0.5f, odr[i], tx.Size() / 2, scale, SpriteEffects.None, 0);
                ap += 1f / (float)odp.Count;
            }
        }
        public static EGlobalItem Entropy(this Item item)
        {
            try
            {
                if (item.TryGetGlobalItem<EGlobalItem>(out var rs))
                    return rs;
            }
            catch { }
            return new EGlobalItem();
        }
        public static bool IsArmor(Item item, bool vanity = false)
        {
            return (vanity || !item.vanity) && (item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1) && item.maxStack == 1;
        }
        public static EGlobalProjectile Entropy(this Projectile p)
        {
            return p.GetGlobalProjectile<EGlobalProjectile>();
        }
        public static EGlobalNPC Entropy(this NPC npc)
        {
            if (npc.TryGetGlobalNPC<EGlobalNPC>(out var rs))
            {
                return rs;
            }
            return new EGlobalNPC();
        }
        public static NPC ToNPC(this int ins)
        {
            return Main.npc[ins];
        }
        public static EModPlayer OwnerEntropy(this Projectile proj)
        {
            if (proj.GetOwner().TryGetModPlayer<EModPlayer>(out var mp))
            {
                return mp;
            }
            return new EModPlayer();
        }
        public static EModPlayer Entropy(this Player player)
        {
            if (player.TryGetModPlayer<EModPlayer>(out var mp))
            {
                return mp;
            }
            return new EModPlayer();
        }

        public static Player GetOwner(this Projectile proj)
        {
            if (proj.owner < 0)
            {
                return null;
            }
            return proj.owner.ToPlayer();
        }
        public static Player ToPlayer(this int ins)
        {
            if (ins < 0 || ins >= Main.player.Length || !Main.player[ins].active)
            {
                return Main.LocalPlayer;
            }
            return Main.player[ins];
        }
        public static Projectile ToProj(this int ins)
        {
            return Main.projectile[ins];
        }
        public static float getRotateAngle(float rNow, float rTo, float rotateSpeed, bool sameSpeed = true)
        {
            float angleNow = MathHelper.ToDegrees(rNow);
            float angleTo = MathHelper.ToDegrees(rTo);
            if (angleNow > 180)
            {
                while (angleNow > 180)
                {
                    angleNow -= 360;
                }
            }
            if (angleNow < -180)
            {
                while (angleNow < -180)
                {
                    angleNow += 360;
                }
            }
            if (angleTo > 180)
            {
                while (angleTo > 180)
                {
                    angleTo -= 360;
                }
            }
            if (angleTo < -180)
            {
                while (angleTo < -180)
                {
                    angleTo += 360;
                }
            }
            float tz = 0;
            if (Math.Abs(angleNow + 360 - angleTo) < Math.Abs(angleTo - angleNow))
            {
                tz = angleTo - angleNow - 360;
            }
            else
            {
                if (Math.Abs(angleTo + 360 - angleNow) < Math.Abs(angleTo - angleNow))
                {
                    tz = angleTo + 360 - angleNow;
                }
                else
                {
                    tz = angleTo - angleNow;
                }
            }
            if (sameSpeed)
            {
                if (tz > rotateSpeed)
                {
                    tz = rotateSpeed;
                }
                if (tz < (rotateSpeed * -1))
                {
                    tz = rotateSpeed * -1;
                }
            }
            else
            {
                tz *= rotateSpeed;
            }
            return MathHelper.ToRadians(tz);

        }

        public static float ToRadians(this float f)
        {
            return MathHelper.ToRadians(f);
        }
        public static float RotateTowardsAngle(float currentRadians, float targetRadians, float rotateSpeed, bool useFixedSpeed = true)
        {
            currentRadians = MathHelper.WrapAngle(currentRadians);
            targetRadians = MathHelper.WrapAngle(targetRadians);

            float difference = targetRadians - currentRadians;
            float turnAmount = MathHelper.WrapAngle(difference);

            if (useFixedSpeed)
            {
                turnAmount = MathHelper.Clamp(turnAmount, -rotateSpeed, rotateSpeed);
            }
            else
            {
                turnAmount *= MathHelper.Clamp(rotateSpeed, 0f, 1f);
            }

            return currentRadians + turnAmount;
        }

        public static void wormFollow(int npc1, int npc2, int spacing = 48, bool type2 = false, float t2speed = 0.2f, float jrot = 0, float angleP = 0f)
        {
            if (type2)
            {
                NPC npc = Main.npc[npc1];
                NPC targetNPC = Main.npc[npc2];
                float rot = npc.rotation - jrot;
                npc.rotation = RotateTowardsAngle(rot, targetNPC.rotation + angleP - jrot, t2speed, false) + jrot;
                npc.Center = targetNPC.Center;
                Vector2 displacement = (RotateTowardsAngle(rot, targetNPC.rotation + angleP - jrot, t2speed, false)).ToRotationVector2() * -1 * spacing;
                npc.Center += displacement;
            }
            else
            {
                NPC npc = Main.npc[npc1];
                NPC targetNPC = Main.npc[npc2];
                float angle = (float)Math.Atan2(targetNPC.Center.Y - npc.Center.Y, targetNPC.Center.X - npc.Center.X);
                npc.rotation = angle + angleP + jrot;
                npc.Center = targetNPC.Center;
                Vector2 displacement = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * -1 * spacing;
                npc.Center += displacement;

            }
        }
        public static void drawChain(Vector2 startPos, Vector2 endPos, int spacing, Texture2D tx, Color color)
        {
            int distance = ((int)Math.Sqrt(Math.Pow(endPos.X - startPos.X, 2) + Math.Pow(endPos.Y - startPos.Y, 2)));
            float rot = (endPos - startPos).ToRotation();
            float px = startPos.X;
            float py = startPos.Y;
            int num = ((int)(distance / spacing));
            Vector2 addVec = new Vector2((endPos.X - startPos.X) / num, (endPos.Y - startPos.Y) / num);
            addVec.Normalize();
            float adx = (endPos.X - startPos.X) / num;
            float ady = (endPos.Y - startPos.Y) / num;
            Vector2 drawPos = new Vector2(px, py);
            for (int i = 0; i <= num; i++)
            {
                color = Lighting.GetColor((drawPos / 16).ToPoint());
                Main.EntitySpriteDraw(tx, drawPos - Main.screenPosition, null, color, rot, new Vector2(tx.Width / 2, tx.Height / 2), (new Vector2(1, 1)), SpriteEffects.None, 0);
                drawPos.X += addVec.X * spacing;
                drawPos.Y += addVec.Y * spacing;
            }
        }
        public static void drawChain(Vector2 startPos, Vector2 endPos, int spacing, string texturePath, Color color)
        {
            drawChain(startPos, endPos, spacing, RequestTex(texturePath), color);
        }
        public static void drawChain(Vector2 startPos, Vector2 endPos, int spacing, string texturePath)
        {
            drawChain(startPos, endPos, spacing, RequestTex(texturePath), Color.White);
        }
        public static void drawChain(Vector2 startPos, Vector2 endPos, int spacing, Texture2D texture)
        {
            drawChain(startPos, endPos, spacing, texture, Color.White);
        }
        public static float getDistance(Vector2 v1, Vector2 v2)
        {
            return ((float)Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2)));
        }

        /// <summary>
        /// 施加固定时长的减益:原版对 LongerExpertDebuff 集合内的减益(带电等)在专家/大师会按 DebuffTimeMultiplier 延长,
        /// 这里 AddBuff 后把 buffTime 钉回 time,只对本地玩家有效(减益归属其自身客户端)
        /// </summary>
        public static void AddDebuffFixed(this Player player, int type, int time)
        {
            if (player.whoAmI != Main.myPlayer)
                return;
            player.AddBuff(type, time, true);
            int idx = player.FindBuffIndex(type);
            if (idx >= 0 && player.buffTime[idx] > time)
            {
                player.buffTime[idx] = time;
            }
        }


        public static void drawTexture(Texture2D tex, Vector2 pos, float rotation, Color color, Vector2 scale, SpriteEffects eff = SpriteEffects.None)
        {
            Rectangle rectangle = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origin = rectangle.Size() / 2f;
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle?(rectangle), color, rotation, origin, scale, eff, 0f);
        }
        public static bool LineThroughRect(Vector2 start, Vector2 end, Rectangle rect, int lineWidth = 4)
        {
            float point = 0f;
            return rect.Contains((int)start.X, (int)start.Y) || rect.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(rect.TopLeft(), rect.Size(), start, end, lineWidth, ref point);
        }

        //不碰SpriteBatch状态,假定外面批次已开好;PRT PreDraw里直接Draw用
        public static void drawLine(SpriteBatch spriteBatch, Texture2D px, Vector2 start, Vector2 end, Color color, float width, int wa = 0, bool worldpos = true)
        {
            spriteBatch.Draw(px, start - (worldpos ? Main.screenPosition : Vector2.Zero), null, color, (end - start).ToRotation(), new Vector2(0, 0.5f), new Vector2(getDistance(start, end) + wa, width), SpriteEffects.None, 0);
        }
        public static void drawLine(Vector2 start, Vector2 end, Color color, float width, int wa = 0, bool worldpos = true)
        {
            Main.spriteBatch.Draw(CEExtraAssets.white, start - (worldpos ? Main.screenPosition : Vector2.Zero), null, color, (end - start).ToRotation(), new Vector2(0, 0.5f), new Vector2(getDistance(start, end) + wa, width), SpriteEffects.None, 0);
        }
        public static void drawLineBetter(Vector2 start, Vector2 end, Color color, float width, int wa = 0, bool worldpos = true)
        {
            var tex = BasicTrailThinTex.Value;
            Main.spriteBatch.Draw(tex, start - (worldpos ? Main.screenPosition : Vector2.Zero), null, color, (end - start).ToRotation(), new Vector2(0, tex.Height / 2), new Vector2((getDistance(start, end) + wa) / 200f, width / 40f), SpriteEffects.None, 0);
        }
        public static void drawTextureToPoint(SpriteBatch sb, Texture2D texture, Color color, Vector2 lu, Vector2 ru, Vector2 ld, Vector2 rd)
        {
            sb.End();

            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();

            ve.Add(new ColoredVertex(lu,
                      new Vector3(0, 0, 1),
                      color));
            ve.Add(new ColoredVertex(ld,
                      new Vector3(0, 1, 1),
                      color));
            ve.Add(new ColoredVertex(ru,
                      new Vector3(1, 0, 1),
                      color));
            ve.Add(new ColoredVertex(rd,
                      new Vector3(1, 1, 1),
                      color));


            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Texture2D tx = texture;
            gd.Textures[0] = tx;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public static Vector2 getTxP(List<Vector2> points, float p)
        {
            float dl = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float ds = getDistance(points[i], points[i + 1]);
                if (dl + ds < p)
                {
                    dl += ds;
                }
                else
                {
                    float pc = (p - dl) / ds;
                    return points[i] + (points[i + 1] - points[i]) * pc;
                }
            }

            return points[points.Count - 1];
        }
        public static void drawLaser(SpriteBatch sb, List<Texture2D> txs, List<Vector2> points, int txLength, Color color, int width = 64, int starttx = 0, float startRot = 0)
        {
            for (int j = 0; j < points.Count; j++)
            {
                points[j] -= Main.screenPosition;
            }
            float dl = 0;
            float al = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                al += getDistance(points[i], points[i + 1]);
            }
            int txc = starttx;
            float lr = startRot; Vector2 tp = Vector2.Zero;
            while (true)
            {
                Texture2D tx = txs[txc % txs.Count];
                if (dl > al)
                {
                    break;
                }
                if (dl + txLength > al)
                {
                    Vector2 dp = getTxP(points, dl);
                    Vector2 de = getTxP(points, dl + txLength);
                    tp = de;
                    float rot = (points[points.Count - 1] - points[points.Count - 2]).ToRotation();
                    Vector2 lrof = lr.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * width / 2;
                    Vector2 rof = rot.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * width / 2;
                    drawTextureToPoint(sb, tx, color, dp + lrof, de + rof, dp - lrof, de - rof);
                    lr = rot;
                    dl += txLength;
                    break;
                }
                else
                {
                    Vector2 dp = getTxP(points, dl);
                    Vector2 de = getTxP(points, dl + txLength);
                    tp = de;
                    float rot = (de - dp).ToRotation();
                    Vector2 lrof = lr.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * width / 2;
                    Vector2 rof = rot.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * width / 2;
                    drawTextureToPoint(sb, tx, color, dp + lrof, de + rof, dp - lrof, de - rof);
                    lr = rot;
                    dl += txLength;
                }

                txc += 1;
            }
        }
        #region Localization
        public static string LocalPrefix => "Mods.CalamityEntropy";
        /// <summary>
        /// 干翻所有Tooltip，并借助本地化完全重写一次
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="replacedTextPath"></param>
        public static void FuckThisTooltipAndReplace(this List<TooltipLine> tooltips, string replacedTextPath)
        {
            tooltips.RemoveAll((line) => line.Mod == "Terraria" && line.Name != "Tooltip0" && line.Name.StartsWith("Tooltip"));
            TooltipLine getTooltip = tooltips.FirstOrDefault((x) => x.Name == "Tooltip0" && x.Mod == "Terraria");
            if (getTooltip is not null)
                getTooltip.Text = Language.GetTextValue(replacedTextPath);
        }
        /// <summary>
        /// 干翻所有Tooltip，并借助本地化完全重写一次，重载染色，附带键入值
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="replacedTextPath"></param>
        /// <param name="args"></param>
        public static void FuckThisTooltipAndReplace(this List<TooltipLine> tooltips, string replacedTextPath, Color textColor, params object[] args)
        {
            tooltips.RemoveAll((line) => line.Mod == "Terraria" && line.Name != "Tooltip0" && line.Name.StartsWith("Tooltip"));
            TooltipLine getTooltip = tooltips.FirstOrDefault((x) => x.Name == "Tooltip0" && x.Mod == "Terraria");
            string formateText = replacedTextPath.ToLangValue().ToFormatValue(args);
            if (getTooltip is not null)
            {
                getTooltip.Text = formateText;
                getTooltip.OverrideColor = textColor;
            }

        }
        /// <summary>
        /// 干翻所有Tooltip，并借助本地化完全重写一次，附带键入值
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="replacedTextPath"></param>
        /// <param name="args"></param>
        public static void FuckThisTooltipAndReplace(this List<TooltipLine> tooltips, string replacedTextPath, params object[] args)
        {
            tooltips.RemoveAll((line) => line.Mod == "Terraria" && line.Name != "Tooltip0" && line.Name.StartsWith("Tooltip"));
            TooltipLine getTooltip = tooltips.FirstOrDefault((x) => x.Name == "Tooltip0" && x.Mod == "Terraria");
            string formateText = replacedTextPath.ToLangValue().ToFormatValue(args);
            if (getTooltip is not null)
                getTooltip.Text = formateText;
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需填入本地化路径
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textPath"></param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltip(this List<TooltipLine> tooltips, string textPath, Mod mod = null, string LineName = "CEMod")
        {
            string text = textPath.ToLangValue();
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = tooltips.Count > 0 ? tooltips[^1].OverrideColor : Color.White
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需填入本地化路径，重载传参方法
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textPath"></param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltip(this List<TooltipLine> tooltips, string textPath, Mod mod = null, string LineName = "CEMod", params object[] args)
        {
            string text = textPath.ToLangValue().ToFormatValue(args);
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = tooltips.Count > 0 ? tooltips[^1].OverrideColor : Color.White
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需填入本地化路径，重载颜色代码
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textPath"></param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltip(this List<TooltipLine> tooltips, string textPath, Color color, Mod mod = null, string LineName = "CEMod")
        {
            string text = textPath.ToLangValue();
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = color
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需填入本地化路径，重载传参方法，颜色代码
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textPath"></param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltip(this List<TooltipLine> tooltips, string textPath, Color color, Mod mod = null, string LineName = "CEMod", params object[] args)
        {
            string text = textPath.ToLangValue().ToFormatValue(args);
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = color
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需直接传入需要的文本内容而不是对应的本地化路径
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textValue"></param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltipDirect(this List<TooltipLine> tooltips, string textValue, Mod mod = null, string LineName = "CEMod")
        {
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, textValue)
            {
                OverrideColor = tooltips.Count > 0 ? tooltips[^1].OverrideColor : Color.White
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需直接传入需要的文本内容而不是对应的本地化路径，重载传参方法
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textValue"></param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltipDirect(this List<TooltipLine> tooltips, string textValue, Mod mod = null, string LineName = "CEMod", params object[] args)
        {
            string text = textValue.ToFormatValue(args);
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = tooltips.Count > 0 ? tooltips[^1].OverrideColor : Color.White
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需直接传入需要的文本内容而不是对应的本地化路径，重载颜色代码
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textValue">文本内容</param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltipDirect(this List<TooltipLine> tooltips, string textValue, Color color, Mod mod = null, string LineName = "CEMod")
        {
            string text = textValue.ToLangValue();
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = color
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 从最后一行Tooltip后插入值，需直接传入需要的文本内容而不是对应的本地化路径，需直接传入需要的文本内容而不是对应的本地化路径，重载传参方法，颜色代码
        /// </summary>
        /// <param name="tooltips"></param>
        /// <param name="textValue">文本内容</param>
        /// <param name="mod">该段文本所属的模组，默认值null，将直接选定为本mod</param>
        /// <param name="LineName">为这一行tooltip起名，默认CEMod</param>
        public static void QuickAddTooltipDirect(this List<TooltipLine> tooltips, string textValue, Color color, Mod mod = null, string LineName = "CEMod", params object[] args)
        {
            string text = textValue.ToFormatValue(args);
            Mod tooltipMod = mod ?? CalamityEntropy.Instance;
            var newLine = new TooltipLine(tooltipMod, LineName, text)
            {
                OverrideColor = color
            };
            if (tooltips.Count is 0)
                tooltips.Add(newLine);
            else
                tooltips.Insert(tooltips.Count, newLine);
        }
        /// <summary>
        /// 将整型、浮点与双精度直接变成带百分比符号的字符串，用于进行Tooltip的插值。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToPercentReal(this object obj)
        {
            if (obj is int interga)
                return $"{interga}%";
            if (obj is float floatSingle)
                return $"{(int)(floatSingle * 100f)}%";
            if (obj is double doubleSingle)
                return $"{(int)(doubleSingle * 100)}%";
            return "转化出错";

        }
        public static string ToHexColor(this Color color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";

        public static string ToLangValue(this string textPath) => Language.GetTextValue(textPath);

        public static string ToFormatValue(this string baseTextValue, params object[] args)
        {
            try
            {
                return string.Format(baseTextValue, args);
            }
            catch
            {
                return baseTextValue + "格式化出错";
            }
        }
        #endregion
        /// <summary>
        /// 获取玩家到鼠标位置的单位向量
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static Vector2 GetPlayerToMouseVector2(this Player player)
        {
            Vector2 vec = Main.MouseWorld - player.Center;
            vec = vec.SafeNormalize(Vector2.UnitX);
            return vec;
        }
        public static void QuickDrawWithTrailing(this Projectile proj, float offset, Color color, float rotFix = 0) => QuickDrawWithTrailing(proj, offset, color, proj.Center, proj.scale, 4, rotFix);
        public static void QuickDrawWithTrailing(this Projectile proj, float offset, Color color, int drawTime, float rotFix = 0) => QuickDrawWithTrailing(proj, offset, color, proj.Center, proj.scale, drawTime, rotFix);
        public static void QuickDrawWithTrailing(this Projectile proj, float offset, Color color, int drawTime, Vector2 drawCenter, float rotFix = 0) => QuickDrawWithTrailing(proj, offset, color, drawCenter, proj.scale, drawTime, rotFix);
        public static void QuickDrawWithTrailing(this Projectile proj, float offset, Color color, int drawTime, Vector2 drawCenter, float scale, float rotFix = 0) => QuickDrawWithTrailing(proj, offset, color, drawCenter, scale, drawTime, rotFix);
        public static void QuickDrawWithTrailing(this Projectile proj, float offset, Color color, Vector2 drawCenter, float scale, int drawTime = 4, float rotFix = 0)
        {
            Texture2D tex = proj.GetTexture();
            Vector2 orig = tex.Size() / 2;
            Vector2 drawPos = drawCenter - Main.screenPosition;
            for (int i = 1; i < drawTime; i++)
            {
                Vector2 trailingDrawPos = drawPos - proj.velocity * i * offset;
                float faded = 1 - i / (float)drawTime;
                //平方放缩
                faded = MathF.Pow(faded, 2);
                Color trailColor = color * faded;
                Main.spriteBatch.Draw(tex, trailingDrawPos, null, trailColor, proj.oldRot[i] + rotFix, orig, scale, 0, 0);
            }
            //直接绘制主射弹位于最顶层
            Main.spriteBatch.Draw(tex, drawPos, null, color, proj.rotation + rotFix, orig, scale, 0, 0.1f);
        }
        public static void QuickDrawItemWithBloomToWorld(this Item item, SpriteBatch SB, Color color, ref float scale, float rot)
        {
            Texture2D tex = TextureAssets.Item[item.type].Value;
            Vector2 position = item.position - Main.screenPosition + tex.Size() / 2;
            Rectangle iFrame = tex.Frame();
            for (int i = 0; i < 16; i++)
                SB.Draw(tex, position + MathHelper.ToRadians(i * 60f).ToRotationVector2() * 4f, null, color with { A = 0 }, rot, tex.Size() / 2, scale, 0, 0f);
            SB.Draw(tex, position, iFrame, Color.White, rot, tex.Size() / 2, scale, 0f, 0f);
        }
        public static SpriteEffects FlipHorizonHandler(this Projectile projectile)
        {
            return projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }
        /// <summary>
        /// 为你的射弹绘制一个发光描边。基于射弹本体颜色
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="totalDrawTime"></param>
        /// <param name="posMove"></param>
        public static void QuickDrawBloomEdge(this Projectile proj, int totalDrawTime = 8, float rotOffset = 0, float posMove = 2f)
        {
            QuickDrawBloomEdge(proj, Color.White, totalDrawTime, rotOffset, posMove);
        }
        /// <summary>
        /// 为你的射弹绘制一个发光描边。基于射弹本体，重载输入颜色
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="totalDrawTime"></param>
        /// <param name="posMove"></param>
        public static void QuickDrawBloomEdge(this Projectile proj, Color color, int totalDrawTime = 8, float rotOffset = 0, float posMove = 2f)
        {
            for (int i = 0; i < totalDrawTime; i++)
            {
                Main.spriteBatch.Draw(proj.GetTexture(), proj.Center - Main.screenPosition + MathHelper.ToRadians(i * 60f).ToRotationVector2() * posMove, null, color with { A = 0 }, proj.rotation + rotOffset, proj.GetTexture().Size() / 2, proj.scale, 0, 0f);
            }
        }
        #region 搜索boss掉落物
        /// <summary>
        /// 快速遍历单个Boss所有掉落物并存入字典
        /// </summary>
        /// <typeparam name="T">NPC类型</typeparam>
        /// <param name="includeMaterial">是否包含材料</param>
        /// <returns></returns>
        public static List<int> FindLoots<T>(bool includeMaterial = true) where T : ModNPC => FindLoots(ModContent.NPCType<T>(), includeMaterial);
        /// <summary>
        /// 遍历单个boss所有的掉落物并存入字典
        /// </summary>
        /// <param name="type">NPC类型</param>
        /// <param name="includeMaterial">是否包含材料</param>
        /// </summary>
        public static List<int> FindLoots(int type, bool includeMaterial = true, Mod mod = null)
        {
            mod ??= CalamityEntropy.Instance;

            var list = new List<int>();
            List<IItemDropRule> rulesForNPCID = Main.ItemDropsDB.GetRulesForNPCID(type, false);
            List<DropRateInfo> list2 = [];
            DropRateInfoChainFeed ratesInfo = new(1f);
            foreach (var rule in rulesForNPCID)
            {
                //脱离灾厄:天顶世界掉落条件改原版判定(原灾厄DropHelper.GFB;原版类型名为ZenithSeedIsUp)
                if (rule is LeadingConditionRule lcr && lcr.condition is Conditions.ZenithSeedIsUp)
                    continue;
                rule.ReportDroprates(list2, ratesInfo);
            }
            list.AddRange(list2.Where(i => IsNotMaterial(ContentSamples.ItemsByType[i.itemId], mod, includeMaterial)).Select(item2 => item2.itemId));

            List<int> bagdrops = [];
            foreach (var bag in list)
            {
                var baglist = Main.ItemDropsDB.GetRulesForItemID(bag);
                if (baglist.Count > 0)
                {
                    List<DropRateInfo> list3 = [];
                    foreach (var rule in baglist)
                    {
                        if (rule is LeadingConditionRule lcr && lcr.condition is Conditions.ZenithSeedIsUp) continue;
                        rule.ReportDroprates(list3, ratesInfo);
                    }
                    bagdrops.AddRange(list3.Where(i => IsNotMaterial(ContentSamples.ItemsByType[i.itemId], mod, includeMaterial)).Select(i3 => i3.itemId));
                }
            }
            list.AddRange(bagdrops);
            return list;
        }
        public static bool IsNotMaterial(Item item, Mod mod, bool dontNeedCheck = true)
        {
            if (item.ModItem != null)
            {
                if (item.ModItem.Mod != mod)
                    return false;
            }
            if (dontNeedCheck)
                return true;
            if (item.damage > 0 && item.ammo <= 0)
                return true;
            if (item.accessory || item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0)
                return false;
            return false;
        }
        #endregion
        public static string InvisAsset => "CalamityEntropy/Assets/InvisibleProj";

        public static readonly BlendState ColorInverse = new BlendState()
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
        };

        public static readonly BlendState SubtractiveBlending = new BlendState
        {
            ColorBlendFunction = BlendFunction.ReverseSubtract,
            ColorDestinationBlend = Blend.One,
            ColorSourceBlend = Blend.SourceAlpha,
            AlphaBlendFunction = BlendFunction.ReverseSubtract,
            AlphaDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.SourceAlpha
        };

        /// <summary>
        /// 新的追踪方法，这个会指定一个NPC, 且可以自定义输入额外更新，以及强制速度不受距离影响
        /// 目前没有角度限制等一类的东西，如果需要则可以补上。
        /// </summary>
        /// <param name="proj">射弹</param>
        /// <param name="target">射弹目标</param>
        /// <param name="distRequired">最大范围</param>
        /// <param name="speed">射弹速度</param>
        /// <param name="inertia">惯性</param>
        /// <param name="giveExtraUpdate">给予额外更新，默认1</param>
        /// <param name="forceSpeed">指定射弹无视距离，使射弹使用你输入的速度。这个效果有一个距离特判，即距离比你输入的射弹速度还短的时候才会生效, 一般可无视。</param>
        /// <param name="maxAngleChage">角度限制，默认为空. </param>
        /// <param name="ignoreDist">使这个射弹无视索敌距离(distRequired), 默认取否. </param>
        public static void HomingNPCBetter(this Projectile proj, NPC target, float distRequired, float speed, float inertia, int giveExtraUpdate = 0, float? forceSpeed = null, float? maxAngleChage = null, bool ignoreDist = false)
        {
            //一般来说你用这个方法就说明target理论上应当可以被追，但……just in case
            if (!proj.friendly || target == null || !target.active)
                return;
            bool canHome;

            float curDist = Vector2.Distance(target.Center, proj.Center);
            //存储射弹当前额外更新
            if (proj.GetGlobalProjectile<EGlobalProjectile>().StoredEU == -1)
                proj.GetGlobalProjectile<EGlobalProjectile>().StoredEU = proj.extraUpdates;

            if (!target.chaseable || curDist > distRequired && !ignoreDist)
                canHome = false;
            else
                canHome = true;
            if (canHome)
            {
                //给予额外更新
                proj.extraUpdates = proj.GetGlobalProjectile<EGlobalProjectile>().StoredEU + giveExtraUpdate;
                //开始追踪target
                Vector2 home = (target.Center - proj.Center).SafeNormalize(Vector2.UnitY);
                Vector2 velo = (proj.velocity * inertia + home * speed) / (inertia + 1f);
                //这里给了一个角度限制
                if (maxAngleChage.HasValue)
                {
                    float curAngle = proj.velocity.ToRotation();
                    float tarAngle = velo.ToRotation();
                    float angleDiffer = MathHelper.WrapAngle(tarAngle - curAngle);
                    //转弧度
                    float maxRadians = MathHelper.ToRadians(maxAngleChage.Value);
                    if (Math.Abs(angleDiffer) > maxRadians)
                    {
                        float clampedAngle = curAngle + Math.Sign(angleDiffer) * maxRadians;
                        float setSpeed = velo.Length();
                        velo = new Vector2((float)Math.Cos(clampedAngle), (float)Math.Sin(clampedAngle)) * setSpeed;
                    }
                }
                //除非你当前距离比射弹速度还少, 我们才会重新设定速度
                if (forceSpeed.HasValue && curDist < speed)
                    velo = proj.velocity.SafeNormalize(Vector2.Zero) * home * forceSpeed.Value;
                //设定速度
                proj.velocity = velo;
            }
            //否则返回射弹原本的额外更新
            else
                proj.extraUpdates = proj.GetGlobalProjectile<EGlobalProjectile>().StoredEU;
        }
        /// <summary>
        /// 重载追踪方法，直接快速设定无视距离的追踪
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="target"></param>
        /// <param name="speed"></param>
        /// <param name="inertia"></param>
        /// <param name="giveExtraUpdate"></param>
        /// <param name="forceSpeed"></param>
        /// <param name="maxAngleChage"></param>
        public static void HomingNPCBetter(this Projectile proj, NPC target, float speed, float inertia, int giveExtraUpdate = 0, float? forceSpeed = null, float? maxAngleChage = null) => proj.HomingNPCBetter(target, 1f, speed, inertia, giveExtraUpdate, forceSpeed, maxAngleChage, true);

        /// <summary>
        /// 数学公式：将角度转化为椭圆上的一个点
        /// </summary>
        /// <param name="radians">当前点的弧度</param>
        /// <param name="shortAxis">半短轴长度(短半径)</param>
        /// <param name="longAxis">半长轴长度(长半径)</param>
        /// <param name="rotation">椭圆整体旋转角度(弧度)</param>
        /// <returns>椭圆上相对于原点的点坐标</returns>
        public static Vector2 ToEllipseVector2Edge(this float radians, float shortAxis, float longAxis, float rotation = 0f)
        {
            float x = longAxis * (float)Math.Cos(radians);
            float y = shortAxis * (float)Math.Sin(radians);
            float cosRot = (float)Math.Cos(rotation);
            float sinRot = (float)Math.Sin(rotation);
            float rotX = x * cosRot - y * sinRot;
            float rotY = x * sinRot + y * cosRot;
            return new Vector2(rotX, rotY);
        }
        public static void ClearInvalidPoint(this Projectile proj, out List<Vector2> validPos, out List<float> validRot, Vector2[] rawPosList = null, float[] rawRotList = null)
        {
            validPos = [];
            validRot = [];
            Vector2[] rawPos = rawPosList ?? proj.oldPos;
            float[] rawRot = rawRotList ?? proj.oldRot;
            for (int i = 0; i < rawPos.Length; i++)
            {
                if (rawPos[i] == Vector2.Zero)
                    continue;
                validPos.Add(rawPos[i]);
                validRot.Add(rawRot[i]);
            }
        }
        /// <summary>
        /// 用于搜索距离射弹最近的npc单位，并返回NPC实例。
        /// </summary>
        /// <param name="p">射弹</param>
        /// <param name="maxDist">最大搜索距离</param>
        /// <param name="ignoreTiles">穿墙搜索, 默认为</param>
        /// <param name="arrayFirst">数组优先, 这个将会使射弹优先针对数组内第一个单位,默认为否</param>
        /// <returns>返回一个NPC实例</returns>
        public static NPC FindClosestTarget(this Projectile p, float maxDist, bool ignoreTiles = true, bool arrayFirst = false)
        {
            //bro我真的要遍历整个NPC吗？
            float distStoraged = maxDist;
            NPC acceptableTarget = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float exDist = npc.width + npc.height;
                //单位不可被追踪 或者 超出索敌距离则continue
                if (Vector2.Distance(p.Center, npc.Center) > distStoraged + exDist)
                    continue;

                if (!npc.active || npc.friendly || npc.lifeMax < 5 || !npc.CanBeChasedBy(p.Center, false))
                    continue;

                //搜索符合条件的敌人, 准备返回这个NPC实例
                float curNpcDist = Vector2.Distance(npc.Center, p.Center);
                if (curNpcDist < distStoraged && (ignoreTiles || Collision.CanHit(p.Center, 1, 1, npc.Center, 1, 1)))
                {
                    distStoraged = curNpcDist;
                    acceptableTarget = npc;
                    //如果是数组优先，直接在这返回实例
                    if (arrayFirst)
                        return acceptableTarget;
                }
            }
            //返回这个NPC实例
            return acceptableTarget;
        }
        /// <summary>
        /// 为射弹获取目标，重载Out与判定方法
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="target"></param>
        /// <param name="targetIndex"></param>
        /// <param name="anotherDistance"></param>
        /// <returns></returns>
        public static bool GetTargetSafe(this Projectile proj, out NPC target, int? targetIndex = null, bool canSearchSecondTarget = true, float anotherDistance = 1800f)
        {
            NPC npc;
            if (targetIndex.HasValue)
            {
                npc = Main.npc[targetIndex.Value];
                //当前敌人不可被追踪，跳过这一步并进行下一步
                if (!npc.CanBeChasedBy(proj) || canSearchSecondTarget)
                    npc = proj.FindClosestTarget(anotherDistance);
                else
                    npc = null;
            }
            else
                npc = proj.FindClosestTarget(anotherDistance);

            target = npc;
            return npc != null;
        }

        /// <summary>
        /// 基于当前速度与基准速度比例动态计算部分间隔类的数值。（如用于生成频率和触发间隔等）
        /// 速度越快，间隔越小，速度越慢，间隔越大
        /// </summary>
        /// <param name="baseRates">基准间隔</param>
        /// <param name="minRates">最小间隔限制</param>
        /// <param name="maxRates">最大间隔限制</param>
        /// <param name="baseSpeed">基准速度</param>
        /// <param name="curSpeed">当前实际速度</param>
        /// <returns>被动态调整后的整数间隔值（四舍五入取整）</returns>
        /// <remarks>
        /// 使用示例：部分受到速度影响导致总体生成频率被降低的射弹生成（如夜明锤子）
        /// </remarks>
        public static int RatesBaseOnSpeed(float baseRates, float minRates, float maxRates, float baseSpeed, float curSpeed)
        {
            //计算当前速度的模长
            float dynamicSpawnSpeed = (baseSpeed / curSpeed) * baseRates;
            //基于速度间隔进行刻计算
            dynamicSpawnSpeed = MathHelper.Clamp(dynamicSpawnSpeed, minRates, maxRates);
            //控制在合理范围内
            int spawnRates = (int)Math.Round(dynamicSpawnSpeed);
            //返回
            return spawnRates;
        }
        /// <summary>
        /// 使射弹较为平滑地冲向一个地点。
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="targetPosition"></param>
        /// <param name="speed"></param>
        /// <param name="acceleration"></param>
        /// <param name="killDistance"></param>
        public static void AccelerateToTarget(this Projectile proj, Vector2 targetPosition, float speed, float acceleration, int killDistance = 0)
        {
            Vector2 dist = targetPosition - proj.Center;
            float distLength = dist.Length();
            distLength = speed / distLength;
            dist.X *= distLength;
            dist.Y *= distLength;
            if (proj.velocity.X < dist.X)
            {
                proj.velocity.X += acceleration;
                if (proj.velocity.X < 0f && dist.X > 0f)
                    proj.velocity.X += acceleration;
            }
            else if (proj.velocity.X > dist.X)
            {
                proj.velocity.X -= acceleration;
                if (proj.velocity.X > 0f && dist.X < 0f)
                    proj.velocity.X -= acceleration;
            }
            if (proj.velocity.Y < dist.Y)
            {
                proj.velocity.Y += acceleration;
                if (proj.velocity.Y < 0f && dist.Y > 0f)
                    proj.velocity.Y += acceleration;
            }
            else if (proj.velocity.Y > dist.Y)
            {
                proj.velocity.Y -= acceleration;
                if (proj.velocity.Y > 0f && dist.Y < 0f)
                    proj.velocity.Y -= acceleration;
            }
        }
        public static void BeginDefault(this SpriteBatch SB) =>
    SB.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        #region ShaderSB
        //EnterShaderRegion/ExitShaderRegion在InnoVault里,这批是CEUtils自管的End+Begin换shader批次
        //PRT里WindParticle/Trail那类:Enter画完图元后还得End+BeginDrawingWithMode,光Exit不够
        public static void BeginShader(this SpriteBatch SB) =>
            SB.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        public static void BeginShader(this SpriteBatch SB, BlendState blendState) =>
            SB.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        public static void BeginShader(this SpriteBatch SB, BlendState blendState, Matrix matrix) =>
            SB.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        public static void BeginShader(this SpriteBatch SB, BlendState blendState, SamplerState samplerState) =>
            SB.Begin(SpriteSortMode.Immediate, blendState, samplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        public static void BeginShader(this SpriteBatch SB, BlendState blendState, SamplerState samplerState, Matrix matrix) =>
            SB.Begin(SpriteSortMode.Immediate, blendState, samplerState, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        public static void ReSetToBeginShader()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void ReSetToBeginShader(BlendState blendState)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void ReSetToBeginShader(BlendState blendState, Matrix matrix)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, matrix);
        }
        public static void ReSetToBeginShader(BlendState blendState, SamplerState samplerState)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, samplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void ReSetToBeginShader(BlendState blendState, SamplerState samplerState, Matrix matrix)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, samplerState, DepthStencilState.None, Main.Rasterizer, null, matrix);
        }
        //shader/图元画完还回Deferred+AlphaBlend,跟ExitShaderRegion收尾语义接近
        public static void ReSetToEndShader()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public static void BeginDrawVertex(this SpriteBatch SB, SpriteSortMode SM = SpriteSortMode.Immediate) => SB.Begin(SM, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        /// <summary>
        /// 快速生成一个简单明了的圆形粒子组
        /// </summary>
        /// <param name="dPos"></param>
        /// <param name="dCounts"></param>
        /// <param name="dScale"></param>
        /// <param name="dType"></param>
        /// <param name="dSpeed"></param>
        /// <param name="dPosOffset"></param>
        /// <param name="dGrav"></param>
        /// <param name="dAlpha"></param>
        public static void CirclrDust(this Vector2 dPos, int dCounts, float dScale, int dType, int dSpeed, float dPosOffset = 0f, bool dGrav = true, int dAlpha = 255)
        {
            float rotArg = 360f / dCounts;
            for (int i = 0; i < dCounts; i++)
            {
                float rot = MathHelper.ToRadians(i * rotArg);
                Vector2 offsetPos = new Vector2(dPosOffset, 0f).RotatedBy(rot);
                Vector2 dVel = new Vector2(dSpeed, 0f).RotatedBy(rot);
                Dust d = Dust.NewDustPerfect(dPos + offsetPos, dType, dVel);
                d.noGravity = dGrav;
                d.velocity = dVel;
                d.scale = dScale;
                d.alpha = dAlpha;
            }
        }

        public static float ToClamp(this float value, float min = 0f, float max = 1f) => MathHelper.Clamp(value, min, max);
        public static bool OutOffScreen(this Vector2 pos)
        {
            if (pos.X < Main.screenPosition.X - Main.screenWidth / 2)
                return true;
            if (pos.Y < Main.screenPosition.Y - Main.screenHeight / 2)
                return true;

            if (pos.X > Main.screenPosition.X + Main.screenWidth * 1.5f)
                return true;
            if (pos.Y > Main.screenPosition.Y + Main.screenHeight * 1.5f)
                return true;

            return false;
        }
        #endregion

        //冷却:统一走自研冷却框架(CalamityEntropy.Core.Cooldowns),用法见 Doc/decouple/cooldown-api.md

        #region Zone等价判定(已按biome-map.md表外补充规则落实)
        /// <summary>原灾厄 ZoneAstral(星辉瘟疫)的自有等价:发光蘑菇群系。</summary>
        public static bool ZoneAstralPlaceholder(Player player) => player.ZoneGlowshroom;
        /// <summary>原灾厄 ZoneSulphur(硫磺海)的自有等价:海滩。</summary>
        public static bool ZoneSulphurPlaceholder(Player player) => player.ZoneBeach;
        /// <summary>原灾厄 ZoneAbyss 系的自有等价:洞穴层海侧。</summary>
        public static bool ZoneAbyssPlaceholder(Player player) => player.ZoneRockLayerHeight && player.ZoneBeach;
        #endregion

        #region 灾厄工具函数同名移植(脱离灾厄自研等效实现,签名与原版灾厄一致,供既有调用点机械替换)
        /// <summary>
        /// NPC 是否为有机体(移植自灾厄 NPCUtils.Organic,自写等价),用于命中音效/吸血类逻辑分流。
        /// 判定依据受击音效:金属(NPCHit4/41/42)、幽灵(NPCHit36/49/52/53/54)、史莱姆(NPCHit1以外的凝胶系,
        /// 见NPCHit2/5/11/30/34)与无受击音效者判为非有机,其余为有机。
        /// 灾厄对 Providence/ScornEater/Yharon 的三个白名单特例为其自有NPC,脱离灾厄后不适用,已裁剪;
        /// 原以 IL 钩令木桩(SuperDummy)视为有机的语义,内置为原版训练假人(TargetDummy)特判。
        /// </summary>
        public static bool Organic(this NPC target)
        {
            //原语义保留:木桩视为有机(替代已删的 SuperDummy IL 钩,落到原版训练假人)
            if (target.type == NPCID.TargetDummy)
                return true;
            return target.HitSound != SoundID.NPCHit4 && target.HitSound != SoundID.NPCHit41 && target.HitSound != SoundID.NPCHit2 &&
                   target.HitSound != SoundID.NPCHit5 && target.HitSound != SoundID.NPCHit11 && target.HitSound != SoundID.NPCHit30 &&
                   target.HitSound != SoundID.NPCHit34 && target.HitSound != SoundID.NPCHit36 && target.HitSound != SoundID.NPCHit42 &&
                   target.HitSound != SoundID.NPCHit49 && target.HitSound != SoundID.NPCHit52 && target.HitSound != SoundID.NPCHit53 &&
                   target.HitSound != SoundID.NPCHit54 && target.HitSound != null;
        }
        // —— 编译门补批:以下为灾厄扩展同名移植(签名与灾厄一致),供全仓既有调用点直接接轨 ——

        /// <summary>实体指向目标的安全单位向量(移植自灾厄 SafeDirectionTo)。</summary>
        public static Vector2 SafeDirectionTo(this Entity entity, Vector2 destination, Vector2? fallback = null)
        {
            return (destination - entity.Center).SafeNormalize(fallback ?? Vector2.Zero);
        }

        /// <summary>下标越界判定(移植自灾厄 WithinBounds)。</summary>
        public static bool WithinBounds(this int index, int cap) => index >= 0 && index < cap;

        /// <summary>条件成立才加入列表(移植自灾厄 AddWithCondition)。</summary>
        public static void AddWithCondition<T>(this List<T> list, T type, bool condition)
        {
            if (condition)
                list.Add(type);
        }

        /// <summary>物块是否为实心地面(移植自灾厄 IsTileSolid,不含平台)。</summary>
        public static bool IsTileSolid(this Tile tile) => tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];

        /// <summary>NPC 是否算作 Boss(移植自灾厄 IsABoss;灾厄史莱姆之神分裂体特例随灾厄裁剪)。</summary>
        public static bool IsABoss(this NPC npc)
        {
            if (npc is null || !npc.active)
                return false;
            if (npc.boss && npc.type != NPCID.MartianSaucerCore)
                return true;
            return npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsTail;
        }

        /// <summary>找出指定范围内最近的可追踪 NPC(移植自灾厄 ClosestNPCAt)。</summary>
        public static NPC ClosestNPCAt(this Vector2 origin, float maxDistanceToCheck, bool ignoreTiles = true, bool bossPriority = false)
        {
            NPC closestTarget = null;
            float distance = maxDistanceToCheck;
            if (bossPriority)
            {
                bool bossFound = false;
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    if (bossFound && !(Main.npc[index].boss || Main.npc[index].type == NPCID.WallofFleshEye))
                        continue;

                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);

                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);

                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance && canHit)
                        {
                            if (Main.npc[index].boss || Main.npc[index].type == NPCID.WallofFleshEye)
                                bossFound = true;

                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            else
            {
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);

                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);

                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance && canHit)
                        {
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            return closestTarget;
        }

        /// <summary>保持中心不变地重设弹幕碰撞箱(移植自灾厄 ExpandHitboxBy 四个重载)。</summary>
        public static void ExpandHitboxBy(this Projectile projectile, int width, int height)
        {
            projectile.position = projectile.Center;
            projectile.width = width;
            projectile.height = height;
            projectile.position -= projectile.Size * 0.5f;
        }
        public static void ExpandHitboxBy(this Projectile projectile, int newSize) => projectile.ExpandHitboxBy(newSize, newSize);
        public static void ExpandHitboxBy(this Projectile projectile, Vector2 newSize) => projectile.ExpandHitboxBy((int)newSize.X, (int)newSize.Y);
        public static void ExpandHitboxBy(this Projectile projectile, float expandRatio) => projectile.ExpandHitboxBy((int)(projectile.width * expandRatio), (int)(projectile.height * expandRatio));

        /// <summary>弹幕是否处于本 tick 最后一次额外更新(移植自灾厄 FinalExtraUpdate)。</summary>
        public static bool FinalExtraUpdate(this Projectile proj) => proj.numUpdates == -1;

        /// <summary>0~1 进度转手臂伸展档位(移植自灾厄 ToStretchAmount)。</summary>
        public static Player.CompositeArmStretchAmount ToStretchAmount(this float percent)
        {
            if (percent < 0.25f)
                return Player.CompositeArmStretchAmount.None;
            if (percent < 0.5f)
                return Player.CompositeArmStretchAmount.Quarter;
            if (percent < 0.75f)
                return Player.CompositeArmStretchAmount.ThreeQuarters;

            return Player.CompositeArmStretchAmount.Full;
        }

        /// <summary>前手位置(移植自灾厄 GetFrontHandPositionImproved,自动处理重力翻转)。</summary>
        public static Vector2 GetFrontHandPositionImproved(this Player player, Player.CompositeArmData arm)
        {
            Vector2 position = player.GetFrontHandPosition(arm.stretch, arm.rotation * player.gravDir).Floor();

            if (player.gravDir == -1f)
            {
                position.Y = player.position.Y + (float)player.height + (player.position.Y - position.Y);
            }

            return position;
        }

        /// <summary>后手位置(移植自灾厄 GetBackHandPositionImproved,自动处理重力翻转)。</summary>
        public static Vector2 GetBackHandPositionImproved(this Player player, Player.CompositeArmData arm)
        {
            Vector2 position = player.GetBackHandPosition(arm.stretch, arm.rotation * player.gravDir).Floor();

            if (player.gravDir == -1f)
            {
                position.Y = player.position.Y + (float)player.height + (player.position.Y - position.Y);
            }

            return position;
        }

        /// <summary>
        /// 玩家当前最强职业(移植自灾厄 GetBestClass)。
        /// 按 player-api DamageClass 收敛裁定:检查原版近战/远程/魔法/召唤(召唤按灾厄同款 0.75 折算),盗贼类已随退役剔除。
        /// </summary>
        public static DamageClass GetBestClass(this Player player)
        {
            float bestDamage = 1f;
            DamageClass bestClass = DamageClass.Generic;

            float melee = player.GetTotalDamage(DamageClass.Melee).Additive;
            if (melee > bestDamage)
            {
                bestDamage = melee;
                bestClass = DamageClass.Melee;
            }
            float ranged = player.GetTotalDamage(DamageClass.Ranged).Additive;
            if (ranged > bestDamage)
            {
                bestDamage = ranged;
                bestClass = DamageClass.Ranged;
            }
            float magic = player.GetTotalDamage(DamageClass.Magic).Additive;
            if (magic > bestDamage)
            {
                bestDamage = magic;
                bestClass = DamageClass.Magic;
            }
            //召唤全职业折算系数与灾厄一致(0.75):召唤无暴击,裸伤害普遍偏高
            float summon = player.GetTotalDamage(DamageClass.Summon).Additive * 0.75f;
            if (summon > bestDamage)
            {
                bestDamage = summon;
                bestClass = DamageClass.Summon;
            }
            return bestClass;
        }

        /// <summary>
        /// 玩家最强职业伤害修正(移植自灾厄 GetBestClassDamage)。
        /// 无类型加成沿用 Generic;比较范围按收敛裁定为原版四职业(召唤 0.75 折算),盗贼项剔除。
        /// </summary>
        public static StatModifier GetBestClassDamage(this Player player)
        {
            StatModifier ret = StatModifier.Default;
            StatModifier classless = player.GetTotalDamage(DamageClass.Generic);

            ret.Base = classless.Base;
            ret *= classless.Multiplicative;
            ret.Flat = classless.Flat;

            float best = 1f;

            float melee = player.GetTotalDamage(DamageClass.Melee).Additive;
            if (melee > best) best = melee;
            float ranged = player.GetTotalDamage(DamageClass.Ranged).Additive;
            if (ranged > best) best = ranged;
            float magic = player.GetTotalDamage(DamageClass.Magic).Additive;
            if (magic > best) best = magic;
            float summon = player.GetTotalDamage(DamageClass.Summon).Additive * 0.75f;
            if (summon > best) best = summon;

            ret += best - 1f;
            return ret;
        }

        /// <summary>键位的提示文本(移植自灾厄 TooltipHotkeyString;未绑定时取文案键 Misc.HotkeyNotBound)。</summary>
        public static string TooltipHotkeyString(this ModKeybind mhk)
        {
            if (Main.dedServ || mhk is null)
                return "";

            //灾厄GetAssignedKeysOrEmpty的原生等价:GetAssignedKeys无绑定时本就返回空表
            List<string> keys = mhk.GetAssignedKeys();
            if (keys.Count == 0)
                return GetText("Misc.HotkeyNotBound").Value;
            return string.Join(" / ", keys);
        }

        /// <summary>把 Tooltip 中首个 [KEY] 占位符替换为实际键名(移植自灾厄 IntegrateHotkey/FindAndReplace)。</summary>
        public static void IntegrateHotkey(this List<TooltipLine> tooltips, ModKeybind mhk)
        {
            if (Main.dedServ || mhk is null)
                return;

            string finalKey = mhk.TooltipHotkeyString();
            TooltipLine line = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Text.Contains("[KEY]"));
            if (line != null)
                line.Text = line.Text.Replace("[KEY]", finalKey);
        }

        /// <summary>立即以指定混合模式重开画批(移植自灾厄 SetBlendState)。</summary>
        public static void SetBlendState(this SpriteBatch spriteBatch, BlendState blendState)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, blendState, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 手持物品姿态整理(移植自灾厄CleanHoldStyle,自写等效)。
        /// </summary>
        public static void CleanHoldStyle(Player player, float desiredRotation, Vector2 desiredPosition, Vector2 spriteSize, Vector2? rotationOriginFromCenter = null, bool noSandstorm = false, bool flipAngle = false, bool stepDisplace = true)
        {
            if (noSandstorm)
                player.sandStorm = false;

            if (rotationOriginFromCenter == null)
                rotationOriginFromCenter = Vector2.Zero;

            Vector2 origin = rotationOriginFromCenter.Value;
            origin.X *= player.direction;
            origin.Y *= player.gravDir;

            player.itemRotation = desiredRotation;

            if (flipAngle)
                player.itemRotation *= player.direction;
            else if (player.direction < 0)
                player.itemRotation += MathHelper.Pi;

            //锚定到贴图中心旋转,再按自定义原点偏移
            Vector2 consistentCenterAnchor = player.itemRotation.ToRotationVector2() * (spriteSize.X / -2f - 10f) * player.direction;
            Vector2 consistentAnchor = consistentCenterAnchor - origin.RotatedBy(player.itemRotation);
            Vector2 offsetAgain = spriteSize * -0.5f;
            Vector2 finalPosition = desiredPosition + offsetAgain + consistentAnchor;

            //走路动画抬高帧补偿
            if (stepDisplace)
            {
                int frame = player.bodyFrame.Y / player.bodyFrame.Height;
                if ((frame > 6 && frame < 10) || (frame > 13 && frame < 17))
                {
                    finalPosition -= Vector2.UnitY * 2f;
                }
            }

            player.itemLocation = finalPosition + new Vector2(spriteSize.X * 0.5f, 0);
        }

        /// <summary>
        /// 残影绘制(移植自灾厄DrawAfterimagesCentered)。mode: 0标准/1帕拉丁锤式/2带旋转。
        /// </summary>
        public static void DrawAfterimagesCentered(Projectile proj, int mode, Color lightColor, int typeOneIncrement = 1, Texture2D texture = null, bool drawCentered = true, bool shrink = false, int armorShaderToUse = 0)
        {
            if (texture is null)
                texture = TextureAssets.Projectile[proj.type].Value;

            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            float scale = proj.scale;
            float rotation = proj.rotation;

            Rectangle rectangle = new Rectangle(0, frameY, texture.Width, frameHeight);
            Vector2 origin = rectangle.Size() / 2f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            bool failedToDrawAfterimages = false;
            Vector2 centerOffset = drawCentered ? proj.Size / 2f : Vector2.Zero;
            Color alphaColor = proj.GetAlpha(lightColor);
            switch (mode)
            {
                case 0:
                    for (int i = 0; i < proj.oldPos.Length; ++i)
                    {
                        Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                        float interpolant = ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                        Color color = alphaColor * interpolant;
                        var drawData = new Terraria.DataStructures.DrawData(texture, drawPos, rectangle, color)
                        {
                            rotation = rotation,
                            origin = origin,
                            effect = spriteEffects
                        };
                        GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                        Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, origin, shrink ? scale * interpolant : scale, spriteEffects, 0f);
                    }
                    break;

                case 1:
                    int increment = Math.Max(1, typeOneIncrement);
                    Color drawColor = alphaColor;
                    int afterimageCount = ProjectileID.Sets.TrailCacheLength[proj.type];
                    float afterimageColorCount = (float)afterimageCount * 1.5f;
                    int k = 0;
                    while (k < afterimageCount)
                    {
                        Vector2 drawPos = proj.oldPos[k] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                        float interpolant = ((float)(proj.oldPos.Length - k) / (float)proj.oldPos.Length);
                        if (k > 0)
                        {
                            float colorMult = (float)(afterimageCount - k);
                            drawColor *= colorMult / afterimageColorCount;
                        }
                        var drawData = new Terraria.DataStructures.DrawData(texture, drawPos, rectangle, drawColor)
                        {
                            rotation = rotation,
                            origin = origin,
                            effect = spriteEffects
                        };
                        GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                        Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), drawColor, rotation, origin, shrink ? scale * interpolant : scale, spriteEffects, 0f);
                        k += increment;
                    }
                    break;

                case 2:
                    for (int i = 0; i < proj.oldPos.Length; ++i)
                    {
                        float afterimageRot = proj.oldRot[i];
                        SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                        Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                        float interpolant = ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                        Color color = alphaColor * interpolant;
                        var drawData = new Terraria.DataStructures.DrawData(texture, drawPos, rectangle, color)
                        {
                            rotation = rotation,
                            origin = origin,
                            effect = spriteEffects
                        };
                        GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                        Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, origin, shrink ? scale * interpolant : scale, sfxForThisAfterimage, 0f);
                    }
                    break;

                default:
                    failedToDrawAfterimages = true;
                    break;
            }

            //无残影缓存或mode非法时,保底画本体
            if (ProjectileID.Sets.TrailCacheLength[proj.type] <= 0 || failedToDrawAfterimages)
            {
                Vector2 startPos = drawCentered ? proj.Center : proj.position;
                Vector2 drawPos2 = startPos - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                var drawData2 = new Terraria.DataStructures.DrawData(texture, drawPos2, rectangle, proj.GetAlpha(lightColor));
                GameShaders.Armor.Apply(armorShaderToUse, proj, drawData2);
                Main.spriteBatch.Draw(texture, drawPos2, rectangle, proj.GetAlpha(lightColor), rotation, origin, scale, spriteEffects, 0f);
            }
        }

        /// <summary>
        /// 边缘锚定残影(移植自灾厄DrawAfterimagesFromEdge,子弹类用)。仅支持mode 0/2。
        /// </summary>
        public static void DrawAfterimagesFromEdge(Projectile proj, int mode, Color lightColor, Texture2D texture = null)
        {
            if (texture is null)
                texture = TextureAssets.Projectile[proj.type].Value;

            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            float scale = proj.scale;
            float rotation = proj.rotation;

            Rectangle rectangle = new Rectangle(0, frameY, texture.Width, frameHeight);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, proj.height * 0.5f);

            switch (mode)
            {
                default:
                    return;
                case 0:
                    for (int i = 0; i < proj.oldPos.Length; ++i)
                    {
                        Vector2 drawPos = proj.oldPos[i] + drawOrigin - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                        Color color = proj.GetAlpha(lightColor) * ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                        Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, drawOrigin, scale, spriteEffects, 0f);
                    }
                    return;
                case 2:
                    for (int i = 0; i < proj.oldPos.Length; ++i)
                    {
                        float afterimageRot = proj.oldRot[i];
                        SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                        Vector2 drawPos = proj.oldPos[i] + drawOrigin - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                        Color color = proj.GetAlpha(lightColor) * ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                        Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, drawOrigin, scale, sfxForThisAfterimage, 0f);
                    }
                    return;
            }
        }

        /// <summary>
        /// 多色插值(移植自灾厄MulticolorLerp)。
        /// </summary>
        public static Color MulticolorLerp(float increment, params Color[] colors)
        {
            increment %= 0.999f;
            int currentColorIndex = (int)(increment * colors.Length);
            Color currentColor = colors[currentColorIndex];
            Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
            return Color.Lerp(currentColor, nextColor, increment * colors.Length % 1f);
        }

        /// <summary>
        /// 越界安全取物块(移植自灾厄ParanoidTileRetrieval)。
        /// </summary>
        public static Tile ParanoidTileRetrieval(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
                return new Tile();
            return Main.tile[x, y];
        }

        private const float WorldInsertionOffset = 15f;
        /// <summary>
        /// 物品出界时拉回世界边界内(移植自灾厄ForceItemIntoWorld)。
        /// </summary>
        public static bool ForceItemIntoWorld(Item item, float desiredDist = WorldInsertionOffset)
        {
            if (item is null || !item.active)
                return false;

            float worldEdge = Main.offLimitBorderTiles * 16f;
            float dist = worldEdge + desiredDist;

            float maxPosX = Main.maxTilesX * 16f;
            float maxPosY = Main.maxTilesY * 16f;
            bool moved = false;
            if (item.position.X < worldEdge)
            {
                item.position.X = dist;
                moved = true;
            }
            else if (item.position.X + item.width > maxPosX - worldEdge)
            {
                item.position.X = maxPosX - item.width - dist;
                moved = true;
            }
            if (item.position.Y < worldEdge)
            {
                item.position.Y = dist;
                moved = true;
            }
            else if (item.position.Y + item.height > maxPosY - worldEdge)
            {
                item.position.Y = maxPosY - item.height - dist;
                moved = true;
            }
            return moved;
        }

        /// <summary>
        /// 宝袋世界内脉冲绘制(移植自灾厄DrawTreasureBagInWorld)。
        /// </summary>
        public static bool DrawTreasureBagInWorld(Item item, SpriteBatch spriteBatch, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = TextureAssets.Item[item.type].Value;
            Rectangle frame = texture.Frame();

            if (Main.itemAnimations[item.type] != null)
                frame = Main.itemAnimations[item.type].GetFrame(texture, Main.itemFrameCounter[whoAmI]);

            Vector2 frameOrigin = frame.Size() * 0.5f;
            Vector2 offset = new Vector2(item.width / 2 - frameOrigin.X, item.height - frame.Height);
            Vector2 drawPos = item.position - Main.screenPosition + frameOrigin + offset;

            float localTime = item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;

            //全局时间转0-1三角波
            float time = Main.GlobalTimeWrappedHourly % 4f / 2f;
            if (time >= 1f)
                time = 2f - time;
            time = time * 0.5f + 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 pulseOffset = Vector2.UnitY.RotatedBy((i / 4f + localTime) * MathHelper.TwoPi) * time * 8f;
                spriteBatch.Draw(texture, drawPos + pulseOffset, frame, new Color(90, 70, 255, 50), rotation, frameOrigin, scale, 0, 0);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector2 pulseOffset = Vector2.UnitY.RotatedBy((i / 3f + localTime) * MathHelper.TwoPi) * time * 4f;
                spriteBatch.Draw(texture, drawPos + pulseOffset, frame, new Color(140, 120, 255, 77), rotation, frameOrigin, scale, 0, 0);
            }

            return true;
        }

        public static int SecondsToFrames(int seconds) => seconds * 60;
        public static int SecondsToFrames(float seconds) => (int)MathF.Round(seconds * 60);

        /// <summary>
        /// 0-1输入映射为0-1-0正弦(移植自灾厄Convert01To010)。
        /// </summary>
        public static float Convert01To010(float value) => (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(value, 0f, 1f));

        /// <summary>
        /// 库存物品自定义缩放绘制(移植自灾厄DrawInventoryCustomScale)。
        /// </summary>
        public static void DrawInventoryCustomScale(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale, float wantedScale = 1f, Vector2 drawOffset = default, SpriteEffects spriteEffects = SpriteEffects.None, float rotation = 0f)
        {
            wantedScale = Math.Max(scale, wantedScale * Main.inventoryScale);
            position += drawOffset * wantedScale;
            if (itemColor == Color.Transparent) itemColor = Color.White;
            spriteBatch.Draw(texture, position, frame, itemColor.MultiplyRGB(drawColor), 0f, origin, wantedScale, SpriteEffects.None, 0);
        }

        /// <summary>
        /// 库存物品右下角启停圆点(移植自灾厄DrawInventoryDot,用原版Extra_20贴图)。
        /// </summary>
        public static void DrawInventoryDot(SpriteBatch spriteBatch, Vector2 itemPosition, Vector2 dotOffset, bool enabled)
        {
            var tex = RequestTex("Terraria/Images/Extra_20");
            var dotFrame = tex.Frame(1, 4, frameY: enabled ? 1 : 2);
            spriteBatch.Draw(tex, itemPosition + dotOffset, dotFrame, Color.White, 0, dotFrame.Size() * 0.5f, Main.inventoryScale, SpriteEffects.None, 0);
        }

        /// <summary>
        /// 八向描边文本(移植自灾厄DrawBorderStringEightWay)。
        /// </summary>
        public static void DrawBorderStringEightWay(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 baseDrawPosition, Color main, Color border, float scale = 1f)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;
                    Vector2 drawPosition = baseDrawPosition + new Vector2(x, y);
                    sb.DrawString(font, text, drawPosition, border, 0f, default, scale, SpriteEffects.None, 0f);
                }
            }
            sb.DrawString(font, text, baseDrawPosition, main, 0f, default, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 圆形碰撞箱与矩形求交(移植自灾厄CircularHitboxCollision)。
        /// </summary>
        public static bool CircularHitboxCollision(Vector2 centerCheckPosition, float radius, Rectangle targetHitbox)
        {
            if (radius <= 0f)
                return false;

            float closestX = MathHelper.Clamp(centerCheckPosition.X, targetHitbox.Left, targetHitbox.Right);
            float closestY = MathHelper.Clamp(centerCheckPosition.Y, targetHitbox.Top, targetHitbox.Bottom);

            float dx = centerCheckPosition.X - closestX;
            float dy = centerCheckPosition.Y - closestY;

            return (dx * dx + dy * dy) <= (radius * radius);
        }

        /// <summary>
        /// 追踪最近敌怪(移植自灾厄HomeInOnNPC)。锁定时+1额外更新,失锁还原(暂存于EGlobalProjectile.StoredEU)。
        /// </summary>
        public static void HomeInOnNPC(Projectile projectile, bool ignoreTiles, float distanceRequired, float homingVelocity, float inertia, bool respectIFrames = false)
        {
            if (!projectile.friendly)
                return;

            var gp = projectile.Entropy();
            if (gp.StoredEU == -1)
                gp.StoredEU = projectile.extraUpdates;

            Vector2 destination = projectile.Center;
            float maxDistance = distanceRequired;
            bool locatedTarget = false;

            float npcDistCompare = 25000f;
            int index = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                float extraDistance = (n.width / 2) + (n.height / 2);
                if (!n.CanBeChasedBy(projectile, false) || !projectile.WithinRange(n.Center, maxDistance + extraDistance) || (respectIFrames && (projectile.localNPCImmunity[n.whoAmI] > 0 || projectile.localNPCImmunity[n.whoAmI] == -1 || n.immune[projectile.owner] > 0)))
                    continue;

                float currentNPCDist = Vector2.Distance(n.Center, projectile.Center);
                //带iframe的目标降权但不排除
                if (respectIFrames && Projectile.perIDStaticNPCImmunity[projectile.type][n.whoAmI] > Main.GameUpdateCount)
                    currentNPCDist += 1600;
                if ((currentNPCDist < npcDistCompare) && (ignoreTiles || Collision.CanHit(projectile.Center, 1, 1, n.Center, 1, 1)))
                {
                    npcDistCompare = currentNPCDist;
                    index = n.whoAmI;
                }
            }
            if (index != -1)
            {
                destination = Main.npc[index].Center;
                locatedTarget = true;
            }

            if (locatedTarget)
            {
                projectile.extraUpdates = gp.StoredEU + 1;
                Vector2 homeDirection = (destination - projectile.Center).SafeNormalize(Vector2.UnitY);
                projectile.velocity = (projectile.velocity * inertia + homeDirection * homingVelocity) / (inertia + 1f);
            }
            else
            {
                projectile.extraUpdates = gp.StoredEU;
            }
        }

        /// <summary>
        /// 欧拉法预判弹道(移植自灾厄CalculatePredictiveAimToTargetMaxUpdates)。
        /// </summary>
        public static Vector2 CalculatePredictiveAimToTargetMaxUpdates(Vector2 startingPosition, Vector2 targetPosition, Vector2 targetVelocity, float shootSpeed, int projMaxUpdates, int iterations = 4)
        {
            float previousTimeToReachDestination = 0f;
            Vector2 currentTargetPosition = targetPosition;
            for (int i = 0; i < iterations; i++)
            {
                float timeToReachDestination = Vector2.Distance(startingPosition, currentTargetPosition) / shootSpeed / projMaxUpdates;
                currentTargetPosition += targetVelocity * (timeToReachDestination - previousTimeToReachDestination);
                previousTimeToReachDestination = timeToReachDestination;
            }
            return (currentTargetPosition - startingPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
        }
        public static Vector2 CalculatePredictiveAimToTargetMaxUpdates(Vector2 startingPosition, Entity target, float shootSpeed, int projMaxUpdates, int iterations = 4)
        {
            return CalculatePredictiveAimToTargetMaxUpdates(startingPosition, target.Center, target.velocity, shootSpeed, projMaxUpdates, iterations);
        }

        /// <summary>
        /// 随机方向随机速度(移植自灾厄RandomVelocity)。
        /// </summary>
        public static Vector2 RandomVelocity(float directionMult, float speedLowerLimit, float speedCap, float speedMult = 0.1f)
        {
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
            while (velocity.X == 0f && velocity.Y == 0f)
            {
                velocity = new Vector2(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
            }
            velocity.Normalize();
            velocity *= Main.rand.NextFloat(speedLowerLimit, speedCap) * speedMult;
            return velocity;
        }

        /// <summary>
        /// 木箭类弹药判定(移植自灾厄CheckWoodenAmmo)。
        /// </summary>
        public static bool CheckWoodenAmmo(int type, Player player)
        {
            if (player.hasMoltenQuiver && type == ProjectileID.FireArrow)
                return true;
            return type == ProjectileID.WoodenArrowFriendly;
        }

        /// <summary>
        /// NPC平滑移动(移植自灾厄SmoothMovement)。
        /// </summary>
        public static void SmoothMovement(NPC npc, float movementDistanceGateValue, Vector2 distanceFromDestination, float baseVelocity, float acceleration, bool useSimpleFlyMovement)
        {
            float lerpValue = Utils.GetLerpValue(movementDistanceGateValue, 2400f, distanceFromDestination.Length(), true);

            float minVelocity = distanceFromDestination.Length();
            float minVelocityCap = baseVelocity;
            if (minVelocity > minVelocityCap)
                minVelocity = minVelocityCap;

            Vector2 maxVelocity = distanceFromDestination / 24f;
            float maxVelocityCap = minVelocityCap * 3f;
            if (maxVelocity.Length() > maxVelocityCap)
                maxVelocity = distanceFromDestination.SafeNormalize(Vector2.Zero) * maxVelocityCap;

            Vector2 desiredVelocity = Vector2.Lerp(distanceFromDestination.SafeNormalize(Vector2.Zero) * minVelocity, maxVelocity, lerpValue);
            if (useSimpleFlyMovement)
                npc.SimpleFlyMovement(desiredVelocity, acceleration);
            else
                npc.velocity = desiredVelocity;
        }

        public static void SetMerge(int type1, int type2, bool merge = true)
        {
            if (type1 != type2)
            {
                Main.tileMerge[type1][type2] = merge;
                Main.tileMerge[type2][type1] = merge;
            }
        }
        public static void MergeWithSet(int myType, params int[] otherTypes)
        {
            for (int i = 0; i < otherTypes.Length; ++i)
                SetMerge(myType, otherTypes[i]);
        }
        /// <summary>
        /// 与常见世代物块合并(移植自灾厄MergeWithGeneral,灾厄星幻/硫海物块条目已去除)。
        /// </summary>
        public static void MergeWithGeneral(int type) => MergeWithSet(type, new int[] {
            TileID.Dirt,
            TileID.Mud,
            TileID.ClayBlock,
            TileID.Stone,
            TileID.Ebonstone,
            TileID.Crimstone,
            TileID.Pearlstone,
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Pearlsand,
            TileID.SnowBlock,
        });

        /// <summary>
        /// 取本模组本地化文本(移植自灾厄GetText,前缀Mods.CalamityEntropy.)。
        /// </summary>
        public static LocalizedText GetText(string key)
        {
            return Language.GetOrRegister("Mods.CalamityEntropy." + key);
        }
        /// <summary>
        /// 取本模组本地化字符串(移植自灾厄GetTextValue,前缀Mods.CalamityEntropy.)。
        /// </summary>
        public static string GetTextValue(string key)
        {
            return Language.GetTextValue("Mods.CalamityEntropy." + key);
        }
        /// <summary>
        /// 取物品名本地化(移植自灾厄GetItemName)。
        /// </summary>
        public static LocalizedText GetItemName(int itemID)
        {
            if (itemID < ItemID.Count)
            {
                return Language.GetText("ItemName." + ItemID.Search.GetName(itemID));
            }
            return GetTextFromModItem(itemID, "DisplayName");
        }
        public static LocalizedText GetItemName<T>() where T : ModItem => GetTextFromModItem(ModContent.ItemType<T>(), "DisplayName");
        public static LocalizedText GetTextFromModItem(int itemID, string suffix)
        {
            var modItem = ItemLoader.GetItem(itemID);
            return modItem.GetLocalization(suffix);
        }
        public static LocalizedText GetTextFromModItem<T>(string suffix) where T : ModItem => GetTextFromModItem(ModContent.ItemType<T>(), suffix);
        public static string GetTextValueFromModItem(int itemID, string suffix) => GetTextFromModItem(itemID, suffix).ToString();
        public static string GetTextValueFromModItem<T>(string suffix) where T : ModItem => GetTextFromModItem(ModContent.ItemType<T>(), suffix).ToString();
        #endregion
    }
}