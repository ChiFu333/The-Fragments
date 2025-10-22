using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "new Consumable Class", menuName = "Item/Consumable")]
public class ConsumableClass : ItemClass
{
    //data specific to consumable class
    public override ItemClass GetItem() { return this; }
    public override MiscClass GetMisc() { return null; }
    public override ConsumableClass GetConsumable() { return this; }
}
