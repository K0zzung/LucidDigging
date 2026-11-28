using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSkillShop
{
    [MenuItem("Tools/Lucid Digging/Build Skill Shop %#i")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var shop = Object.FindObjectOfType<ShopPanelController>(true);
        if (shop == null) return;
        var so = new SerializedObject(shop);
        var panel = (RectTransform)so.FindProperty("panel").objectReferenceValue;
        if (panel.Find("SkillContent") != null) { Selection.activeGameObject = panel.Find("SkillContent").gameObject; return; }
        var equipment = panel.Find("EquipmentContent");
        if (equipment == null) { Debug.LogError("EquipmentContent template not found."); return; }
        var template = equipment.Find("WeaponUpgradeCard");
        var skillHUD = Object.FindObjectOfType<SkillHUDController>(true);
        var manager = Object.FindObjectOfType<PermanentUpgradeManager>(true);
        var skills = (PlayerSkillController)new SerializedObject(manager).FindProperty("playerSkillController").objectReferenceValue;
        var hud = new SerializedObject(skillHUD);
        var rootObject = new GameObject("SkillContent", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootObject, "Create skill shop");
        rootObject.layer = panel.gameObject.layer;
        var root = (RectTransform)rootObject.transform;
        root.SetParent(panel, false); root.localScale = new Vector3(.01f, .01f, 1f);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
        root.anchoredPosition = new Vector2(0, -1.1f); root.sizeDelta = new Vector2(1000, 1100);
        var types = new[] { PermanentUpgradeType.Dash, PermanentUpgradeType.Shield, PermanentUpgradeType.Shockwave };
        var names = new[] { "DASH", "SHIELD", "SHOCKWAVE" };
        var keys = new[] { "Q", "W", "E" };
        var slotNames = new[] { "dashSlot", "shieldSlot", "shockwaveSlot" };
        var offers = new ShopSkillPanel.Row[3];
        for (int i = 0; i < 3; i++) {
            // Editor-only duplication: the result is saved as normal scene objects.
            var card = Object.Instantiate(template.gameObject, root);
            Undo.RegisterCreatedObjectUndo(card, "Create skill upgrade card"); card.name = names[i] + "UpgradeCard";
            var rect = (RectTransform)card.transform; rect.anchoredPosition = new Vector2(0, 330 - i * 300); rect.sizeDelta = new Vector2(1000, 275);
            card.transform.Find("EquipmentName").GetComponent<TMP_Text>().text = names[i];
            var slot = hud.FindProperty(slotNames[i]).objectReferenceValue;
            var icon = (Image)new SerializedObject(slot).FindProperty("skillIcon").objectReferenceValue;
            card.transform.Find("IconFrame/SelectedEquipmentIcon").GetComponent<Image>().sprite = icon.sprite;
            var key = new GameObject("KeyBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(key, "Create skill key label"); key.layer = rootObject.layer;
            var kr = (RectTransform)key.transform; kr.SetParent(card.transform, false); kr.sizeDelta = new Vector2(67, 67); kr.anchoredPosition = new Vector2(-465, 104);
            key.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/UI/RunInventory/InventorySlot.png"); key.GetComponent<Image>().raycastTarget = false;
            var label = Object.Instantiate(card.transform.Find("UpgradeLevel").gameObject, key.transform);
            label.name = "Key"; var lr = (RectTransform)label.transform; lr.anchoredPosition = Vector2.zero; lr.sizeDelta = new Vector2(60, 60);
            var kt = label.GetComponent<TMP_Text>(); kt.text = keys[i]; kt.fontSize = 37;
            var effect = card.transform.Find("UpgradeEffect").GetComponent<TMP_Text>(); effect.text = ShopSkillPanel.Preview(skills, types[i]);
            var currency = card.transform.Find("PinkCurrency").GetComponent<Image>(); currency.name = "RequiredCurrency";
            if (i == 1) currency.sprite = (Sprite)so.FindProperty("yellowCurrency").objectReferenceValue;
            if (i == 2) currency.sprite = (Sprite)so.FindProperty("blueCurrency").objectReferenceValue;
            offers[i] = new ShopSkillPanel.Row {
                Type = types[i], Effect = effect,
                Level = card.transform.Find("UpgradeLevel").GetComponent<TMP_Text>(),
                Cost = card.transform.Find("Cost").GetComponent<TMP_Text>(),
                Buy = card.transform.Find("UpgradeButton").GetComponent<Button>(),
                BuyText = card.transform.Find("UpgradeButton/Label").GetComponent<TMP_Text>()
            };
        }
        var controller = Undo.AddComponent<ShopSkillPanel>(rootObject);
        controller.ConfigureEditor(shop, skills, offers); EditorUtility.SetDirty(controller);
        var content = so.FindProperty("contentRoots"); content.GetArrayElementAtIndex(2).objectReferenceValue = rootObject;
        so.ApplyModifiedProperties();
        for (int i = 0; i < 3; i++) {
            var section = content.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (section != null) { Undo.RecordObject(section, "Show skill section"); section.SetActive(i == 2); }
            var image = ((Button)so.FindProperty("tabButtons").GetArrayElementAtIndex(i).objectReferenceValue).image;
            Undo.RecordObject(image, "Highlight skill tab");
            var sprite = so.FindProperty(i == 2 ? "selectedTabSprite" : "unselectedTabSprite").objectReferenceValue as Sprite;
            if (sprite != null) image.sprite = sprite;
        }
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        EditorSceneManager.SaveScene(shop.gameObject.scene);
        Selection.activeGameObject = rootObject;
        Debug.Log("SkillContent saved in Hierarchy with existing skill icons and permanent upgrade logic.");
    }
}
