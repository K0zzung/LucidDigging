/*using UnityEngine;

public class ItemPickup : GridPickup
{
    [SerializeField]
    private ItemData itemData;


    public override void Collect() {
        if (itemData == null)
            return;

        string key =
            $"Item_{itemData.itemId}_Owned";

        PlayerPrefs.SetInt(
            key,
            1);

        PlayerPrefs.Save();

        Debug.Log(
            $"Item È¹µæ : {itemData.itemName}");

        Destroy(gameObject);
    }
}*/