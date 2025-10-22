using System;
using UnityEngine;
[Serializable]
public class TagInventoryItem : EntityComponentDefinition
{
    [Header("Item")] //data shared across every item
    public string itemName;
    public Sprite itemIcon;
    public bool isStackable = true;
}
