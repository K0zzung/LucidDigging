using UnityEngine;
public enum EnemyCombatCategory { Monster, Turret }
// Applies only to ordinary enemy attacks. Terrain and skills retain their existing paths.
public static class EquipmentCombat {
    public static int Attack(int baseAttack,int runBonus,int runPenalty,float efficiency) {
        return Mathf.Max(1,Mathf.RoundToInt(Mathf.Max(1,baseAttack+runBonus-runPenalty)*Mathf.Max(0,efficiency)));
    }
}
