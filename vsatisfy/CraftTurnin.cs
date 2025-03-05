using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Lumina.Data.Files;
using Dalamud.Game.ClientState.Objects;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.Sheets;
using System.Numerics;
using System.Linq.Expressions;

namespace Satisfy;


// data & functions needed to buy crafting ingredients, craft and turn-in
public sealed class CraftTurnin
{
    public ulong VendorInstanceId;
    public Vector3 VendorLocation;
    public uint VendorShopId;
    public ulong TurnInInstanceId;
    public Vector3 TurnInLocation;
    //private static int jobid;
    //private static readonly IClientState _clientState;
    public static readonly IClientState myjobid;

    public CraftTurnin(uint supplyId, uint turnInENPCId, uint territoryId)
    {
        // note: we assume that first supply subrow for rank-one supply is always a craft (if this changes, we can check Slot column)
        // note: we assume that all ingredients are sold by the same vendor of the same shop in the same territory as turn-in npc
        var craftedItemId = Service.LuminaRow<SatisfactionSupply>(supplyId, 0)!.Value.Item.RowId;
        var ingredientId = GetCraftIngredient(craftedItemId).id;
        //jobid = (int)_clientState.LocalPlayer!.ClassJob.RowId;
        string scene = Service.LuminaRow<TerritoryType>(territoryId)!.Value.Bg.ToString();
        var filenameStart = scene.LastIndexOf('/') + 1;
        var planeventLayerGroup = "bg/" + scene[0..filenameStart] + "planevent.lgb";
        Service.Log.Debug($"Territory {territoryId} -> {planeventLayerGroup}");
        var lvb = Service.DataManager.GetFile<LgbFile>(planeventLayerGroup);
        if (lvb != null)
        {
            foreach (var layer in lvb.Layers)
            {
                foreach (var instance in layer.InstanceObjects)
                {
                    if (instance.AssetType != LayerEntryType.EventNPC)
                        continue;

                    var baseId = ((LayerCommon.ENPCInstanceObject)instance.Object).ParentData.ParentData.BaseId;
                    if (baseId == turnInENPCId)
                    {
                        TurnInInstanceId = (1ul << 32) | instance.InstanceId;
                        TurnInLocation = new(instance.Transform.Translation.X, instance.Transform.Translation.Y, instance.Transform.Translation.Z);
                        Service.Log.Debug($"Found turn-in npc {baseId} {instance.InstanceId} '{Service.LuminaRow<ENpcResident>(baseId)?.Singular}' at {TurnInLocation}");
                    }

                    var vendor = FindVendorItem(baseId, ingredientId);
                    if (vendor.itemIndex >= 0)
                    {
                        VendorInstanceId = (1ul << 32) | instance.InstanceId;
                        VendorLocation = new(instance.Transform.Translation.X, instance.Transform.Translation.Y, instance.Transform.Translation.Z);
                        VendorShopId = vendor.shopId;
                        Service.Log.Debug($"Found vendor npc {baseId} {instance.InstanceId} '{Service.LuminaRow<ENpcResident>(baseId)?.Singular}' at {VendorLocation}: shop {vendor.shopId} '{Service.LuminaRow<GilShop>(vendor.shopId)?.Name}' #{vendor.itemIndex}");
                    }
                }
            }
        }
    }

    // TODO: job selection
    public static uint GetRecipeId(uint craftedItemId)
    {
        //Carpenter = 8,
        //Blacksmith = 9,
        //Armorer = 10,
        //Goldsmith = 11,
        //Leatherworker = 12,
        //Weaver = 13,
        //Alchemist = 14,
        //Culinarian = 15,
        //Dalamud.Plugin.Services.IClientState myjobid;
        int number = (int)myjobid.LocalPlayer!.ClassJob.RowId;
        switch (number)
        {
            case 8:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.CRP.RowId ?? 0;
            case 9:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.BSM.RowId ?? 0;
            case 10:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.ARM.RowId ?? 0;
            case 11:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.GSM.RowId ?? 0;
            case 12:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.LTW.RowId ?? 0;
            case 13:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.WVR.RowId ?? 0;
            case 14:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.ALC.RowId ?? 0;
            case 15:
                // code block
                return Service.LuminaRow<RecipeLookup>(craftedItemId)?.CUL.RowId ?? 0;
            default:
                // code block
                return 0;
        }

        
    }

    public static (uint id, int count) GetCraftIngredient(uint craftedItemId)
    {
        int number = (int)myjobid.LocalPlayer!.ClassJob.RowId;
        var recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.CUL.Value;
        switch (number)
        {
            case 8:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.CRP.Value;
                break;
            case 9:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.BSM.Value;
                break;
            case 10:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.ARM.Value;
                break;
            case 11:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.GSM.Value;
                break;
            case 12:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.LTW.Value;
                break;
            case 13:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.WVR.Value;
                break;
            case 14:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.ALC.Value;
                break;
            case 15:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.CUL.Value;
                break;
            default:
                // code block
                recipe = Service.LuminaRow<RecipeLookup>(craftedItemId)?.CUL.Value;
                break;
        }
        
        return recipe != null ? (recipe.Value.Ingredient[0].RowId, recipe.Value.AmountIngredient[0]) : default;
    }

    private static (uint shopId, int itemIndex) FindVendorItem(uint enpcId, uint itemId)
    {
        var enpcBase = Service.LuminaRow<ENpcBase>(enpcId);
        if (enpcBase == null)
            return (0, -1);

        foreach (var handler in enpcBase.Value.ENpcData)
        {
            if ((handler.RowId >> 16) != (uint)EventHandlerType.Shop)
                continue;

            var items = Service.LuminaSubrows<GilShopItem>(handler.RowId);
            if (items == null)
                continue;

            for (int i = 0; i < items.Value.Count; ++i)
            {
                var shopItem = items.Value[i];
                if (shopItem.Item.RowId == itemId)
                {
                    return (handler.RowId, i);
                }
            }
        }
        return (0, -1);
    }
}
