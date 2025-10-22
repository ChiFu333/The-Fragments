using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Inventory : MonoBehaviour, IService, INeedCameraForCanvas
{
    [HideInInspector] public InventoryPanel InventoryPanel;
    public bool isOpenedInventory = false;
    private bool isInAnim;
    public void Init()
    {
        GameObject g = Instantiate(Resources.Load<GameObject>("Services/" + "Inventory"), GameBootstrapper.serviceHolder.transform, true);
        InventoryPanel = g.GetComponent<InventoryPanel>();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isInAnim)
        {
            OpenBackpack(!isOpenedInventory).Forget();
            isOpenedInventory = !isOpenedInventory;
        }
    }

    private async UniTask OpenBackpack(bool toOpen)
    {
        isInAnim = true;
        if (toOpen)
            await InventoryPanel.OpenBackpack();
        else
            await InventoryPanel.CloseBackpack();
        await UniTask.Delay(100);
        isInAnim = false;
    }
    public void UpdateCanvasField(Camera c)
    {
        InventoryPanel.Canvas.worldCamera = c;
    }
}
