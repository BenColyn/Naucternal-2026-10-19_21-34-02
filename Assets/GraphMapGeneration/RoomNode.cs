using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Start, Normal, Treasure, Boss }

public class RoomNode
{
    public string id;
    public RoomType type;
    public Vector2Int size; // Z.B. (2,2) für klein, (4,3) für groß
    public Vector2 gridPosition; // Speichert die echte Raster-Koordinate

    // Die Kanten: Welche Räume sind direkt mit diesem verbunden?
    public List<RoomNode> connectedRooms = new List<RoomNode>();

    // Hilfsfeld für Algorithmen (z.B. Entfernung zum Start)
    public int distanceFromStart = -1;

    public RoomNode(string id, RoomType type, Vector2Int size)
    {
        this.id = id;
        this.type = type;
        this.size = size;
    }

    public void ConnectTo(RoomNode otherRoom)
    {
        if (!connectedRooms.Contains(otherRoom))
        {
            connectedRooms.Add(otherRoom);
            otherRoom.connectedRooms.Add(this); // Doppelter Weg (ungerichteter Graph)
        }
    }
}