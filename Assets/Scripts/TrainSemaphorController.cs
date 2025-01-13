using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class TrainSemaphorController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Dictionary<Spline,List<RailCart>> trains = new Dictionary<Spline, List<RailCart>>();
    private Dictionary<Spline,List<Semaphor>> semaphors = new Dictionary<Spline, List<Semaphor>>();
    
    public void registerTrain(RailCart train){
        if(!trains.ContainsKey(train.currentSpline)){
            trains.Add(train.currentSpline,new List<RailCart>());
        }
        trains[train.currentSpline].Add(train);
    }

    public void registerSemaphor(Semaphor semaphor){
        if(!semaphors.ContainsKey(semaphor.getCurrentSpline())){
            semaphors.Add(semaphor.getCurrentSpline(),new List<Semaphor>());
        }
        semaphors[semaphor.getCurrentSpline()].Add(semaphor);
    }
    public void unregisterSemaphor(Semaphor semaphor){
        if(!semaphors.ContainsKey(semaphor.getCurrentSpline())){
            semaphors.Add(semaphor.getCurrentSpline(),new List<Semaphor>());
        }
        semaphors[semaphor.getCurrentSpline()].Remove(semaphor);
    }


    public void unregisterTrain(RailCart train){
        if(!trains.ContainsKey(train.currentSpline)){
            trains.Add(train.currentSpline,new List<RailCart>());
        }
        trains[train.currentSpline].Remove(train);
    }

    public bool checkAvailable(Semaphor semaphor)
    {
        Spline currentSpline = semaphor.getCurrentSpline();

        // Check if the spline is being tracked
        if (!semaphors.ContainsKey(currentSpline))
        {
            Debug.LogWarning("Spline is not registered with any semaphores.");
            return true; // If no semaphores are registered, assume it's available
        }

        // Get all semaphores for this spline
        List<Semaphor> splineSemaphores = semaphors[currentSpline];

        // Ensure the semaphore exists in the list
        if (!splineSemaphores.Contains(semaphor))
        {
            Debug.LogWarning("Semaphore is not registered on its current spline.");
            return true; // If semaphore isn't registered, assume it's available
        }

        // Find the next semaphore by comparing positions
        float semaphorPosition = semaphor.getSplinePos();
        Semaphor nextSemaphor = null;
        float minDistance = float.MaxValue;

        foreach (var other in splineSemaphores)
        {
            if (other == semaphor) continue;

            float otherPosition = other.getSplinePos();
            float distance = otherPosition >= semaphorPosition ? otherPosition - semaphorPosition : 1.0f - semaphorPosition + otherPosition; // Account for looping

            if (distance < minDistance)
            {
                minDistance = distance;
                nextSemaphor = other;
            }
        }

        if (nextSemaphor == null)
        {
            Debug.LogError("No valid next semaphore found.");
            return true; // If no next semaphore, assume it's available
        }

        // Check if any trains are between the two semaphores
        if (trains.ContainsKey(currentSpline))
        {
            List<RailCart> splineTrains = trains[currentSpline];

            foreach (RailCart train in splineTrains)
            {
                float trainPosition = train.getSplinePos(); // Assume this method gives normalized position
                float startPosition = semaphor.getSplinePos();
                float endPosition = nextSemaphor.getSplinePos();


                // Normalize positions for looping splines
                if (endPosition < startPosition){
                    endPosition += 1.0f;
                    trainPosition += 1.0f;
                } 

                if (trainPosition >= startPosition && trainPosition <= endPosition)
                {
                    Debug.Log("Train Is in Segment");
                    return false; // A train is occupying the segment
                }
            }
        }

        // No trains are between the two semaphores, so the segment is available
        return true;
    }

}
