using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageCalculator
{
    private const float DefenseConstant = 100f;

    public static int CalculateDamage(
        int attack,
        int defense) {
        if (attack <= 0)
            return 0;

        float damage =
            attack *
            (DefenseConstant /
            (DefenseConstant + Mathf.Max(0, defense)));

        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }
}
