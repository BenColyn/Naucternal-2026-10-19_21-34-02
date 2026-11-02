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
        Vector2 currentGridPos = Vector2.zero; // Als Vector2 für präzise Mitten
        RoomNode startRoom = new RoomNode("Start_Room", RoomType.Start, new Vector2Int(3, 3));
        startRoom.gridPosition = currentGridPos;

        MarkAreaAsOccupied(currentGridPos, startRoom.size);
        allNodes.Add(startRoom);

        RoomNode lastRoom = startRoom;

        // 2. Der Grid-Walk
        int attempts = 0;
        while (allNodes.Count < targetRoomCount && attempts < 1000)
        {
            attempts++;

            Vector2[] directions = { Vector2.down, Vector2.left, Vector2.right, };
            Vector2 randomDir = directions[Random.Range(0, directions.Length)];

            // Raumgröße direkt ohne Multiplikation bestimmen
            Vector2Int roomSize = new Vector2Int(Random.Range(2, 5), Random.Range(2, 5));

            // Abstand von Mitte zu Mitte berechnen (Hälfte von Raum A + Hälfte von Raum B + Gang-Puffer)
            float distanceX = (lastRoom.size.x / 2f) + (roomSize.x / 2f) + 2f;
            float distanceY = (lastRoom.size.y / 2f) + (roomSize.y / 2f) + 2f;

            float stepDist = (randomDir.x != 0) ? distanceX : distanceY;
            Vector2 nextGridPos = currentGridPos + (randomDir * stepDist);

            if (IsAreaFree(nextGridPos, roomSize))
            {
                RoomNode newRoom = new RoomNode("Room_" + allNodes.Count, RoomType.Normal, roomSize);
                newRoom.gridPosition = nextGridPos;

                lastRoom.ConnectTo(newRoom);

                MarkAreaAsOccupied(nextGridPos, roomSize);
                allNodes.Add(newRoom);

                currentGridPos = nextGridPos;
                lastRoom = newRoom;
            }
            else
            {
                if (allNodes.Count > 0)
                {
                    lastRoom = allNodes[Random.Range(0, allNodes.Count)];
                    currentGridPos = lastRoom.gridPosition;
                }
            }
        }

        ApplyBossCondition(allNodes, startRoom);
        Debug.Log($"Grid-Walk beendet! Räume platziert: {allNodes.Count}");
        return allNodes;
    }

    private bool IsAreaFree(Vector2 centerPos, Vector2Int size)
    {
        // Von der echten Float-Mitte zur unteren linken Ecke runden/konvertieren für das HashSet
        Vector2 lowerLeftFloat = centerPos - new Vector2(size.x / 2f, size.y / 2f);
        Vector2Int lowerLeft = new Vector2Int(Mathf.RoundToInt(lowerLeftFloat.x), Mathf.RoundToInt(lowerLeftFloat.y));

        for (int x = 0; x < size.x+1; x++)
        {
            for (int y = 0; y < size.y+1; y++)
            {
                if (occupiedCells.Contains(lowerLeft + new Vector2Int(x, y))) return false;
            }
        }
        return true;
    }

    private void MarkAreaAsOccupied(Vector2 centerPos, Vector2Int size)
    {
        Vector2 lowerLeftFloat = centerPos - new Vector2(size.x / 2f, size.y / 2f);
        Vector2Int lowerLeft = new Vector2Int(Mathf.RoundToInt(lowerLeftFloat.x), Mathf.RoundToInt(lowerLeftFloat.y));

        for (int x = 0; x < size.x+1; x++)
        {
            for (int y = 0; y < size.y+1; y++)
            {
                occupiedCells.Add(lowerLeft + new Vector2Int(x, y));
            }
        }
    }

    private void ApplyBossCondition(List<RoomNode> nodes, RoomNode startNode)
    {
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