using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.ILEditing;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Projectiles.HBProj;
using CalamityEntropy.Content.Projectiles.SamsaraCasket;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using CalamityEntropy.Content.Projectiles.VoidEchoProj;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Common
{
    public enum SyncDataType
    {
        Int,
        String,
        Boolean,
        Float,
        Double,
        Vector2,
        Color,
    }
    public class SynchronousData
    {
        public SyncDataType syncDataType;
        public object Value;
        public string Name;
        public SynchronousData(SyncDataType type, string name, object value) {
            syncDataType = type;
            Name = name;
            Value = value;
        }
        public void Write(BinaryWriter writer) {
            switch (syncDataType) {
                case SyncDataType.Int: writer.Write((int)Value); break;
                case SyncDataType.String: writer.Write((string)Value); break;
                case SyncDataType.Boolean: writer.Write((bool)Value); break;
                case SyncDataType.Float: writer.Write((float)Value); break;
                case SyncDataType.Double: writer.Write((double)Value); break;
                case SyncDataType.Vector2: writer.WriteVector2((Vector2)Value); break;
                case SyncDataType.Color: writer.WriteRGB((Color)Value); break;
            }
        }
        public object Read(BinaryReader reader) {
            switch (syncDataType) {
                case SyncDataType.Int: return reader.ReadInt32();
                case SyncDataType.String: return reader.ReadString();
                case SyncDataType.Boolean: return reader.ReadBoolean();
                case SyncDataType.Float: return reader.ReadSingle();
                case SyncDataType.Double: return reader.ReadDouble();
                case SyncDataType.Vector2: return reader.ReadVector2();
                case SyncDataType.Color: return reader.ReadRGB();
            }
            return null;
        }
        public void ReadToValue(BinaryReader reader) {
            Value = Read(reader);
        }
        public T GetValue<T>() {
            return ((T)Value);
        }

    }
    public class EGlobalProjectile : GlobalProjectile
    {
        //注意:这些字段只在客户端绘制钩子里读,专用服务器上恒为 null。
        [VaultLoaden("CalamityEntropy/Assets/Extra/ArchSmear")]
        internal static Asset<Texture2D> ArchSmearTex;
        [VaultLoaden("CalamityEntropy/Assets/Particles/BasicTrail")]
        internal static Asset<Texture2D> BasicTrailAsset;
        [VaultLoaden("CalamityEntropy/Assets/Extra/WyrmArrow")]
        internal static Asset<Texture2D> WyrmArrowTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/godhead")]
        internal static Asset<Texture2D> GodheadTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/LightningArrow")]
        internal static Asset<Texture2D> LightningArrowTex;
        public List<int> applyBuffs = new();
        public event Action<Projectile> OnKillActions;
        public bool Lightning = false;
        public List<Vector2> odp = new List<Vector2>();
        public List<Vector2> odp2 = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float counter = 0;
        public int AtlasItemType = 0;
        public int AtlasItemStack = 0;
        public bool DI = false;
        public bool gh = false;
        public int ghcounter = 0;
        public int IndexOfTwistedTwinShootedThisProj = -1;
        public Color CacheColor = Color.White;
        public float MissileHoming = 0;
        public float ShootCount = 2;
        public int OnProj { get { return IndexOfTwistedTwinShootedThisProj; } set { IndexOfTwistedTwinShootedThisProj = value; } }
        public int flagTT = 0;
        public Vector2 playerPosL;
        public Vector2 playerMPosL;
        public bool daTarget = false;
        public int maxDmgUps = 0;
        public float dmgUp = 0.05f;
        public bool GWBow = false;
        public int dmgupcount = 10;
        public bool ashesArrow = false;
        public bool buriedShoot = false;
        public int vddirection = 1;
        public bool ToFriendly = false;
        public bool BarrenHoming = false;
        public bool EventideShot = false;
        public bool ProminenceArrow = false;
        public float promineceDamageAddition = 0.25f;
        public int hittingTarget = -1;
        public bool IlmeranEnhanced = false;
        public bool LuminarArrow = false;
        public bool flameTrail = false;
        /// <summary>
        /// 初次生成
        /// </summary>
        public bool FirstFrames = true;
        /// <summary>
        /// 用于临时存储的额外更新
        /// </summary>
        public int StoredEU = -1;
        //全局表示是否处于右键状态
        public bool IsRightClick = false;

        public Dictionary<string, SynchronousData> DataSynchronous = new Dictionary<string, SynchronousData>();
        public void DefineSynchronousData(SyncDataType type, string name, object defaultValue) {
            DataSynchronous[name] = new SynchronousData(type, name, defaultValue);
        }
        public T GetSyncValue<T>(string name) {
            return this.DataSynchronous[name].GetValue<T>();
        }
        public void SetSyncValue(string name, object value) {
            this.DataSynchronous[name].Value = value;
        }
        public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox) {
            if (hittingTarget >= 0) {
                if (CEUtils.getDistance(targetHitbox.Center.ToVector2(), hittingTarget.ToNPC().Hitbox.Center.ToVector2()) < 32) {
                    return true;
                }
            }
            return base.Colliding(projectile, projHitbox, targetHitbox);
        }

        public override GlobalProjectile Clone(Projectile from, Projectile to) {
            var p = to.Entropy();
            p.EventideShot = EventideShot;
            p.DI = DI;
            p.gh = gh;
            p.IndexOfTwistedTwinShootedThisProj = IndexOfTwistedTwinShootedThisProj;
            p.flagTT = flagTT;
            p.daTarget = daTarget;
            p.maxDmgUps = maxDmgUps;
            p.dmgUp = dmgUp;
            p.GWBow = GWBow;
            p.dmgupcount = dmgupcount;
            p.counter = counter;
            p.withGrav = withGrav;
            p.ToFriendly = ToFriendly;
            p.ProminenceArrow = ProminenceArrow;
            p.DataSynchronous = DataSynchronous;
            return p;
        }
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
            return true;
        }
        public override void SendExtraAI(Projectile projectile, BitWriter _, BinaryWriter binaryWriter) {
            binaryWriter.Write(Shooter);
            binaryWriter.Write(OnProj);
            binaryWriter.Write(IndexOfTwistedTwinShootedThisProj);
            binaryWriter.Write(GWBow);
            binaryWriter.Write(withGrav);
            binaryWriter.Write(vdtype);
            binaryWriter.Write(vddirection);
            binaryWriter.Write(rpBow);
            binaryWriter.Write(ToFriendly);
            binaryWriter.Write(EventideShot);
            binaryWriter.Write(zypArrow);
            binaryWriter.Write(ProminenceArrow);
            binaryWriter.Write(IlmeranEnhanced);
            binaryWriter.Write(LuminarArrow);
            binaryWriter.Write(SmartArcEffect);
            binaryWriter.Write(FirstFrames);
            binaryWriter.Write(ashesArrow);
            binaryWriter.Write(buriedShoot);
            binaryWriter.Write(flameTrail);
            foreach (var key in DataSynchronous.Keys) {
                DataSynchronous[key].Write(binaryWriter);
            }
        }
        public override void ReceiveExtraAI(Projectile projectile, BitReader _, BinaryReader binaryReader) {
            Shooter = binaryReader.ReadInt32();
            OnProj = binaryReader.ReadInt32();
            IndexOfTwistedTwinShootedThisProj = binaryReader.ReadInt32();
            GWBow = binaryReader.ReadBoolean();
            withGrav = binaryReader.ReadBoolean();
            vdtype = binaryReader.ReadInt32();
            vddirection = binaryReader.ReadInt32();
            rpBow = binaryReader.ReadBoolean();
            ToFriendly = binaryReader.ReadBoolean();
            EventideShot = binaryReader.ReadBoolean();
            zypArrow = binaryReader.ReadBoolean();
            ProminenceArrow = binaryReader.ReadBoolean();
            IlmeranEnhanced = binaryReader.ReadBoolean();
            LuminarArrow = binaryReader.ReadBoolean();
            SmartArcEffect = binaryReader.ReadBoolean();
            FirstFrames = binaryReader.ReadBoolean();
            ashesArrow = binaryReader.ReadBoolean();
            buriedShoot = binaryReader.ReadBoolean();
            flameTrail = binaryReader.ReadBoolean();
            foreach (var key in DataSynchronous.Keys) {
                DataSynchronous[key].ReadToValue(binaryReader);
            }
        }
        public override bool InstancePerEntity => true;
        public override void SetDefaults(Projectile entity) {
            if (entity.ModProjectile != null && entity.ModProjectile.Mod is CalamityEntropy) {
                if (entity.minion)
                    entity.netImportant = true;
            }
        }
        public bool netsnc = true;
        public static bool checkHoldOut = true;
        public bool dmgUpFrd = true;
        public int Lifetime = 0;
        public int updateTimes = 0;
        public override void AI(Projectile projectile) {
            updateTimes++;
            if (updateTimes % projectile.MaxUpdates == 0)
                Lifetime++;
            if (buriedShoot) {
                PRTLoader.NewParticle<PRT_CustomSpark>(projectile.Center + projectile.velocity.normalize() * 40, -projectile.velocity * 0.05f, Color.Black, 0.03f).Configure("CalamityEntropy/Assets/Particles/GlowSpark2", false, 9, new Vector2(0.6f, 1.3f), false, false, 0f, false, false);
                //绿火花AfterPlayers:弹幕前后层PRT没有,硬提RenderLayer盖蓝光
                PRTLoader.NewParticle<PRT_CustomSpark>(projectile.Center + projectile.velocity.normalize() * 40, -projectile.velocity * 0.05f, Color.LightGreen, 0.012f).Configure("CalamityEntropy/Assets/Particles/GlowSpark", false, 7, new Vector2(0.6f, 1.9f), true, false, 0f, false, false, renderLayer: PRTRenderLayer.AfterPlayers);
            }
        }
        public bool Losted = false;
        public int Shooter = -1;
        public override void OnSpawn(Projectile projectile, IEntitySource source) {
            if (Main.gameMenu) {
                return;
            }
            if (projectile.type == ProjectileID.Terragrim) {
                projectile.scale *= 2;
            }
            if (projectile.friendly) {
                if (projectile.owner.ToPlayer().Entropy().BarrenCard) {
                    // 灾厄盗贼伤害类型退役，荒瘠卡追踪改按原版投掷伤害判定
                    if (projectile.DamageType.CountsAsClass(DamageClass.Throwing)) {
                        BarrenHoming = true;
                    }
                }
            }
            if (source is EntitySource_Parent s) {
                if (s.Entity is Player player) {
                    projectile.velocity *= player.Entropy().shootSpeed;

                }
                if (s.Entity is NPC npc) {
                    if (!npc.friendly) {
                        Shooter = npc.whoAmI;
                        projectile.netSpam = 0;
                        projectile.netUpdate = true;
                    }
                    ToFriendly = npc.Entropy().ToFriendly;
                    if (CalamityEntropy.EntropyMode) {
                        if (npc.type == NPCID.Golem || npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight || npc.type == NPCID.GolemHead || npc.type == NPCID.GolemHeadFree) {
                            Losted = true;
                            projectile.netSpam = 0;
                            projectile.netUpdate = true;
                        }
                        if (npc.type == NPCID.CultistBoss || npc.type == NPCID.AncientLight || npc.type == NPCID.AncientDoom || EModILEdit.LostNPCsEntropy.Contains(npc.type)) {
                            Losted = true;
                            projectile.netSpam = 0;
                            projectile.netUpdate = true;
                        }

                    }
                }
                if (s.Entity is Projectile pj) {
                    Shooter = pj.Entropy().Shooter;
                    if (Shooter >= 0) {
                        projectile.netSpam = 0;
                        projectile.netUpdate = true;
                    }
                    ToFriendly = pj.Entropy().ToFriendly;
                    if (pj.Entropy().Losted) {
                        Losted = true;
                        projectile.netSpam = 0;
                        projectile.netUpdate = true;
                    }
                }
                if (ToFriendly) {
                    projectile.usesLocalNPCImmunity = true;
                    projectile.localNPCHitCooldown = 14;
                    projectile.friendly = true;
                    projectile.hostile = false;
                    if (Main.netMode != NetmodeID.SinglePlayer) {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectile.whoAmI);
                    }
                }
            }
            if (projectile.friendly) {
                // 2026-08-31 平衡案:虚寂猎影盔职业奖励重做,原弹幕伤害递增机制退役(改为远伤×1.15+50%弹速,在头盔文件里)
                if ((source is EntitySource_ItemUse && checkHoldOut && projectile.owner == Main.myPlayer && (projectile.type == ModContent.ProjectileType<VoidEchoProj>() || projectile.type == ModContent.ProjectileType<HB>() || projectile.type == ModContent.ProjectileType<GhostdomWhisperHoldout>() || projectile.type == ModContent.ProjectileType<RailPulseBowProjectile>() || projectile.type == ModContent.ProjectileType<SamsaraCasketProj>() || projectile.type == ModContent.ProjectileType<OblivionHoldout>() || projectile.type == ModContent.ProjectileType<HadopelagicEchoIIProj>()))) {
                    checkHoldOut = false;
                    foreach (Projectile p in Main.projectile) {
                        if (p.active && p.type == ModContent.ProjectileType<TwistedTwinMinion>() && p.owner == Main.myPlayer) {

                            int phd = Projectile.NewProjectile(Main.LocalPlayer.GetSource_ItemUse(Main.LocalPlayer.HeldItem), p.Center, Vector2.Zero, projectile.type, projectile.damage, projectile.knockBack, projectile.owner);
                            Projectile ph = phd.ToProj();
                            ph.scale *= 0.8f;
                            ph.Entropy().IndexOfTwistedTwinShootedThisProj = p.identity;
                            p.netUpdate = true;
                            ph.netUpdate = true;
                            Projectile projts = ph;
                            ph.damage = (int)(ph.damage * TwistedTwinMinion.damageMul);
                            if (!projts.usesLocalNPCImmunity) {
                                projts.usesLocalNPCImmunity = true;
                                projts.localNPCHitCooldown = 12;
                            }
                        }
                    }
                    checkHoldOut = true;
                }
                if (source is EntitySource_Parent ps) {
                    if (ps.Entity is Projectile pj) {

                        if (((Projectile)ps.Entity).Entropy().DI) {
                            projectile.friendly = ((Projectile)ps.Entity).friendly;
                            projectile.hostile = ((Projectile)ps.Entity).hostile;
                        }
                        if (pj.Entropy().IndexOfTwistedTwinShootedThisProj != -1) {
                            projectile.Entropy().IndexOfTwistedTwinShootedThisProj = pj.Entropy().IndexOfTwistedTwinShootedThisProj;
                            int type = projectile.type;


                        }
                        projectile.Entropy().flagTT = pj.Entropy().flagTT + 1;
                    }
                    if (ps.Entity is Player plr) {
                        if (plr.Entropy().twinSpawnIndex != -1) {
                            if (projectile.type != ModContent.ProjectileType<TwistedTwinMinion>()) {
                                projectile.scale *= 0.8f;
                                IndexOfTwistedTwinShootedThisProj = plr.Entropy().twinSpawnIndex;
                                projectile.netUpdate = true;

                                Projectile projts = projectile;
                                if (!projts.usesLocalNPCImmunity) {
                                    projts.usesLocalNPCImmunity = true;
                                    projts.localNPCHitCooldown = 12;
                                }
                            }
                        }

                        if (plr.HasBuff(ModContent.BuffType<SoyMilkBuff>())) {
                            if (!CELists.SoyMilkProjectileBlacklist.Contains(projectile.type) && !(projectile.ModProjectile is EntropyBookHeldProjectile))
                                projectile.MaxUpdates = projectile.MaxUpdates * 3;
                        }
                    }

                }

            }
        }
        public override bool ShouldUpdatePosition(Projectile projectile) {
            if (WisperArrow && Freeze)
                return false;
            if (typhoonBullet || OverrideBulletMoveAI) {
                return false;
            }
            if (projectile.Entropy().daTarget) {
                return false;
            }
            return base.ShouldUpdatePosition(projectile);
        }

        public override bool CanHitPlayer(Projectile projectile, Player target) {
            if (vdtype >= 0) {
                return false;
            }
            if (target.Entropy().immune > 0)
                return false;
            return base.CanHitPlayer(projectile, target);
        }
        public Vector2? plrOldPos = null;
        public Vector2? plrOldVel = null;
        public PRT_ProminenceTrail trail_pmn = null;   //ProminenceArrow,AI里AddPoint续命
        public bool init_ = true;
        public float maxSpd = -1;
        public List<int> luminarHited = new List<int>();
        public bool bulletInit = true;
        public bool typhoonBullet = false;
        public bool OverrideBulletMoveAI = false;
        public Vector2 typVel = Vector2.Zero;
        public bool SmartArcEffect = false;
        public bool Freeze = true;//For wisper arrows
        public PRT_HeavenfallStar2 ParticleOnMe = null;   //WisperArrow,AI里跟Center
        public bool slowFlag = true;
        public bool SetMaxUpdates = true;
        public override bool PreAI(Projectile projectile) {
            if (FirstFrames && projectile.ModProjectile != null && projectile.ModProjectile is BaitProj) {
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.ModProjectile != null && p.ModProjectile is BaitHeldEffect bh) {
                        bh.throwAnm = 1;
                    }
                }
            }
            if (SetMaxUpdates) {
                if (ashesArrow) {
                    SetMaxUpdates = false;
                    projectile.MaxUpdates *= 2;
                }
                if (Lightning) {
                    SetMaxUpdates = false;
                    projectile.MaxUpdates = 4;
                    projectile.velocity = projectile.velocity.normalize() * 16;
                }
            }
            if (Losted && slowFlag) {
                projectile.velocity *= 1.5f;
                slowFlag = false;
            }
            if (bulletInit) {
                bulletInit = false;
                {
                    if (projectile.GetOwner().HeldItem.type == ModContent.ItemType<Typhoon>() && projectile.GetOwner().PickAmmo(projectile.GetOwner().HeldItem, out var pts, out var s, out var d, out var kb, out var ua, true) && pts == projectile.type) {
                        typVel = projectile.velocity;
                        typhoonBullet = true;
                    }
                }
                {
                    if (projectile.GetOwner().HeldItem.type == ModContent.ItemType<SmartArc>() && projectile.GetOwner().PickAmmo(projectile.GetOwner().HeldItem, out var pts, out var s, out var d, out var kb, out var ua, true) && pts == projectile.type) {
                        OverrideBulletMoveAI = true;
                    }
                }
            }
            if (typhoonBullet) {
                float targetDist = Vector2.Distance(projectile.GetOwner().Center, projectile.Center);

                Vector3 DustLight = new Vector3(0.190f, 0.190f, 0.190f);
                Lighting.AddLight(projectile.Center, DustLight * 2);

                if (targetDist < 1400f) {
                    int positionVariation = 8;
                    PRTLoader.NewParticle<PRT_LineCal>(projectile.Center + Main.rand.NextVector2Circular(positionVariation, positionVariation), -projectile.velocity * Main.rand.NextFloat(0.003f, 0.001f), Color.Chocolate, 1.45f).Configure(false, 4);
                }
                projectile.position += projectile.velocity;
            }
            if (OverrideBulletMoveAI) {
                projectile.position += projectile.velocity;
            }
            if (ProjectileID.Sets.IsAGravestone[projectile.type] && projectile.GetOwner().head == EquipLoader.GetEquipSlot(Mod, "LuminarRing", EquipType.Head)) {
                if (Main.myPlayer == projectile.owner) {
                    Projectile.NewProjectile(projectile.GetOwner().GetSource_Death(), projectile.Center, CEUtils.randomPointInCircle(8), ModContent.ProjectileType<LuminarGrave>(), 0, 0, projectile.owner);
                }
                projectile.active = false;
                return false;
            }
            if (SmartArcEffect) {
                Vector2 position = projectile.Center - projectile.velocity;
                Vector2 velocity = projectile.velocity * 0.2f;
                Vector2 top = position;
                int sparkLifetime2 = Main.rand.Next(4, 6);
                float sparkScale2 = Main.rand.NextFloat(0.64f, 0.8f);
                var sparkColor2 = Color.Lerp(Color.DeepSkyBlue, Color.SkyBlue, Main.rand.NextFloat(0, 1));

                PRTLoader.NewParticle<PRT_LineCal>(top, velocity, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            if (WisperArrow) {
                if (Freeze) {
                    // 鼠标世界坐标改走 CEUtils.mouseWorld() 扩展（内部为去灾厄自研实现，含联机同步）
                    if (wisperShine)
                        projectile.Center = projectile.GetOwner().Center + wisperOffset.RotatedBy((projectile.GetOwner().mouseWorld() - projectile.GetOwner().Center).ToRotation());
                    else
                        for (int i = 0; i < int.Max(9 - projectile.MaxUpdates, 1); i++)
                            projectile.Center = Vector2.Lerp(projectile.Center, projectile.GetOwner().Center + wisperOffset.RotatedBy((projectile.GetOwner().mouseWorld() - projectile.GetOwner().Center).ToRotation()), 0.01f);
                    if (projectile.velocity.Length() < 2)
                        projectile.velocity = Vector2.One * 2;
                    projectile.velocity = new Vector2(projectile.velocity.Length(), 0).RotatedBy((projectile.GetOwner().mouseWorld() - projectile.Center).ToRotation());
                    projectile.timeLeft++;
                    if (projectile.velocity.Length() * projectile.MaxUpdates < 46) {
                        projectile.velocity = new Vector2(projectile.velocity.Length(), 0).normalize().RotatedBy(projectile.velocity.ToRotation()) * (46f / projectile.MaxUpdates);
                    }
                }
                if (wisperShine) {
                    wisperShine = false;
                    ParticleOnMe = PRTLoader.NewParticle<PRT_HeavenfallStar2>(projectile.Center, Vector2.Zero, new Color(180, 120, 255), 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                }
            }
            if (LuminarArrow) {
                if (starTrailPt == null || starTrailPt.Lifetime <= 0) {
                    starTrailPt = PRTLoader.NewParticle<PRT_StarTrailParticle>(projectile.Center, Vector2.Zero, Color.LightBlue, 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    starTrailPt.addPoint = false;
                    starTrailPt.maxLength = projectile.MaxUpdates * 16;
                    starTrailPt2 = PRTLoader.NewParticle<PRT_StarTrailParticle>(projectile.Center, Vector2.Zero, Color.White, 0.64f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    starTrailPt2.addPoint = false;
                    starTrailPt2.maxLength = projectile.MaxUpdates * 16;

                }
                starTrailPt.Velocity = projectile.velocity * 0.8f * projectile.MaxUpdates;
                starTrailPt.Lifetime = 30;
                starTrailPt.Position = projectile.Center;
                starTrailPt.AddPoint(starTrailPt.Position + projectile.velocity * projectile.MaxUpdates - starTrailPt.Velocity * 0.3f);
                starTrailPt2.Velocity = projectile.velocity * 0.8f * projectile.MaxUpdates;
                starTrailPt2.Lifetime = 30;
                starTrailPt2.Position = projectile.Center;
                starTrailPt2.AddPoint(starTrailPt2.Position + projectile.velocity * projectile.MaxUpdates - starTrailPt2.Velocity * 0.3f);
                NPC homing = CEUtils.FindTarget_HomingProj(projectile, projectile.Center, 1000, (npc) => !luminarHited.Contains(npc));
                if (counter > 10 && homing != null) {
                    projectile.velocity *= (float)Math.Pow(0.97f * Utils.Remap(CEUtils.getDistance(projectile.Center, homing.Center), 0, 600, 0.8f, 1), 1f / projectile.MaxUpdates);

                    projectile.velocity += (homing.Center - projectile.Center).normalize() * Utils.Remap(CEUtils.getDistance(projectile.Center, homing.Center), 600, 0, 0.6f, 3) / ((float)projectile.MaxUpdates);
                }
            }
            // 原熵灾模式下对灾厄神明吞噬者烈焰弹幕的强化已随灾厄脱钩移除
            init_ = false;
            if (ProminenceArrow) {
                if (trail_pmn == null || !trail_pmn.active) {
                    trail_pmn = PRTLoader.NewParticle<PRT_ProminenceTrail>(projectile.Center + projectile.velocity * 2, Vector2.Zero, Color.White, projectile.scale)
                        .Configure(1, true, PRTDrawModeEnum.NonPremultiplied);
                }
                trail_pmn.AddPoint(projectile.Center + projectile.velocity * 1.5f);
                trail_pmn.AddPoint(projectile.Center + projectile.velocity * 2);
                trail_pmn.Lifetime = trail_pmn.Time + 11;
            }
            promineceDamageAddition -= 0.006f / projectile.MaxUpdates;
            if (zypArrow) {
                NPC target = projectile.FindTargetWithinRange(360, false);
                if (target != null && counter > 12) {
                    projectile.velocity += (target.Center - projectile.Center).SafeNormalize(Vector2.Zero) * 1.6f;
                    projectile.velocity *= 0.92f;
                }
            }
            // 堕化卡组:投掷武器轻微追踪(2026-08-31 平衡案)
            if (projectile.friendly && !projectile.minion && projectile.owner >= 0
                && projectile.DamageType.CountsAsClass(DamageClass.Throwing)
                && projectile.owner.ToPlayer().Entropy().EvilDeck) {
                NPC homeTarget = projectile.FindTargetWithinRange(400, false);
                if (homeTarget != null && counter > 10) {
                    projectile.velocity += (homeTarget.Center - projectile.Center).SafeNormalize(Vector2.Zero) * 0.3f;
                    projectile.velocity *= 0.995f;
                }
            }
            if ((projectile.type == ProjectileID.LastPrismLaser || projectile.type == ProjectileID.LastPrism) && projectile.owner.ToPlayer().Entropy().WeaponBoost > 0) {
                if (counter % 2 == 0) {
                    projectile.extraUpdates += projectile.owner.ToPlayer().Entropy().WeaponBoost;
                }
                else {
                    projectile.extraUpdates -= projectile.owner.ToPlayer().Entropy().WeaponBoost;
                }
            }
            if (BarrenHoming) {
                NPC target = projectile.FindTargetWithinRange(Math.Max(projectile.width, projectile.height) + 600, projectile.tileCollide);
                if (target != null) {
                    float homingSpeed = 0.35f;
                    projectile.velocity += (target.Center - projectile.Center).SafeNormalize(Vector2.Zero) * homingSpeed;
                    projectile.velocity *= 1 - homingSpeed * (projectile.tileCollide ? 0.05f : 0.02f);
                }
            }
            if (ToFriendly) {
                projectile.usesLocalNPCImmunity = true;
                projectile.localNPCHitCooldown = 14;
                projectile.friendly = true;
                projectile.hostile = false;
            }
            if (dmgUpFrd && ToFriendly) {
                dmgUpFrd = false;
                projectile.damage *= EGlobalNPC.TamedDmgMul;
                projectile.originalDamage *= EGlobalNPC.TamedDmgMul;
            }
            if (ToFriendly) {
                /*NPC t = null;
                float dist = 4600;
                foreach (NPC n in Main.npc)
                {
                    if (n.active && !n.friendly && !n.dontTakeDamage)
                    {
                        if (Util.getDistance(n.Center, projectile.Center) < dist)
                        {
                            t = n;
                            dist = Util.getDistance(n.Center, projectile.Center);
                        }
                    }
                }
                if (t == null)
                {
                }
                else
                {
                    plrOldPos = Main.player[0].position;
                    plrOldVel = Main.player[0].velocity;
                    Main.player[0].Center = t.Center;
                    Main.player[0].velocity = t.velocity;
                }*/
            }
            if (projectile.Entropy().vdtype >= 0) {
                projectile.hostile = false;
                projectile.friendly = true;
            }
            if (vdtype >= 0 && !ProjectileID.Sets.IsARocketThatDealsDoubleDamageToPrimaryEnemy[projectile.type]) {
                vdtype = -1;
            }
            if (vdtype == 0) {
                projectile.velocity = projectile.velocity.RotatedBy(Math.Cos((counter + MathHelper.Pi * 0.5f) * 0.2f) * (float)vddirection * 0.18f);
                projectile.rotation = projectile.velocity.ToRotation();
            }
            if (withGrav) {
                projectile.velocity.Y += 0.3f / projectile.MaxUpdates;
            }
            dmgupcount--;
            if (maxDmgUps > 0 && dmgupcount <= 0 && projectile.DamageType == DamageClass.Ranged) {
                dmgupcount = 24 * projectile.extraUpdates;
                maxDmgUps--;
                projectile.damage = (int)(Math.Ceiling(projectile.damage * dmgUp)) + (projectile.damage);
            }
            if (GWBow && projectile.arrow) {
                if (Main.rand.NextBool(4 * projectile.MaxUpdates)) {
                    var __prt = PRTLoader.NewParticle<PRT_HeavenfallStar2>(projectile.Center + CEUtils.randomPointInCircle(16), projectile.velocity * 0.3f, Main.hslToRgb(0.85f, 1, 0.8f), 0.3f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    __prt.drawScale = Vector2.One;
                }
                if (Main.rand.NextBool(projectile.MaxUpdates)) {
                    var __prt = PRTLoader.NewParticle<PRT_HeavenfallStar2>(projectile.Center + CEUtils.randomPointInCircle(12), projectile.velocity * 0.1f, Main.hslToRgb(0.85f, 1, 0.8f), 1.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, projectile.velocity.ToRotation(), 16);
                    __prt.drawScale = new Vector2(0.4f, 1f);
                }
                for (float i = 0; i < 1; i += 0.25f) {
                    if (Main.rand.NextBool(int.Max(1, projectile.MaxUpdates / 3))) {
                        Vector2 direction = new Vector2(-1, 0).RotatedBy(projectile.velocity.ToRotation());
                        Vector2 smokeSpeed = direction.RotatedByRandom(MathHelper.PiOver4 * 0.1f) * Main.rand.NextFloat(10f, 30f) * 0.9f;

                        Vector2 p = projectile.Center - projectile.velocity * i;
                        if (Main.rand.NextBool(2)) {
                            var smoke = PRTLoader.NewParticle<PRT_HeavySmokeCal>(p + direction * 46f, smokeSpeed + projectile.velocity, Color.Lerp(Color.Purple, Color.Blue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 16f)), Main.rand.NextFloat(0.6f, 0.7f)).Configure(0.8f, 30, 0, false, 0, true);
                            smoke.Rotation = CEUtils.randomRot();
                        }
                        {
                            var smokeGlow = PRTLoader.NewParticle<PRT_HeavySmokeCal>(p + direction * 46f, smokeSpeed + projectile.velocity, Main.hslToRgb(0.85f, 1, 0.8f), Main.rand.NextFloat(0.36f, 0.5f)).Configure(0.8f, 20, 0.01f, true, 0.01f, true);
                            smokeGlow.Rotation = CEUtils.randomRot();
                        }
                    }
                }


                NPC target = projectile.FindTargetWithinRange(1000, false);
                if (target != null && counter > 15) {
                    gwHoming += (6 - gwHoming) * 0.0004f;
                    projectile.velocity = new Vector2(projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(projectile.velocity.ToRotation(), (target.Center - projectile.Center).ToRotation(), gwHoming.ToRadians() * projectile.velocity.Length(), true));
                }
            }
            if (flameTrail) {
                for (float i = 0; i < 1; i += 0.5f) {
                    Vector2 smokeSpeed = CEUtils.randomPointInCircle(2);

                    Vector2 p = projectile.Center - projectile.velocity * Main.rand.NextFloat();
                    var smokeGlow = PRTLoader.NewParticle<PRT_HeavySmokeCal>(p, smokeSpeed - projectile.velocity * 0.3f, Main.rand.NextBool() ? Color.OrangeRed : Color.Firebrick, Main.rand.NextFloat(0.3f, 0.36f)).Configure(1f, 16, Main.rand.NextFloat(-0.01f, 0.01f), true, 0.002f, true);
                    smokeGlow.Rotation = CEUtils.randomRot();

                }
            }
            if (projectile.Entropy().daTarget) {
                return false;
            }
            if (projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0 && projectile.friendly) {
                if (netsnc) {
                    projectile.netUpdate = true;
                    netsnc = false;
                }
                playerPosL = projectile.owner.ToPlayer().Center;
                projectile.owner.ToPlayer().Center = IndexOfTwistedTwinShootedThisProj.ToProj_Identity().Center;
            }
            if (ParticleOnMe != null) {
                ParticleOnMe.Position = projectile.Center;
            }
            projectile.Entropy().counter++;
            projectile.Entropy().odp.Add(zypArrow ? projectile.position : projectile.Center);
            projectile.Entropy().odp2.Add(projectile.Center);
            projectile.Entropy().odr.Add(projectile.rotation);
            if (projectile.Entropy().odp.Count > (zypArrow ? 24 : 7)) {
                projectile.Entropy().odp.RemoveAt(0);
                projectile.Entropy().odr.RemoveAt(0);
            }
            if (projectile.Entropy().odp2.Count > 22) {
                projectile.Entropy().odp2.RemoveAt(0);
            }
            if (projectile.Entropy().Lightning && projectile.owner >= 0) {
                Lighting.AddLight(projectile.Center, 0.7f, 0.7f, 0.9f);
            }
            // 2026-08-31 平衡案:神性重做为玩家中心隐形光环(GodheadAura),原弹幕AoE脉冲退役

            return true;
        }

        public bool SSFlag = true;
        public bool evRu = true;
        public bool SmartScopeHoming = false;
        public static int SSCD = 3;
        public override bool? CanHitNPC(Projectile projectile, NPC target) {
            if (WisperArrow && Freeze) {
                return false;
            }
            return null;
        }
        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity) {
            if (WisperArrow && Freeze) {
                return false;
            }
            return true;
        }
        public override void DrawBehind(Projectile projectile, int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            if (WisperArrow) {
                overPlayers.Add(index);
            }
        }
        public override void PostAI(Projectile projectile) {
            if (ashesArrow) {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(projectile.Center - projectile.velocity, projectile.velocity * 0.05f, Color.Orange, 0.034f).Configure(false, 11, new Vector2(0.3f, 1f), false, false);
            }
            if (projectile.friendly && projectile.DamageType == DamageClass.Ranged && projectile.GetOwner().HeldItem.useAmmo == AmmoID.Bullet) {
                if (projectile.GetOwner().Entropy().hasAcc(SmartScope.ID) && projectile.numHits < 1) {
                    if (SSFlag) {
                        SSFlag = false;
                        if (!(SSCD <= 0)) {
                            SmartScopeHoming = true;
                            SSCD--;
                        }
                    }
                    if (SmartScope.target != null && SmartScopeHoming) {
                        projectile.velocity = (SmartScope.target.Center - projectile.Center).normalize() * projectile.velocity.Length();
                    }
                }
            }
            if (evRu && EventideShot) {
                evRu = false;
                projectile.extraUpdates = (1 + projectile.extraUpdates) * 6 - 1;
            }
            if (plrOldPos.HasValue) {
                Main.player[0].position = plrOldPos.Value;
                plrOldPos = null;
            }
            if (plrOldVel.HasValue) {
                Main.player[0].velocity = plrOldVel.Value;
                plrOldVel = null;
            }
            if (projectile.owner >= 0) {
                if (projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0) {
                    projectile.owner.ToPlayer().Center = playerPosL;
                }
            }


            if (FirstFrames)
                FirstFrames = false;
        }
        public static bool CircleIntersectsRectangle(Vector2 circleCenter, float radius, Rectangle rectangle) {
            float nearestX = Math.Max(rectangle.Left, Math.Min(circleCenter.X, rectangle.Right));
            float nearestY = Math.Max(rectangle.Top, Math.Min(circleCenter.Y, rectangle.Bottom));

            float deltaX = circleCenter.X - nearestX;
            float deltaY = circleCenter.Y - nearestY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            return distance <= radius;
        }
        public Vector2 lmc;
        public Vector2 lastCenter;
        public bool withGrav = false;
        public int vdtype = -1;
        public bool rpBow = false;
        public float gwHoming = 0.06f;
        public FieldInfo SSMFInfo = null;
        public bool WisperArrow = false;
        public Vector2 wisperOffset = Vector2.Zero;
        public bool wisperShine = true;
        public override void PostDraw(Projectile projectile, Color lightColor) {
            if (Losted)
                Main.spriteBatch.ExitShaderRegion();

            if (ashesArrow) {
                Main.spriteBatch.UseAdditive();
                float sine = MathHelper.Lerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly * 50f / MathHelper.Pi)), 0.8f, 0.7f);
                Texture2D bTexture = ArchSmearTex.Value;
                for (int i = 0; i < 10; i++) {
                    float bScale2 = 0.75f;
                    float fxFade = 1f;
                    Vector2 scale = new Vector2((1 - i * 0.13f) * sine * 0.85f, ((1 + i * 0.012f) + fxFade * 0.2f) * 1.6f) * (bScale2 + i * 0.08f) * fxFade * 0.3f;
                    Vector2 aimVel = projectile.velocity;
                    Main.EntitySpriteDraw(bTexture, projectile.Center - projectile.velocity.normalize() * 24 - Main.screenPosition + aimVel.SafeNormalize(Vector2.UnitX) * -4, null, Color.Lerp(Color.Firebrick, Color.Orange, i * 0.15f) with { A = 255 } * fxFade * 0.9f, aimVel.ToRotation() + MathHelper.PiOver2, bTexture.Size() * 0.5f, scale, i % 2 == 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion(); ;
            }
            if (rpBow) {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                lightColor = Color.White;
                Texture2D txx = TextureAssets.Projectile[projectile.type].Value;
                float rot = 0;
                for (int i = 0; i < 8; i++) {
                    Main.spriteBatch.Draw(txx, projectile.Center + rot.ToRotationVector2() * 2 - Main.screenPosition, null, lightColor, projectile.rotation, new Vector2(txx.Width / 2, 0), float.Max(1, projectile.scale), SpriteEffects.None, 0);
                    rot += MathHelper.Pi * 2f / 8f;
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(txx, projectile.Center - Main.screenPosition, null, lightColor, projectile.rotation, new Vector2(txx.Width / 2, 0), float.Max(1, projectile.scale), SpriteEffects.None, 0);
            }
            if (zypArrow) {
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
                odp.Add(projectile.position);
                odp.Reverse();
                // 自有 TrailStreak 等效 shader + 自研图元渲染器；条带贴图暂用自绘 BasicTrail（原灾厄 ScarletDevilStreak，texture-map 就绪后可再换近似贴图）
                GameShaders.Misc["CalamityEntropy:TrailStreak"].SetShaderTexture(BasicTrailAsset);
                CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(WidthFunction_Zyp, ColorFunction_Zyp, (_, _) => projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityEntropy:TrailStreak"]), 30);
                odp.Reverse();
                odp.RemoveAt(odp.Count - 1);
                Texture2D txx = WyrmArrowTex.Value;
                Main.spriteBatch.ExitShaderRegion();
                Main.spriteBatch.Draw(txx, projectile.Center - Main.screenPosition + projectile.velocity.SafeNormalize(Vector2.UnitX) * 4, null, lightColor, projectile.velocity.ToRotation() + MathHelper.PiOver2, txx.Size() / 2f, float.Max(projectile.scale, 1), (projectile.velocity.X < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally), 0);
            }
            if (GWBow || WisperArrow) {

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Texture2D star = CEExtraAssets.StarTexture;

                float sx = (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 24) * 0.15f + 0.9f);
                float ls = GWBow ? 2 : 1;
                sx *= ls;
                Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition + new Vector2(-40, 0).RotatedBy(projectile.velocity.ToRotation()), null, Color.White, projectile.velocity.ToRotation(), star.Size() / 2, projectile.scale * new Vector2(0.03f, 0.03f) * sx, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition + new Vector2(10, 0).RotatedBy(projectile.velocity.ToRotation()), null, Color.White, projectile.velocity.ToRotation(), star.Size() / 2, projectile.scale * new Vector2(0.14f, 0.14f) * sx, SpriteEffects.None, 0);
                for (float i = 0; i < 1; i += 0.01f) {
                    Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition + new Vector2(float.Lerp(10, -40, i), 0).RotatedBy(projectile.velocity.ToRotation()), null, Color.MediumPurple, projectile.velocity.ToRotation(), star.Size() / 2, projectile.scale * new Vector2(0.1f, 0.01f) * (1.1f - i) * ls, SpriteEffects.None, 0);
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
            /*if (projectile.ModProjectile != null)
            {
                if (SSMFInfo == null)
                {
                    SSMFInfo = Main.spriteBatch.GetType().GetField("sortMode", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (SSMFInfo != null)
                {
                    var v = SSMFInfo.GetValue(Main.spriteBatch);
                    if (v != null && (SpriteSortMode)v == SpriteSortMode.Immediate)
                    {
                        Main.spriteBatch.ExitShaderRegion();
                    }
                }
            }*/

            if (IndexOfTwistedTwinShootedThisProj >= 0 && projectile.owner >= 0 && lastCenter != Vector2.Zero) {
                projectile.owner.ToPlayer().Center = lastCenter;
            }
        }
        public static float GetEventideDamageMultiplier(float radian, float maxR, float maxDmgMul) {
            if (radian >= maxR) {
                return 1f;
            }

            float ratio = radian / maxR;

            ratio = Math.Max(ratio, 0.0001f);

            return Math.Min(maxDmgMul, (float)(1 / Math.Log(ratio + 1)));
        }
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers) {
            if (IlmeranEnhanced) {
                modifiers.SourceDamage *= IlmeranAsylum.DMGMult + 1;
            }
            if (EventideShot) {
                float maxR = MathHelper.ToRadians(60);
                float maxDmgMul = 2;

                float r = CEUtils.GetAngleBetweenVectors(projectile.velocity, (target.Center - projectile.Center));
                if (r < MathHelper.ToRadians(16)) {
                    modifiers.SetCrit();
                }
                modifiers.SourceDamage *= GetEventideDamageMultiplier(r, maxR, maxDmgMul);
            }
            if (ProminenceArrow) {
                modifiers.SourceDamage *= 1 + promineceDamageAddition;
            }
        }
        public PRT_StarTrailParticle starTrailPt = null;   //LuminarArrow,addPoint=false,AI里手动AddPoint
        public PRT_StarTrailParticle starTrailPt2 = null;
        public override bool PreDraw(Projectile projectile, ref Color lightColor) {
            if (Losted) {
                Effect trans = CEEffectAssets.Trans;
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, trans);
                trans.Parameters["strength"].SetValue(1);
                trans.Parameters["color"].SetValue(new Vector4(0, 0, 0, 1));
                trans.CurrentTechnique.Passes[0].Apply();
            }
            if (WisperArrow)
                return false;
            this.projectile = projectile;
            if (IndexOfTwistedTwinShootedThisProj >= 0 && projectile.friendly) {
                lastCenter = projectile.owner.ToPlayer().Center;
                projectile.owner.ToPlayer().Center = IndexOfTwistedTwinShootedThisProj.ToProj_Identity().Center;
            }
            Texture2D tx;
            if (projectile.Entropy().DI) {
                lightColor = new Color(230, 230, 150);
            }
            if (projectile.Entropy().daTarget) {
                lightColor = Color.Black;
            }
            if (projectile.Entropy().gh && projectile.friendly && projectile.owner >= 0 && projectile.owner.ToPlayer().Entropy().GodHeadVisual) {
                tx = GodheadTex.Value;
                float rsize = (projectile.width + projectile.height) / 2 * 6;
                if (rsize < 128) {
                    rsize = 128;
                }
                if (rsize > 600) {
                    rsize = 600;
                }
                SpriteBatch sb = Main.spriteBatch;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(tx, projectile.Center - Main.screenPosition, null, new Color(248, 246, 165) * 0.4f, 0, tx.Size() / 2, rsize / 64, SpriteEffects.None, 0);
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            }
            if (projectile.Entropy().Lightning) {
                float size = 4;
                float sizej = size / odp2.Count;
                Color cl = new Color(250, 250, 255);
                for (int i = odp2.Count - 1; i >= 1; i--) {
                    CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, this.odp2[i], this.odp2[i - 1], cl * ((float)i / (float)odp2.Count), size);
                    size -= sizej;
                }
                tx = LightningArrowTex.Value;
                float x = 0f;
                for (int i = 0; i < projectile.Entropy().odp.Count; i++) {
                    Main.spriteBatch.Draw(tx, projectile.Entropy().odp[i] - Main.screenPosition, null, Color.White * x * 0.6f, projectile.Entropy().odr[i], new Vector2(tx.Width, tx.Height) / 2, projectile.scale, SpriteEffects.None, 0);
                    x += 1 / 7f;
                }
                Main.spriteBatch.Draw(tx, projectile.Center - Main.screenPosition, null, Color.White, projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, projectile.scale, SpriteEffects.None, 0);
                Main.spriteBatch.UseAdditive();
                float sine = MathHelper.Lerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly * 50f / MathHelper.Pi)), 0.8f, 0.7f);
                Texture2D bTexture = ArchSmearTex.Value;
                for (int i = 0; i < 10; i++) {
                    float bScale2 = 0.75f;
                    float fxFade = 1f;
                    Vector2 scale = new Vector2((1 - i * 0.13f) * sine, ((1 + i * 0.012f) + fxFade * 0.2f) * 1.6f) * (bScale2 + i * 0.08f) * fxFade * 0.3f;
                    Vector2 aimVel = projectile.velocity;
                    Main.EntitySpriteDraw(bTexture, projectile.Center - Main.screenPosition + aimVel.SafeNormalize(Vector2.UnitX) * -4, null, Color.Lerp(Color.Aquamarine, Color.Aqua, i * 0.15f) with { A = 255 } * fxFade * 1f, aimVel.ToRotation() + MathHelper.PiOver2, bTexture.Size() * 0.5f, scale, i % 2 == 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();

                return false;
            }
            if (vdtype >= 0) {
                Color color = Color.Purple;
                if (vdtype == 1) {
                    color = Color.IndianRed;
                }
                if (vdtype == 2) {
                    color = Color.DeepSkyBlue;
                }
                if (vdtype == 3) {
                    color = Color.Black;
                }
                if (vdtype == 4) {
                    color = Color.DarkRed;
                }
                float size = 5;
                float sizej = size / odp2.Count;
                for (int i = odp2.Count - 1; i >= 1; i--) {
                    CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, this.odp2[i], this.odp2[i - 1], color * ((float)i / (float)odp2.Count), size);
                    size -= sizej;
                }

            }

            if (LuminarArrow) {
                return false;
            }
            if (rpBow) {
                return false;
            }
            if (zypArrow) {
                return false;
            }
            return true;
        }
        public Projectile projectile;
        internal Color ColorFunction_Zyp(float completionRatio, Vector2 vertexPos) {
            float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos(-Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio, true) * projectile.Opacity;
            Color colorHue = Color.LightSkyBlue;

            Color endColor = Color.Lerp(colorHue, Color.Turquoise, (float)Math.Sin(completionRatio * MathHelper.Pi * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
        }

        internal float WidthFunction_Zyp(float completionRatio, Vector2 vertexPos) {
            float expansionCompletion = (float)Math.Pow(1 - completionRatio, 3);
            return MathHelper.Lerp(0f, 12 * float.Max(projectile.scale, 1) * projectile.Opacity, expansionCompletion);
        }

        public bool zypArrow = false;

        public override void OnKill(Projectile projectile, int timeLeft) {
            OnKillActions?.Invoke(projectile);
            if (ashesArrow) {
                float scale = 1.6f;
                PRTLoader.NewParticle<PRT_ShineParticle>(projectile.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
                PRTLoader.NewParticle<PRT_CustomPulse>(projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
            }
            if (projectile.friendly) {
                if (vdtype == 4) {
                    for (int i = 0; i < 2; i++) {
                        Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.position, projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.ToRadians(180)).RotatedByRandom(35) * 16, projectile.type, ((int)(projectile.damage * 0.7f)), projectile.knockBack, projectile.owner, projectile.ai[0], projectile.ai[1], projectile.ai[2]);
                    }
                }
                if (rpBow && Main.myPlayer == projectile.owner) {
                    for (int i = 0; i < 3; i++) {
                        Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, new Vector2(30, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), ModContent.ProjectileType<Lightning>(), (int)(projectile.damage * 0.3f), 4, projectile.owner, 0, 0, (Main.rand.NextBool(8) ? 1 : 0));
                    }
                }
            }
        }

        public bool MariExplode = true;
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) {
            if (ashesArrow) {
                target.AddBuff(BuffID.OnFire3, 120);
            }
            if (zypArrow) {
                PRTLoader.NewParticle<PRT_GlowOrbCal>(projectile.Center, Vector2.Zero, new Color(Main.rand.Next(140, 220), Main.rand.Next(140, 220), 255), 4).Configure(false, 20);

                for (int i = 0; i < 8; i++) {
                    Vector2 pos = target.Center;
                    Vector2 vel = projectile.velocity.normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(8, 38);
                    Color clr = new Color(Main.rand.Next(140, 220), Main.rand.Next(140, 220), 255);
                    Color clr2 = Color.Blue;
                    float scale = Main.rand.NextFloat(0.5f, 0.9f) * 0.08f;
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(pos, vel, clr, scale).Configure(false, 20, new Vector2(0.25f, 1));
                }
            }
            if (Lightning) {
                for (int i = 0; i < 14; i++) {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, ModContent.DustType<SquashDust>(), -projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2f, 2.5f);
                    dust.velocity = (projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(16f, 30));
                    dust.noGravity = true;
                    dust.color = Color.Aqua;
                    dust.fadeIn = 2f;
                }
                PRTLoader.NewParticle<PRT_CustomPulse>(projectile.Center, Vector2.Zero, new Color(60, 255, 255), 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 0.8f, 20);
                if (projectile.numHits == 0) {
                    Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, projectile.velocity * projectile.MaxUpdates, ModContent.ProjectileType<KinanitionSpawn>(), projectile.damage, projectile.knockBack, projectile.owner);
                }
            }
            foreach (int id in applyBuffs) {
                target.AddBuff(id, 5 * 60);
            }
            if (zypArrow) {
                target.AddBuff<LifeOppress>(600);
            }
            if (GWBow) {
                CEUtils.PlaySound("bne_hit", 1.2f + 0.2f * projectile.numHits, target.Center);
                var __prt = PRTLoader.NewParticle<PRT_HeavenfallStar2>(target.Center, Vector2.Zero, Main.hslToRgb(0.85f, 1, 0.8f), 2.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 28);
                __prt.drawScale = Vector2.One;
                var __prt2 = PRTLoader.NewParticle<PRT_HeavenfallStar2>(target.Center, Vector2.Zero, Main.hslToRgb(0.85f, 1, 0.8f), 1.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, MathHelper.PiOver4, 40);
                __prt2.drawScale = Vector2.One;
            }
            if (projectile.friendly && projectile.DamageType.CountsAsClass(DamageClass.Ranged)) {
                if (projectile.GetOwner().Entropy().fruitCake) {
                    if (Main.rand.NextBool(84)) {
                        int buffIndex = Main.rand.Next(BuffLoader.BuffCount);
                        //脱离灾厄:DoT排除过滤同步兼容自有PortsDoT命名空间(原仅排除灾厄DamageOverTime)
                        string buffNs = BuffLoader.GetBuff(buffIndex)?.GetType().Namespace ?? "";
                        if (Main.debuff[buffIndex] && BuffLoader.GetBuff(buffIndex) != null && !buffNs.Contains("DamageOverTime") && !buffNs.Contains("PortsDoT")) {
                            target.AddBuff(buffIndex, 120);
                        }
                    }
                }
            }
            if (typhoonBullet) {
                Vector2 top = projectile.Center;
                Vector2 sparkVelocity2 = projectile.velocity.normalize().RotateRandom(0.3f) * Main.rand.NextFloat(16f, 36f);
                int sparkLifetime2 = Main.rand.Next(16, 26);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));

                if (target.Organic()) {
                    CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(0.8f, 1.2f), target.Center, 3, volume: 0.3f);
                }
                else {
                    CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.6f, 1f), target.Center, 3, volume: 0.22f);
                }
            }
            if (LuminarArrow) {
                luminarHited.Add(target.whoAmI);
                CEUtils.PlaySound("LuminarArrowHit", Main.rand.NextFloat(0.7f, 1.3f), projectile.Center);
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(projectile.Center, Vector2.Zero, Color.LightBlue, 0.02f).Configure(new Vector2(2f, 2f), 0, 0.6f * 0.4f, 12);
                for (int i = 0; i < 4; i++) {
                    PRTLoader.NewParticle<PRT_StarTrailParticle>(projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(10, 20), Color.White, Main.rand.NextFloat(0.6f, 1.2f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                }
                // 自研移植的星幻感染 DoT（debuff-map：PortsDoT 同短名）
                target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);
            }
            if (IlmeranEnhanced) {
                // 自研移植的溺渊重压 DoT（debuff-map：PortsDoT 同短名）
                target.AddBuff(ModContent.BuffType<CrushDepth>(), 400);
            }
            if (projectile.type == ProjectileID.Terragrim && projectile.velocity.Y > 0 && projectile.velocity.Y > Math.Abs(projectile.velocity.X)) {
                projectile.GetOwner().velocity.Y = -projectile.velocity.Y * 0.4f;
                projectile.GetOwner().Entropy().gravAddTime = 30;
            }
            hittingTarget = -1;
            if (ProminenceArrow || projectile.ModProjectile is ProminenceSplitShot) {
                // 自研移植的圣火 DoT（debuff-map：PortsDoT 同短名）+ 原版破晓，与脱钩前双 debuff 行为一致
                target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
                target.AddBuff(BuffID.Daybreak, 300);
            }
            // 潜行系统退役：玛瑞薇姆套装原「潜伏攻击命中水爆」效果暂停用，新机制后续按映射表实装
            BarrenHoming = false;
            if (vdtype == 3) {
                EGlobalNPC.AddVoidTouch(target, 240, 10, 800, 16);
            }
            if (GWBow) {
                EGlobalNPC.AddVoidTouch(target, 160, 6, 600, 20);
            }
            if (EventideShot) {
                float r = CEUtils.GetAngleBetweenVectors(projectile.velocity, (target.Center - projectile.Center));
                if (r < MathHelper.ToRadians(15)) {
                    target.AddBuff(ModContent.BuffType<VoidVirus>(), 320);
                    CEUtils.PlaySound("voidseekercrit", 1, projectile.Center);
                    EGlobalNPC.AddVoidTouch(target, 160, 10, 800, 10);
                    projectile.owner.ToPlayer().Heal(16);
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), 0, 0, projectile.owner);
                }
            }
            if (projectile.Entropy().Lightning && projectile.penetrate >= 5) {
                projectile.velocity *= 1.4f;
            }
            if (projectile.owner != -1 && projectile.friendly) {
                EModPlayer plr = projectile.owner.ToPlayer().Entropy();
                // 2026-08-31 平衡案:瘟疫内燃机重做,原真近战回血/临时护甲退役(新效果在 EModPlayer.OnHitNPC/OnHurt)
                if (plr.holyMoonlight && plr.HMRegenCd <= 0) {
                    // 2026-08-31 平衡案:护盾冷却期间按当前魔力吸血(100:1),45帧CD,单次上限5
                    if (plr.MagiShield <= 0) {
                        plr.HMRegenCd = 45;
                        int heal = int.Min(5, projectile.owner.ToPlayer().statMana / 100);
                        if (heal > 0) {
                            projectile.owner.ToPlayer().Heal(heal);
                        }
                    }
                }
                if (plr.VFHelmMagic && projectile.owner >= 0) {
                    // 2026-08-31 平衡案:虚灵宙法盔改为攻击敌人后大幅提升自然生命再生(5hp/s,持续5秒)
                    plr.cosmosRegenTime = 300;
                }
            }
        }

    }
}
