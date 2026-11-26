using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridPickupManager : MonoBehaviour
{
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Transform pickupRoot;

    private readonly Dictionary<Vector3Int, GridPickup>
        pickups = new Dictionary<Vector3Int, GridPickup>();


    public bool HasPickup(Vector3Int cell) {
        return pickups.ContainsKey(cell);
    }


    public bool SpawnPickup(
        GridPickup prefab,
        Vector3Int cell) {
        if (prefab == null)
            return false;

        if (HasPickup(cell))
            return false;

        Vector3 position =
            terrainTilemap.GetCellCenterWorld(cell);

        GridPickup pickup =
            Instantiate(
                prefab,
                position,
                Quaternion.identity,
                pickupRoot);

        pickup.Initialize(
            this,
            cell);

        pickups.Add(
            cell,
            pickup);

        return true;
    }


    public void TryCollect(
     Vector3Int cell,
     Transform collector) {
        if (!pickups.TryGetValue(
            cell,
            out GridPickup pickup)) {
            return;
        }


        bool collected =
            pickup.TryCollect(
                collector);


        // ========================
        // È¹µæ ¼º°øÇßÀ» ¶§¸¸ Á¦°Å
        // ========================

        if (collected) {
            pickups.Remove(cell);
        }
    }
}