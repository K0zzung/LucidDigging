using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConnectShopAndMusic
{
    [MenuItem("Tools/Lucid Digging/Connect Shop and Music %#m")]
    public static void Apply() {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var audio=Object.FindObjectOfType<AudioManager>(true);
        if(audio!=null) {
            Undo.RecordObject(audio,"Assign Lucid Digging music");
            var so=new SerializedObject(audio);
            so.FindProperty("gameplayBGM").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Lucid_Digging_MainTheme.mp3");
            so.FindProperty("hadesBattleBGM").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Hades_Battle.mp3");
            so.ApplyModifiedProperties();EditorUtility.SetDirty(audio);
            var source=so.FindProperty("bgmSource").objectReferenceValue as AudioSource;
            if(source!=null) {
                Undo.RecordObject(source,"Set default music source");
                source.clip=(AudioClip)so.FindProperty("gameplayBGM").objectReferenceValue;source.loop=true;
                EditorUtility.SetDirty(source);
            }
        }
        var equipment=Object.FindObjectOfType<ShopEquipmentPanel>(true);
        var manager=Object.FindObjectOfType<PermanentUpgradeManager>(true);
        if(equipment!=null && manager!=null) {
            var so=new SerializedObject(equipment);
            foreach(var name in new[]{"weaponRow","pickaxeRow"}) {
                var row=so.FindProperty(name);
                var icon=row.FindPropertyRelative("Icon").objectReferenceValue as UnityEngine.UI.Image;
                WeaponData item=null;
                var slots=so.FindProperty("slots");
                for(int i=0;i<slots.arraySize;i++) {
                    var candidate=slots.GetArrayElementAtIndex(i).FindPropertyRelative("Item").objectReferenceValue as WeaponData;
                    if(candidate!=null && candidate.InventoryIcon==icon.sprite){item=candidate;break;}
                }
                if(item==null)continue;
                int basic=manager.GetEquipmentBaseAttack(item),bonus=manager.GetEquipmentAttackBonus(item);
                var effect=(TMP_Text)row.FindPropertyRelative("Effect").objectReferenceValue;
                Undo.RecordObject(effect,"Show equipment ATK breakdown");
                effect.text=(item.IsPickaxe?"터렛 특화":"몬스터·보스 특화")+$"\nATK : +{(long)basic+bonus} ({basic} + <color=#FFF270>{bonus}</color>)";
                EditorUtility.SetDirty(effect);
            }
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("Shop ATK display and MainTheme/Hades_Battle references saved.");
    }
}
