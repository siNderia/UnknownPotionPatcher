using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;


namespace UnknownPotions
{
    public static class MerchantPatcher
    {
        private static readonly Dictionary<FormKey, ILeveledItemGetter> ClonedLists = new();
        private static readonly HashSet<FormKey> ActiveLists = new();
        private static readonly Dictionary<FormKey, bool> PotionListCache = new();

        private static ILinkCache<ISkyrimMod, ISkyrimModGetter> _linkCache;
        private static IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;

        private static bool _logging;

        public static void Run(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder, bool logging)
        {
            _state = state;
            _logging = logging;

            ClonedLists.Clear();
            ActiveLists.Clear();

            _linkCache = loadOrder.ToImmutableLinkCache();

            //BEGIN MERCHANT PATCHING
            Console.WriteLine(Environment.NewLine + "Patching Merchant Inventory..." + Environment.NewLine);
            if (_logging) Thread.Sleep(3500);

            int containerCount = 0;
            int merchants = 0;
            int patched = 0;

            foreach (var container in state.LoadOrder.PriorityOrder.Container().WinningOverrides())
            {
                if (container.EditorID == null) continue;
                containerCount++;

                //Blacklist check
                if (!IsMerchantContainer(container.EditorID)) continue;
                bool hasPotionInventory = false;


                foreach (var item in container.Items.EmptyIfNull())
                {
                    if (item.Item.Item == null) continue;


                    if (ContainsPotionRecursive(item.Item.Item.FormKey))
                    {
                        hasPotionInventory = true;
                        break;
                    }
                }

                if (!hasPotionInventory)
                {
                    if (_logging) Console.WriteLine($"Skipping merchant without potions: {container.EditorID}");
                    continue;
                }

                merchants++;
                if (_logging) Console.WriteLine($"Potion merchant found: {container.EditorID}");

                var copy = container.DeepCopy();
                bool changed = false;

                foreach (var item in copy.Items.EmptyIfNull())
                {
                    if (item.Item.Item == null) continue;
                    if (!item.Item.Item.TryResolve<ILeveledItemGetter>(_linkCache, out var leveled)) continue;
                    if (!ContainsPotionRecursive(leveled.FormKey)) continue;

                    var newList = CloneLeveledList(leveled);

                    if (newList != null && newList.FormKey != leveled.FormKey)
                    {
                        item.Item.Item.SetTo(newList.FormKey);
                        changed = true;
                    }
                }

                if (changed)
                {
                    state.PatchMod.Containers.GetOrAddAsOverride(copy);
                    patched++;
                }
            }

            Console.WriteLine("--------------------");
            Console.WriteLine($"Containers scanned: {containerCount}");
            Console.WriteLine($"Merchants found: {merchants}");
            Console.WriteLine($"Merchants patched: {patched}");
            Console.WriteLine($"Lists duplicated: {ClonedLists.Count}");
            Console.WriteLine("--------------------");
        }
        private static ILeveledItemGetter CloneLeveledList(ILeveledItemGetter source)
        {
            if (_state == null || _linkCache == null) return null;

            //Already patched
            if (ClonedLists.TryGetValue(source.FormKey, out var existing))
                return existing;

            //Circular prevention
            if (ActiveLists.Contains(source.FormKey))
            {
                Console.WriteLine($"Circular reference detected: {source.EditorID}");
                return source;
            }

            ActiveLists.Add(source.FormKey);

            Console.WriteLine($"Cloning leveled list: {source.EditorID}");

            var newList = _state.PatchMod.LeveledItems.DuplicateInAsNewRecord(source);

            newList.EditorID = "Unk" + source.EditorID;

            ClonedLists[source.FormKey] = newList;

            foreach (var entry in newList.Entries.EmptyIfNull())
            {
                if (entry.Data == null) continue;

                var resolved = entry.Data.Reference.TryResolve(_linkCache);

                if (resolved == null) continue;


                //LL
                if (resolved is ILeveledItemGetter nested)
                {
                    var nestedClone = CloneLeveledList(nested);

                    if (nestedClone != null)
                    {
                        entry.Data.Reference.SetTo(nestedClone);
                    }

                    continue;
                }

                //Potion Replacement
                if (resolved is IIngestibleGetter ingestible)
                {
                    var replacement =
                        FindUnknownReplacement(ingestible);

                    if (replacement != null)
                    {
                        Console.WriteLine($"Potion replacement: {ingestible.EditorID} -> {replacement}");

                        entry.Data.Reference.SetTo(replacement);
                    }
                }
            }

            ActiveLists.Remove(source.FormKey);
            return newList;
        }

        private static FormKey? FindUnknownReplacement(IIngestibleGetter potion)
        {
            if (_linkCache == null)
                return null;

            if (potion.EditorID == null)
                return null;

            if (!potion.EditorID.StartsWith("UNK", StringComparison.OrdinalIgnoreCase))
                return null;

            //Lookup original potion
            string replacementID = potion.EditorID[3..];
            Console.WriteLine($"Searching replacement: {replacementID}");

            //Found
            if (_linkCache.TryResolveIdentifier<IIngestibleGetter>(replacementID, out var replacementFormKey))
                return replacementFormKey;

            //Not found
            Console.WriteLine($"Replacement not found: {replacementID}");
            return null;
        }

        private static bool ContainsPotionRecursive(FormKey formKey)
        {
            if (_linkCache == null) return false;

            if (PotionListCache.TryGetValue(formKey, out var cached))
                return cached;

            //Circular prevention
            if (ActiveLists.Contains(formKey))
                return false;

            ActiveLists.Add(formKey);
            bool result = false;

            if (_linkCache.TryResolve<IIngestibleGetter>(formKey, out _))
            {
                result = true;
            }
            else if (_linkCache.TryResolve<ILeveledItemGetter>(
                    formKey,
                    out var list))
            {
                foreach (var entry in list.Entries.EmptyIfNull())
                {
                    if (entry.Data == null) continue;


                    if (ContainsPotionRecursive(entry.Data.Reference.FormKey))
                    {
                        result = true;
                        break;
                    }
                }
            }

            //Cache and finish
            ActiveLists.Remove(formKey);
            PotionListCache[formKey] = result;
            return result;
        }

        //Probably a better way?
        private static bool IsMerchantContainer(string editorID)
        {

            string[] allowed =
            {
                "Merchant",
                "Vendor",
                "Shop"
            };

            string[] banned =
            {
                "Test",
                "Inn"
            };

            if (banned.Any(s => editorID.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return allowed.Any(s => editorID.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
    }
}