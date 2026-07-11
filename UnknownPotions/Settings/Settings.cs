using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.Collections.Generic;

namespace UnknownPotions.Settings
{
    public class Settings
    {
        [MaintainOrder, SettingName("Create Unknown Potions"), Tooltip("The main function of this patcher - you probably want this on.")]
        public bool CopyPotions = true;

        [MaintainOrder, SettingName("Replace Placed Objects"), Tooltip("Finds all potions placed on map and replaces with unknown potion. - REQUIRES MAIN PATCHER - This will take a few minutes...")]
        public bool PatchPlacedObjects = true;

        [MaintainOrder, SettingName("Patch Leveled Lists"), Tooltip("Edits existing leveled lists to replace any potions within.")]
        public bool PatchLeveledLists = true;

        [MaintainOrder, SettingName("Patch Merchant Inventories"), Tooltip("When ON - undos leveled list changes for merchant stock. Makes merchants sell KNOWN potions.")]
        public bool PatchMerchantsFix = true;

        [MaintainOrder, SettingName("Better Logging"), Tooltip("Writes all found items in output log.")]
        public bool ExtraLogging = true;

    }
    public class ManualLists
    {
        [MaintainOrder, SettingName("Manual Potion Types"), Tooltip("Add potion EditorIDs to to create unknown potion copies. - Partial Matches Allowed.")]
        public List<string> PotionTypes = new();

        [MaintainOrder, SettingName("Manual Poison Types"), Tooltip("Add poison EditorIDs to to create unknown poison copies. - Partial Matches Allowed.")]
        public List<string> PoisonTypes = new();

        [MaintainOrder, SettingName("Excluded Potion Types"), Tooltip("Add EditorIDs the patcher should ignore. - Partial Matches Allowed.")]
        public List<string> BannedTypes = new();

        [MaintainOrder, SettingName("Explicit Potion Types"), Tooltip("Add EditorIDs that will ALWAYS create unknown copies - even if in banned list. - Partial Matches Allowed.")]
        public List<string> ExplicitTypes = new();

        [MaintainOrder, SettingName("Manual Leveled Lists"), Tooltip("Add EditorIDs of leveled lists the patcher should include. - Partial Matches Allowed.")]
        public List<string> ListNames = new();

        [MaintainOrder, SettingName("Excluded Leveled Lists"), Tooltip("Add EditorIDs of leveled lists the patcher should ignore. - Partial Matches Allowed.")]
        public List<string> BannedLists = new();

        [MaintainOrder, SettingName("Manual Merchant Names"), Tooltip("Add EditorIDs of merchant CONTAINER names to patch. - Partial Matches Allowed.")]
        public List<string> MerchantContainers = new();

        [MaintainOrder, SettingName("Manual Merchant Cells"), Tooltip("Add EditorIDs of merchant CELL names to patch. - Partial Matches Allowed.")]
        public List<string> MerchantCells = new();

        [MaintainOrder, SettingName("Excluded Leveled Lists"), Tooltip("Add EditorIDs of merchant CONTAINER names the patcher should ignore. - Partial Matches Allowed.")]
        public List<string> BannedContainers = new();
    }
}