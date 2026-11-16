using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public int targetRoomCount = 10;
    private List<RoomNode> dungeonGraph;

    void Start()
    {
        BuildAndVisualiseDungeon();
    }

    public void BuildAndVisualiseDungeon()
    {
        GridWalkDungeonGenerator generator = new GridWalkDungeonGenerator();
        dungeonGraph = generator.GenerateGridDungeon(targetRoomCount);
    }

    private void OnDrawGizmos()
    {
        if (dungeonGraph == null) return;

        // 1. Verbindungen direkt von Mitte zu Mitte zeichnen
        Gizmos.color = Color.gray;
        foreach (var node in dungeonGraph)
        {
            foreach (var neighbor in node.connectedRooms)
            {
                Gizmos.DrawLine(node.gridPosition, neighbor.gridPosition);

            }
        }

        // 2. Räume zentriert zeichnen
        foreach (var node in dungeonGraph)
        {
            switch (node.type)
            {
                case RoomType.Start: Gizmos.color = Color.green; break;
                case RoomType.Boss: Gizmos.color = Color.red; break;
                default: Gizmos.color = Color.blue; break;
            }

            Gizmos.DrawCube(node.gridPosition, new Vector3(node.size.x, node.size.y, 0.1f));
        }
    }
}