using UnityEngine;
public static class PickaxeBossReward {
    private static readonly System.Collections.Generic.HashSet<string> pending = new System.Collections.Generic.HashSet<string>();
    public static void Release(WeaponData item) {if(item!=null)pending.Remove(item.WeaponId);}
    public static bool ShouldDrop(bool owned,bool pendingDrop,float chance,float roll) {
        return !owned && !pendingDrop && chance>0f && roll<Mathf.Clamp01(chance);
    }
    public static void Spawn(WeaponData item,float chance,WeaponPickup prefab,PlayerGridController player,EnemyManager grid,Vector3 start) {
        if(item==null || !item.IsPickaxe || prefab==null || player==null || grid==null || WeaponManager.Instance==null)return;
        if(!ShouldDrop(WeaponManager.Instance.IsOwned(item),pending.Contains(item.WeaponId),chance,Random.value))return;
        if(!player.TryGetBossLandingCell(out Vector3Int cell))return;
        var position=grid.GetCellCenterWorld(cell)+Vector3.right*.2f;
        var pickup=Object.Instantiate(prefab,position,Quaternion.identity);
        pending.Add(item.WeaponId);
        pickup.Initialize(item,cell,start,player,2f*(grid.GetCellCenterWorld(cell+Vector3Int.up)-grid.GetCellCenterWorld(cell)).magnitude);
    }
}
