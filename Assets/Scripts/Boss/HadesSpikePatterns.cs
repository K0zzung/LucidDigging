using System;
using System.Collections.Generic;
using UnityEngine;

public enum HadesSpikePattern { Line, Fork, Sweep, DelayedHunt, SafeLane, AcceleratingHunt, AimedWave, Horizontal }

// Geometry only, shared by runtime and managed tests. No damage or scene ownership.
public static class HadesSpikePatterns
{
    private static readonly Vector3Int[] escapeSteps = {
        new Vector3Int(-1,0,0),new Vector3Int(1,0,0),new Vector3Int(0,-1,0),
        new Vector3Int(-1,-1,0),new Vector3Int(1,-1,0)
    };
    public static bool IsAvailable(HadesSpikePattern kind, float hpRatio) => kind == HadesSpikePattern.Horizontal || kind == HadesSpikePattern.AimedWave || kind == HadesSpikePattern.AcceleratingHunt || (int)kind < PatternCount(hpRatio);
    public static int PatternCount(float hpRatio) => hpRatio > 0.7f ? 2 : hpRatio > 0.35f ? 4 : 5;
    public static void Build(HadesSpikePattern kind, int left, int width, int baseY, int range,
        Vector3Int target, bool reverse, List<Vector3Int> result) {
        result.Clear();
        int x = Mathf.Clamp(target.x, left, left + width - 1);
        int row = Mathf.Clamp(target.y, baseY, baseY + range - 1);
        if (kind == HadesSpikePattern.Line || kind == HadesSpikePattern.DelayedHunt) {
            result.Add(new Vector3Int(x,row,0));
        } else if (kind == HadesSpikePattern.Fork) {
            // Alternate occupied/empty columns, all rising in the same frame.
            int parity = reverse ? 1 : 0;
            for (int i = 0; i < width; i++)
                if (i % 2 == parity) result.Add(new Vector3Int(left+i,row,0));
        } else if (kind == HadesSpikePattern.Sweep) {
            for (int i = 0; i < width; i++) result.Add(new Vector3Int(left+(reverse?width-1-i:i),row,0));
        } else {
            int lane = Mathf.Clamp(x + (reverse ? -1 : 1), left, left + width - 1);
            for (int i = 0; i < width; i++)
                if (left+i != lane) result.Add(new Vector3Int(left+i,row,0));
        }
    }
    // Complement of the actual first wave, on the same row even if the player moves.
    public static void BuildComplement(int left,int width,int row,List<Vector3Int> first,List<Vector3Int> result) {
        result.Clear();
        for(int i=0;i<width;i++) {
            var cell=new Vector3Int(left+i,row,0);
            if(!first.Contains(cell))result.Add(cell);
        }
    }
    public static void BuildHorizontal(int left,int width,int row,List<Vector3Int> bases,List<Vector3Int> danger) {
        bases.Clear();danger.Clear();
        if(width<7)return; // Keep a central escape lane between the two 3-cell attacks.
        bases.Add(new Vector3Int(left,row,0));
        bases.Add(new Vector3Int(left+width-1,row,0));
        for(int i=0;i<3;i++) {
            danger.Add(new Vector3Int(left+i,row,0));
            danger.Add(new Vector3Int(left+width-1-i,row,0));
        }
    }
    public static bool HasEscape(Vector3Int from, List<Vector3Int> danger, Func<Vector3Int,bool> free) {
        if (!danger.Contains(from)) return true; // Staying still is a valid no-damage choice.
        foreach (Vector3Int step in escapeSteps) {
            Vector3Int next = from + step;
            if (!danger.Contains(next) && free(next)) return true;
        }
        return false;
    }
    public static bool ReserveEscape(Vector3Int from, List<Vector3Int> danger, Func<Vector3Int,bool> free) {
        if (HasEscape(from,danger,free)) return true;
        foreach (Vector3Int step in escapeSteps) {
            Vector3Int next = from + step;
            if (!free(next)) continue;
            danger.Remove(next);
            return true;
        }
        return false;
    }
}
