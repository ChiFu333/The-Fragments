using System.Collections;
using UnityEngine;

public abstract class ItemClass : ScriptableObject
{
    [Header("Item")] //data shared across every item
    public string itemName;
    public Sprite itemIcon;
    public bool isStackable = true;

    public abstract ItemClass GetItem();
    public abstract MiscClass GetMisc();
    public abstract ConsumableClass GetConsumable();
}
