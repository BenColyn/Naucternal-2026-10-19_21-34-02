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
        // Generator aufrufen
        GridWalkDungeonGenerator generator = new GridWalkDungeonGenerator();
        dungeonGraph = generator.GenerateGridDungeon(targetRoomCount);
    }

    private void OnDrawGizmos()
    {
        if (dungeonGraph == null) return;

        // 1. Verbindungen (Kanten) von der Mitte zum Zentrum des Nachbarraumes zeichnen
        Gizmos.color = Color.gray;
        foreach (var node in dungeonGraph)
        {
            // Berechne die Mitte von Raum A
            Vector2 centerA = new Vector2(node.gridPosition.x + node.size.x / 2f, node.gridPosition.y + node.size.y / 2f);

            foreach (var neighbor in node.connectedRooms)
            {
                // Berechne die Mitte von Raum B
                Vector2 centerB = new Vector2(neighbor.gridPosition.x + neighbor.size.x / 2f, neighbor.gridPosition.y + neighbor.size.y / 2f);

                Gizmos.DrawLine(centerA, centerB);
            }
        }

        // 2. Räume exakt zentriert als Rechtecke auf dem Raster zeichnen
        foreach (var node in dungeonGraph)
        {
            // Berechne die Mitte für die Darstellung des Cubes
            Vector2 center = new Vector2(node.gridPosition.x + node.size.x / 2f, node.gridPosition.y + node.size.y / 2f);

            switch (node.type)
            {
                case RoomType.Start: Gizmos.color = Color.green; break;
                case RoomType.Boss: Gizmos.color = Color.red; break;
                default: Gizmos.color = Color.blue; break;
            }

            // Zeichne den Raum in seiner wahren Größe, zentriert auf seiner Position
            Gizmos.DrawCube(center, new Vector3(node.size.x, node.size.y, 0.1f));
        }
    }
}