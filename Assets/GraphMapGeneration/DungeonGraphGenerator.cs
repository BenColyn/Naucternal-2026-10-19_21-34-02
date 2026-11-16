using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridWalkDungeonGenerator
{
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    public List<RoomNode> GenerateGridDungeon(int targetRoomCount)
    {
        List<RoomNode> allNodes = new List<RoomNode>();
        occupiedCells.Clear();
        List<RoomNode> beginRoom = new List<RoomNode>();

        // 1. Start-Raum im Zentrum (0,0) platzieren
        Vector2 currentGridPos = Vector2.zero; // Als Vector2 für präzise Mitten
        RoomNode startRoom = new RoomNode("Start_Room", RoomType.Start, new Vector2Int(3, 3));
        startRoom.gridPosition = currentGridPos;

        MarkAreaAsOccupied(currentGridPos, startRoom.size);
        allNodes.Add(startRoom);
        beginRoom.Add(startRoom);

        

        // 2. Der Grid-Walk
        int attempts = 0;
        while (allNodes.Count < targetRoomCount && attempts < 1000 && beginRoom.Count > 0)
        {
            attempts++;

            RoomNode activeRoom = beginRoom[Random.Range(0, beginRoom.Count-1)];

            Vector2[] directions = { Vector2.down, Vector2.left, Vector2.right, };
            Vector2 randomDir = directions[Random.Range(0, directions.Length)];

            // Raumgröße direkt ohne Multiplikation bestimmen
            Vector2Int roomSize = new Vector2Int(Random.Range(2, 5), Random.Range(2, 5));

            currentGridPos = activeRoom.gridPosition;

            // Abstand von Mitte zu Mitte berechnen (Hälfte von Raum A + Hälfte von Raum B + Gang-Puffer)
            float distanceX = (activeRoom.size.x / 2f) + (roomSize.x / 2f) + 2f;
            float distanceY = (activeRoom.size.y / 2f) + (roomSize.y / 2f) + 2f;

            float stepDist = (randomDir.x != 0) ? distanceX : distanceY;
            Vector2 nextGridPos = currentGridPos + (randomDir * stepDist);

            if (IsAreaFree(nextGridPos, roomSize))
            {
                 
                RoomNode newRoom = new RoomNode("Room_" + allNodes.Count, RoomType.Normal, roomSize);
                newRoom.gridPosition = nextGridPos;

                activeRoom.ConnectTo(newRoom);

                MarkAreaAsOccupied(nextGridPos, roomSize);
                allNodes.Add(newRoom);
                beginRoom.Add(newRoom);
               
                currentGridPos = nextGridPos;
                
            }
            else
            {

                if (IsAreaFree(currentGridPos + directions[0] * distanceY, roomSize)|| IsAreaFree(currentGridPos + directions[1] * distanceX, roomSize) || IsAreaFree(currentGridPos + directions[2] * distanceX, roomSize)){
                     
                }
                else
                beginRoom.Remove(activeRoom);

             
            }
        }
        foreach (var node in allNodes)
        {
            foreach (var neighbor in allNodes)
            {
                // 1. Sinnvolle Reichweite (z.B. 35 Einheiten) statt 1000, um Performance und Unsinn zu sparen
                if (node != neighbor && Vector2.Distance(node.gridPosition, neighbor.gridPosition) < 35f && !node.connectedRooms.Contains(neighbor))
                {
                    // 2. Prüfen, ob sie perfekt auf einer Achse liegen (mit kleiner Toleranz für unterschiedliche Raumgrößen)
                    bool isSameX = Mathf.Abs(node.gridPosition.x - neighbor.gridPosition.x) < 0.5f;
                    bool isSameY = Mathf.Abs(node.gridPosition.y - neighbor.gridPosition.y) < 0.5f;

                    if (isSameX || isSameY)
                    {
                        float distance = Vector2.Distance(node.gridPosition, neighbor.gridPosition);
                        bool canConnect = true;

                        // 3. Exakter, sauberer Schritt-Check auf der Achse
                        for (float i = 2f; i <= distance - 2f; i += 1f)
                        {
                            // Wir erzwingen, dass sich der Check rein auf der geraden Achse bewegt, keine Diagonalen-Rutscher!
                            Vector2 checkPos = node.gridPosition;
                            if (isSameX)
                                checkPos.y += Mathf.Sign(neighbor.gridPosition.y - node.gridPosition.y) * i;
                            else
                                checkPos.x += Mathf.Sign(neighbor.gridPosition.x - node.gridPosition.x) * i;

                            // Da Gänge meist dünn sind, reicht ein Punkt-Check oder ein sehr kleiner Bereich, 
                            // aber wir prüfen gegen die bereits belegten Zellen (occupiedCells)
                            Vector2Int cellCoord = new Vector2Int(Mathf.RoundToInt(checkPos.x), Mathf.RoundToInt(checkPos.y));

                            if (occupiedCells.Contains(cellCoord))
                            {
                                canConnect = false;
                                break;
                            }
                        }

                        if (canConnect)
                        {
                            node.ConnectTo(neighbor);
                        }
                    }
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