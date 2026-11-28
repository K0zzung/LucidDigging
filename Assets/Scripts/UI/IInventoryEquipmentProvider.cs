using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct InventoryEquipmentItem
{
    public readonly string Id, Name;
    public readonly Sprite Icon;
    public readonly bool Unlocked, Equipped, InHand;
    public InventoryEquipmentItem(string id,string name,Sprite icon,bool unlocked,bool equipped,bool inHand) {
        Id=id;Name=name;Icon=icon;Unlocked=unlocked;Equipped=equipped;InHand=inHand;
    }
}

// Optional real equipment integration. UI never invents ownership, saves or combat effects.
public interface IInventoryEquipmentProvider
{
    IReadOnlyList<InventoryEquipmentItem> Items { get; }
    bool TryEquip(string id);
    event Action Changed;
}
