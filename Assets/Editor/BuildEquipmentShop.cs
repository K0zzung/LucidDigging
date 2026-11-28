using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit editor command only. No InitializeOnLoad, asset refresh loop or runtime builder.
public static class BuildEquipmentShop
{
    private static TMP_FontAsset font;
    private static Sprite slotSprite, outlineSprite, frameSprite, buttonSprite, pinkSprite;
    private static readonly Color Pale = new Color(.88f, .8f, 1f);

    [MenuItem("Tools/Lucid Digging/Build Equipment Shop %#u")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            Debug.LogError("Stop Play Mode before building the equipment shop."); return;
        }
        var shop = Object.FindObjectOfType<ShopPanelController>(true);
        if (shop == null) { Debug.LogError("Open the scene containing ShopPanelController first."); return; }
        var so = new SerializedObject(shop);
        ConfigureTabSprites(so);
        var panel = so.FindProperty("panel").objectReferenceValue as RectTransform;
        if (panel == null) return;
        if (panel.Find("EquipmentContent") != null) {
            UpdateExisting(panel.Find("EquipmentContent"));
            Selection.activeGameObject = panel.Find("EquipmentContent").gameObject;
            EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
            EditorSceneManager.SaveScene(shop.gameObject.scene);
            Debug.Log("EquipmentContent references updated and saved; layout preserved."); return;
        }
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Create editable equipment shop");
        font = ((TMP_Text)so.FindProperty("pinkText").objectReferenceValue).font;
        slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/UI/RunInventory/InventorySlot.png");
        outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/UI/RunInventory/EquippedOutline.png");
        var statButton = (Button)so.FindProperty("statButtons").GetArrayElementAtIndex(0).objectReferenceValue;
        frameSprite = statButton.image.sprite;
        buttonSprite = ((Button)so.FindProperty("buyButton").objectReferenceValue).image.sprite;
        pinkSprite = ((Image)so.FindProperty("costIcon").objectReferenceValue).sprite;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("41376b9f0c9a3514b8f700285f69dc45"))) {
            if (!(asset is Sprite sprite)) continue;
            string guid; long localId;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out localId) && localId == 1631781561L) pinkSprite = sprite;
        }

        var root = Rect("EquipmentContent", panel, Vector2.zero, new Vector2(1000, 1100));
        root.localScale = new Vector3(.01f, .01f, 1f);
        root.anchoredPosition = new Vector2(0, -1.1f);
        var weaponRow = Row("WeaponUpgradeCard", root, new Vector2(0, 330), false);
        var pickaxeRow = Row("PickaxeUpgradeCard", root, new Vector2(0, 65), true);
        var inventory = Rect("EquipmentSlots", root, new Vector2(0, -330), new Vector2(1000, 440));
        var invFrame = Image("Frame", inventory, Vector2.zero, inventory.sizeDelta,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/UI/RunInventory/InventoryPanel.png"));
        invFrame.raycastTarget = false;
        Text("WeaponHeading", inventory, "무기", new Vector2(-400, 110), new Vector2(165, 55), 34);
        Text("PickaxeHeading", inventory, "곡괭이", new Vector2(-400, -100), new Vector2(165, 55), 34);
        var divider = Image("Divider", inventory, new Vector2(0, 0), new Vector2(910, 3), null);
        divider.color = new Color(.45f, .3f, .65f, .7f);

        var equipment = new List<ShopEquipmentPanel.EquipmentSlot>();
        var manager = Object.FindObjectOfType<WeaponManager>(true);
        if (manager == null) { Debug.LogError("WeaponManager not found."); return; }
        var wm = new SerializedObject(manager);
        AddSlots(equipment, inventory, wm.FindProperty("allWeapons"), false);
        AddSlots(equipment, inventory, wm.FindProperty("allPickaxes"), true);
        foreach (var slot in equipment) {
            var row = slot.Item.IsPickaxe ? pickaxeRow : weaponRow;
            if (row.Icon.sprite != null) continue;
            row.Icon.sprite = slot.Item.InventoryIcon;
            row.Title.text = slot.Item.WeaponName;
        }
        var controller = Undo.AddComponent<ShopEquipmentPanel>(root.gameObject);
        controller.ConfigureEditor(shop, weaponRow, pickaxeRow, equipment.ToArray());
        EditorUtility.SetDirty(controller);
        var content = so.FindProperty("contentRoots");
        content.GetArrayElementAtIndex(1).objectReferenceValue = root.gameObject;
        // Keep the user's existing complete stat section, including both stat selectors.
        var status = panel.Find("Status");
        if (status != null) content.GetArrayElementAtIndex(0).objectReferenceValue = status.gameObject;
        so.ApplyModifiedProperties();
        // Initial editor presentation shows the new tab; runtime still opens on Stats.
        if (status != null) { Undo.RecordObject(status.gameObject, "Show equipment tab"); status.gameObject.SetActive(false); }
        // Both offers start with the same independent level/cost; real save data fills them on open.
        var scene = shop.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        EditorGUIUtility.PingObject(root.gameObject);
        Debug.Log("Equipment shop saved: two upgrade cards and six editable slots. No runtime UI generation.");
    }

    private static void ConfigureTabSprites(SerializedObject so) {
        var buttons = so.FindProperty("tabButtons");
        var selected = so.FindProperty("selectedTabSprite");
        var unselected = so.FindProperty("unselectedTabSprite");
        if (selected.objectReferenceValue == null) selected.objectReferenceValue = ((Button)buttons.GetArrayElementAtIndex(0).objectReferenceValue).image.sprite;
        if (unselected.objectReferenceValue == null) unselected.objectReferenceValue = ((Button)buttons.GetArrayElementAtIndex(1).objectReferenceValue).image.sprite;
        so.ApplyModifiedProperties();
        for (int i = 0; i < buttons.arraySize; i++) {
            var image = ((Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue).image;
            Undo.RecordObject(image, "Show equipment tab highlight");
            image.sprite = (Sprite)(i == 1 ? selected.objectReferenceValue : unselected.objectReferenceValue);
            EditorUtility.SetDirty(image);
        }
    }
    private static void UpdateExisting(Transform root) {
        var controller = root.GetComponent<ShopEquipmentPanel>();
        if (controller == null) return;
        var so = new SerializedObject(controller);
        Sprite pink = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("41376b9f0c9a3514b8f700285f69dc45"))) {
            if (!(asset is Sprite sprite)) continue;
            string guid; long id;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out id) && id == 1631781561L) pink = sprite;
        }
        var slots = so.FindProperty("slots");
        bool weaponSet = false, pickaxeSet = false;
        for (int i = 0; i < slots.arraySize; i++) {
            var item = slots.GetArrayElementAtIndex(i).FindPropertyRelative("Item").objectReferenceValue as WeaponData;
            if (item == null || (item.IsPickaxe ? pickaxeSet : weaponSet)) continue;
            var row = so.FindProperty(item.IsPickaxe ? "pickaxeRow" : "weaponRow");
            var icon = row.FindPropertyRelative("Icon").objectReferenceValue as UnityEngine.UI.Image;
            var title = row.FindPropertyRelative("Title").objectReferenceValue as TMP_Text;
            Undo.RecordObject(icon, "Link equipment icon"); icon.sprite = item.InventoryIcon; EditorUtility.SetDirty(icon);
            Undo.RecordObject(title, "Link equipment title"); title.text = item.WeaponName; EditorUtility.SetDirty(title);
            if (item.IsPickaxe) pickaxeSet = true; else weaponSet = true;
        }
        foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)) {
            if (image.name != "PinkCurrency" || pink == null) continue;
            Undo.RecordObject(image, "Link pink currency"); image.sprite = pink; EditorUtility.SetDirty(image);
        }
    }
    private static ShopEquipmentPanel.UpgradeRow Row(string name, Transform parent, Vector2 position, bool pickaxe) {
        var root = Rect(name, parent, position, new Vector2(1000, 245));
        var frame = root.gameObject.AddComponent<Image>(); frame.sprite = frameSprite; frame.raycastTarget = false;
        var slot = Image("IconFrame", root, new Vector2(-382, 0), new Vector2(182, 182), slotSprite);
        var icon = Image("SelectedEquipmentIcon", slot.transform, Vector2.zero, new Vector2(140, 140), null);
        icon.preserveAspect = true;
        var title = Text("EquipmentName", root, pickaxe ? "곡괭이 공격력" : "무기 공격력", new Vector2(-77, 69), new Vector2(390, 56), 36);
        var level = Text("UpgradeLevel", root, "Lv.0 → <color=#FFF270>Lv.1</color>", new Vector2(-77, 8), new Vector2(390, 48), 32);
        var effect = Text("UpgradeEffect", root, (pickaxe ? "터렛 특화" : "몬스터·보스 특화") + "\n+10 ATK / Lv  ·  강화 +0", new Vector2(-77, -63), new Vector2(390, 70), 23);
        effect.color = Pale;
        var line = Image("Divider", root, new Vector2(166, 0), new Vector2(3, 186), null);
        line.color = new Color(.52f, .32f, .7f, .75f);
        Image("PinkCurrency", root, new Vector2(270, 58), new Vector2(56, 56), pinkSprite).preserveAspect = true;
        var cost = Text("Cost", root, "x1", new Vector2(378, 58), new Vector2(160, 50), 36);
        var buyImage = Image("UpgradeButton", root, new Vector2(333, -53), new Vector2(255, 94), buttonSprite);
        buyImage.raycastTarget = true;
        var buy = buyImage.gameObject.AddComponent<Button>(); buy.targetGraphic = buyImage;
        var buyText = Text("Label", buyImage.transform, "강화", Vector2.zero, new Vector2(218, 65), 36);
        return new ShopEquipmentPanel.UpgradeRow { Icon = icon, Title = title, Level = level, Effect = effect, Cost = cost, Buy = buy, BuyText = buyText };
    }
    private static void AddSlots(List<ShopEquipmentPanel.EquipmentSlot> result, Transform parent, SerializedProperty items, bool pickaxe) {
        int count = 0;
        for (int i = 0; i < items.arraySize && count < 3; i++) {
            var item = items.GetArrayElementAtIndex(i).objectReferenceValue as WeaponData;
            if (item == null) continue;
            float y = pickaxe ? -100 : 110;
            var image = Image(item.name + "Slot", parent, new Vector2(-185 + count * 225, y), new Vector2(172, 172), slotSprite);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var icon = Image("Icon", image.transform, Vector2.zero, new Vector2(132, 132), item.InventoryIcon); icon.preserveAspect = true;
            var selected = Image("SelectedOutline", image.transform, Vector2.zero, new Vector2(172, 172), outlineSprite);
            selected.enabled = count == 0;
            var state = Text("State", image.transform, count == 0 ? "선택" : "", new Vector2(0, -59), new Vector2(150, 34), 22);
            state.color = new Color(.4f, 1f, .95f);
            result.Add(new ShopEquipmentPanel.EquipmentSlot { Item = item, Button = button, Icon = icon, Selected = selected, State = state });
            count++;
        }
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size) {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create equipment shop UI");
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite) {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
        if (sprite != null && sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
        return image;
    }
    private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize) {
        var rect = Rect(name, parent, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = fontSize; text.enableAutoSizing = false;
        text.text = value; text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Truncate; text.raycastTarget = false;
        return text;
    }
}
