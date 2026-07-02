using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RacerData
{
    public GameObject node;
    public int currentLap;
    public int lastWaypointIndex;
}

public class RaceManager : MonoBehaviour
{
    public ProceduralTrackGenerator trackGenerator;
    public int totalLaps = 3;

    private Dictionary<int, RacerData> racers = new Dictionary<int, RacerData>();
    private int totalWaypoints = 0;

    void Start()
    {
        Invoke("InitializeWaypoints", 0.5f);
    }

    private void InitializeWaypoints()
    {
        if (trackGenerator != null)
        {
            List<Transform> waypoints = trackGenerator.GetWaypoints();
            totalWaypoints = waypoints.Count;
            
            for (int i = 0; i < waypoints.Count; i++)
            {
                Waypoint wp = waypoints[i].gameObject.GetComponent<Waypoint>();
                if (wp == null) wp = waypoints[i].gameObject.AddComponent<Waypoint>();
                wp.Init(i, this);
            }
        }
    }

    public void RegisterRacer(GameObject node)
    {
        racers[node.GetInstanceID()] = new RacerData
        {
            node = node,
            currentLap = 1,
            lastWaypointIndex = -1
        };
    }

    public void OnWaypointPassed(GameObject shipNode, int waypointIndex)
    {
        int id = shipNode.GetInstanceID();
        if (!racers.ContainsKey(id)) return;

        RacerData racer = racers[id];

        if (waypointIndex > racer.lastWaypointIndex && waypointIndex <= racer.lastWaypointIndex + 2)
        {
            racer.lastWaypointIndex = waypointIndex;
            
            if (waypointIndex == totalWaypoints - 1)
            {
                racer.currentLap++;
                racer.lastWaypointIndex = -1;
                
                if (shipNode.CompareTag("Player"))
                {
                    Debug.Log($"Player started Lap {racer.currentLap}!");
                }

                if (racer.currentLap > totalLaps)
                {
                    OnRaceFinished(shipNode);
                }
            }
        }
    }

    private void OnRaceFinished(GameObject shipNode)
    {
        if (shipNode.CompareTag("Player"))
        {
            Debug.Log("Player finished the race!");
        }
        else
        {
            Debug.Log($"{shipNode.name} finished the race!");
        }
    }

    public List<GameObject> GetPositions()
    {
        var sortedRacers = racers.Values.OrderByDescending(r => r.currentLap)
                                        .ThenByDescending(r => r.lastWaypointIndex)
                                        .ToList();

        return sortedRacers.Select(r => r.node).ToList();
    }
}
