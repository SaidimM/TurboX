using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public int index = 0;
    private RaceManager raceManager;

    public void Init(int index, RaceManager raceManager)
    {
        this.index = index;
        this.raceManager = raceManager;
    }

    void OnTriggerEnter(Collider other)
    {
        if (raceManager != null)
        {
            raceManager.OnWaypointPassed(other.gameObject, index);
        }
    }
}
