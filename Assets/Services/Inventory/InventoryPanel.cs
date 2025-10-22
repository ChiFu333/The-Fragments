using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InventoryPanel : MonoBehaviour
{
    [HideInInspector] public Canvas Canvas;
    [SerializeField] private Image backpackImage;
    [SerializeField] private Image backpackImageBack;
    [SerializeField] private GameObject InventoryLine;
    [SerializeField] private GameObject ShadowGradient;
    [SerializeField] private Sprite closedBackpack, openedBackpack;
    private float closedPos = -408f;
    private float openedPos = 54f;
    private float closedScale = 0.02f;
    private float openedScale = 1f;
    
    private float openingTime = 0.4f;

    private Ease showEase = Ease.OutBack;
    private Ease hideEase = Ease.InBack;
    
    private List<SlotClass> items = new List<SlotClass>();
    private List<GameObject> slots = new List<GameObject>();

    private void Awake()
    {
        Canvas = GetComponent<Canvas>();
        for (int i = 0; i < InventoryLine.transform.GetChild(0).childCount; i++)
            slots.Add(InventoryLine.transform.GetChild(0).GetChild(i).gameObject);
        Add(CMS.Get<CMSEntity>("CMS/Inventory/Headphone").Get<TagInventoryItem>());
        Add(CMS.Get<CMSEntity>("CMS/Inventory/Apple").Get<TagInventoryItem>());
        float x = InventoryLine.GetComponent<RectTransform>().anchoredPosition.x;
        InventoryLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, closedPos);
        InventoryLine.transform.localScale = new Vector3(1, closedScale, 1);
    }

    public async UniTask OpenBackpack()
    {
        backpackImage.sprite = openedBackpack;
        backpackImageBack.gameObject.SetActive(true);
        InventoryLine.SetActive(true);
        ShadowGradient.GetComponent<Image>().DOFade(0.9f, 0.3f);
        RefreshUI();
        float x = InventoryLine.GetComponent<RectTransform>().anchoredPosition.x;
        InventoryLine.GetComponent<RectTransform>().DOAnchorPos(new Vector2(x, openedPos), openingTime).SetEase(showEase);
        await InventoryLine.transform.DOScaleY(openedScale, openingTime).SetEase(showEase).AsyncWaitForCompletion().AsUniTask();
    }

    public async UniTask CloseBackpack()
    {
        ShadowGradient.GetComponent<Image>().DOFade(0, 0.3f);
        float x = InventoryLine.GetComponent<RectTransform>().anchoredPosition.x;
        InventoryLine.GetComponent<RectTransform>().DOAnchorPos(new Vector2(x, closedPos), openingTime).SetEase(hideEase);
        await InventoryLine.transform.DOScaleY(closedScale, openingTime).SetEase(hideEase).AsyncWaitForCompletion().AsUniTask();
        backpackImage.sprite = closedBackpack;
        backpackImageBack.gameObject.SetActive(false);
        InventoryLine.SetActive(false);
    }
    public void RefreshUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            try
            {
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = items[i].GetItem().itemIcon;
                if (items[i].GetItem().isStackable)
                    slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = items[i].GetQuantity().ToString();
                else slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
            }
            catch
            {
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = null;
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
                slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
            }
        }
    }

    public bool Add(TagInventoryItem item)
    {
        //check if inventory contains item
        SlotClass slot = Contains(item);
        if (slot != null && slot.GetItem().isStackable) slot.AddQuantity(1);
        else
        {
            if (items.Count < slots.Count) items.Add(new SlotClass(item, 1));
            else return false;
        }

        RefreshUI();
        return true;
    }
    public bool Remove(TagInventoryItem item)
    {
        SlotClass temp = Contains(item);
        if (temp != null)
        {
            if (temp.GetQuantity() > 1) temp.SubQuantity(1);
            else
            {
                SlotClass slotToRemove = new SlotClass();
                foreach (SlotClass slot in items)
                {
                    if (slot.GetItem() == item)
                    {
                        slotToRemove = slot;
                        break;
                    }
                }
                items.Remove(slotToRemove);
            }
        }
        else return false;

        RefreshUI();
        return true;
    }

    public SlotClass Contains(TagInventoryItem item)
    {
        foreach (SlotClass slot in items)
        {
            if(slot.GetItem() == item) return slot;
        }

        return null;
    }
}