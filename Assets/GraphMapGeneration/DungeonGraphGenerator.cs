using System.Collections.Generic;
using UnityEngine;

public class GridWalkDungeonGenerator
{
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    public List<RoomNode> GenerateGridDungeon(int targetRoomCount)
    {
        List<RoomNode> allNodes = new List<RoomNode>();
        occupiedCells.Clear();

        // 1. Start-Raum im Zentrum (0,0) platzieren
        Vector2Int currentGridPos = Vector2Int.zero;
        RoomNode startRoom = new RoomNode("Start_Room", RoomType.Start, new Vector2Int(3, 3));
        startRoom.gridPosition = currentGridPos; // Position speichern!

        MarkAreaAsOccupied(currentGridPos, startRoom.size);
        allNodes.Add(startRoom);

        RoomNode lastRoom = startRoom;

        // 2. Der Grid-Walk
        int attempts = 0;
        while (allNodes.Count < targetRoomCount && attempts < 1000)
        {
            attempts++;

            // Richtungen auf dem Raster (nur exakte 90-Grad-Schritte, keine Diagonalen)
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            Vector2Int randomDir = directions[Random.Range(0, directions.Length)];

            // Abstandhalter, damit zwischen Räumen Platz für Gänge bleibt (z.B. Raumgröße + 1)
            Vector2Int nextGridPos = currentGridPos + (randomDir * 5); 

            Vector2Int roomSize = new Vector2Int(Random.Range(2, 5), Random.Range(2, 5));

            if (IsAreaFree(nextGridPos, roomSize))
            {
                RoomNode newRoom = new RoomNode("Room_" + allNodes.Count, RoomType.Normal, roomSize);
                newRoom.gridPosition = nextGridPos; // Position speichern!

                // Verbinden
                lastRoom.ConnectTo(newRoom);

                MarkAreaAsOccupied(nextGridPos, roomSize);
                allNodes.Add(newRoom);

                currentGridPos = nextGridPos;
                lastRoom = newRoom;
            }
            else
            {
                // Wenn blockiert, springen wir zu einem zufälligen bestehenden Raum zurück für Verzweigungen
                if (allNodes.Count > 0)
                {
                    lastRoom = allNodes[Random.Range(0, allNodes.Count)];
                    currentGridPos = lastRoom.gridPosition; // Von dort aus weitermachen
                }
            }
        }

        // Boss-Raum bestimmen (der am weitesten vom Start weg ist)
        ApplyBossCondition(allNodes, startRoom);

        Debug.Log($"Grid-Walk beendet! Räume platziert: {allNodes.Count}");
        return allNodes;
    }

    private bool IsAreaFree(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (occupiedCells.Contains(pos + new Vector2Int(x, y))) return false;
            }
        }
        return true;
    }

    private void MarkAreaAsOccupied(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                occupiedCells.Add(pos + new Vector2Int(x, y));
            }
        }
    }

    private void ApplyBossCondition(List<RoomNode> nodes, RoomNode startNode)
    {
        // Einfache Breitensuche für den Boss
        Queue<RoomNode> queue = new Queue<RoomNode>();
        foreach (var node in nodes) node.distanceFromStart = -1;

        startNode.distanceFromStart = 0;
        queue.Enqueue(startNode);
        RoomNode farthestNode = startNode;

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            if (current.distanceFromStart > farthestNode.distanceFromStart) farthestNode = current;

            foreach (var neighbor in current.connectedRooms)
            {
                if (neighbor.distanceFromStart == -1)
                {
                    neighbor.distanceFromStart = current.distanceFromStart + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        farthestNode.type = RoomType.Boss;
        farthestNode.size = new Vector2Int(5, 5);
    }
}