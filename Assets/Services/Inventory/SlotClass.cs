using System.Collections;
using UnityEngine;

[System.Serializable]
public class SlotClass
{
    [SerializeField] private TagInventoryItem item;
    [SerializeField] private int quantity;

    public SlotClass()
    {
        item = null;
        quantity = 0;
    }
    public SlotClass(TagInventoryItem _item, int _quantity)
    {
        item = _item;
        quantity = _quantity;
    }

    public TagInventoryItem GetItem() { return item; }
    public int GetQuantity() { return quantity; }
    public void AddQuantity(int _guantity) { quantity += _guantity; }
    public void SubQuantity(int _guantity) { quantity -= _guantity; }
}
