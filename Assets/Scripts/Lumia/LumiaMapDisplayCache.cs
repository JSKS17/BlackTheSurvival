using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        RunState mapDisplayState;
        int mapDisplayAct = -1;
        MapDisplayNode[] mapDisplayTopology = new MapDisplayNode[0];
        MapNode[] mapDisplayNodes = new MapNode[0];
        MapDisplayConnection[] mapDisplayConnections = new MapDisplayConnection[0];

        struct MapDisplayNode
        {
            public MapNode node;
            public int id, act, row, lane;
        }

        struct MapDisplayConnection
        {
            public MapNode from, to;
        }

        MapNode[] MapDisplayNodes(RunState state)
        {
            var map = state.map;
            bool same = ReferenceEquals(mapDisplayState, state) && mapDisplayAct == state.act && mapDisplayTopology.Length == map.Count;
            for (int i = 0; same && i < map.Count; i++)
            {
                var previous = mapDisplayTopology[i]; var node = map[i];
                same = ReferenceEquals(previous.node, node) && previous.id == node.id && previous.act == node.act
                    && previous.row == node.row && previous.lane == node.lane;
            }
            if (same) return mapDisplayNodes;
            mapDisplayState = state; mapDisplayAct = state.act;
            mapDisplayTopology = new MapDisplayNode[map.Count];
            var nodes = new List<MapNode>();
            for (int i = 0; i < map.Count; i++)
            {
                var node = map[i];
                mapDisplayTopology[i] = new MapDisplayNode { node = node, id = node.id, act = node.act, row = node.row, lane = node.lane };
                if (node.act == state.act) nodes.Add(node);
            }
            mapDisplayNodes = nodes.ToArray();
            var connections = new List<MapDisplayConnection>();
            foreach (var node in mapDisplayNodes)
                foreach (var next in mapDisplayNodes)
                    if (next.row == node.row + 1 && Math.Abs(next.lane - node.lane) <= 1)
                        connections.Add(new MapDisplayConnection { from = node, to = next });
            mapDisplayConnections = connections.ToArray();
            return mapDisplayNodes;
        }

        static bool MapDisplayVisible(Rect bounds, Vector2 position)
        {
            return bounds.xMax >= position.x && bounds.xMin <= position.x + 843
                && bounds.yMax >= position.y && bounds.yMin <= position.y + 371;
        }

        static bool ScrollRowVisible(float y, float height, float scrollY, float viewportHeight)
        {
            return y + height >= scrollY && y <= scrollY + viewportHeight;
        }
    }
}
