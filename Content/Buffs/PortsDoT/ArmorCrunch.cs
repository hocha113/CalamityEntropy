namespace CalamityEntropy.Content.Buffs.PortsDoT
{
    /// <summary>破甲：受击时防御 -15（结算在 CEDoTGlobalNPC.ModifyIncomingHit）</summary>
    public class ArmorCrunch : CEPortDebuff
    {
        public const int DefenseReduction = 15;

        public override bool NurseCannotRemove => true;
    }
}
