using UnityEngine;
public enum EnemyCombatCategory { Monster, Turret }
// Applies only to ordinary enemy attacks. Terrain and skills retain their existing paths.
public static class EquipmentCombat {
    public static int Attack(int baseAttack,int runBonus,int runPenalty,float efficiency) {
        double attack=System.Math.Max(1L,(long)baseAttack+runBonus-runPenalty)*(double)Mathf.Max(0,efficiency);
        return attack>=int.MaxValue?int.MaxValue:Mathf.Max(1,Mathf.RoundToInt((float)attack));
    }
}
