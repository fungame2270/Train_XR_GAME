using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Splines;

public class TrainSemaphorController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Dictionary<Spline,SortedList<float,Semaphor>> semaphors = new Dictionary<Spline, SortedList<float,Semaphor>>();
    private Dictionary<Semaphor,List<RailCart>> Sections = new Dictionary<Semaphor, List<RailCart>>();

    private int getPreviousSemaphoreIndex(float number,Spline spline){
        List<float> keys = new List<float>(semaphors[spline].Keys);

        int index = keys.BinarySearch(number);

        if (index < 0)
        {
            index = ~index;
            index = (index == 0) ? semaphors[spline].Count - 1 : index - 1;
        }

        return index;
    }
    
    public void registerTrain(RailCart train){
        if(!semaphors.Any() || !semaphors[train.currentSpline].Any()) return;
        List<Semaphor> localSemaphores = new List<Semaphor>(semaphors[train.currentSpline].Values);
        if(!localSemaphores.Any()) return;
        
        int index = getPreviousSemaphoreIndex(train.getSplinePos(),train.currentSpline);
        Semaphor previous = localSemaphores[index];

        Sections[previous].Add(train);
        train.setLastSemaphore(previous);
    }

    public void registerSemaphor(Semaphor semaphor){
        List<RailCart> toAdd;
        if(semaphors.Any()){
            int index = getPreviousSemaphoreIndex(semaphor.getSplinePos(),semaphor.getCurrentSpline());
            List<Semaphor> localSemaphores = new List<Semaphor>(semaphors[semaphor.getCurrentSpline()].Values);
            Semaphor previous = localSemaphores[index];
            toAdd = new List<RailCart>();
            foreach(var train in Sections[previous]){
                train.setLastSemaphore(semaphor);
                toAdd.Add(train);
            }
            Sections[previous] = new List<RailCart>();
        }else{
            toAdd = new List<RailCart>();
        }
        if(!semaphors.ContainsKey(semaphor.getCurrentSpline())){
            semaphors.Add(semaphor.getCurrentSpline(),new SortedList<float, Semaphor>());
        }
        semaphors[semaphor.getCurrentSpline()].Add(semaphor.getSplinePos(),semaphor);
        Sections.Add(semaphor,toAdd);
    }
    public void unregisterSemaphor(Semaphor semaphor){
        if(semaphors[semaphor.getCurrentSpline()].Count > 1){
            int index = getPreviousSemaphoreIndex(semaphor.getSplinePos(),semaphor.getCurrentSpline());
            List<Semaphor> localSemaphores = new List<Semaphor>(semaphors[semaphor.getCurrentSpline()].Values);
            Semaphor prevSemaphor = localSemaphores[index];
            List<RailCart> newList = new List<RailCart>();
            foreach(var train in Sections[semaphor]){
                newList.Add(train);
                train.setLastSemaphore(prevSemaphor);
            }
            Sections[prevSemaphor] = newList;
        }
        if(!semaphors.ContainsKey(semaphor.getCurrentSpline())){
            semaphors.Add(semaphor.getCurrentSpline(),new SortedList<float, Semaphor>());
        }
        semaphors[semaphor.getCurrentSpline()].Remove(semaphor.getSplinePos());
        Sections.Remove(semaphor);
    }


    public void unregisterTrain(RailCart train){
        Semaphor lastSemaphore = train.getLastSemaphore();
        train.setLastSemaphore(null);
        if (lastSemaphore == null || !Sections.ContainsKey(lastSemaphore)) return;
        Sections[lastSemaphore].Remove(train);
    }

    public void trainUpdate(Semaphor semaphorHit,RailCart train){
        Debug.Log($"Train:{train}");
        Debug.Log($"TrainLastSemaphor:{train.getLastSemaphore()}");
        Debug.Log($"SemaphoreHit:{semaphorHit}");
        if(train.getLastSemaphore() == null || !Sections.ContainsKey(train.getLastSemaphore())) return;
        Sections[train.getLastSemaphore()].Remove(train);
        Sections[semaphorHit].Add(train);
        train.setLastSemaphore(semaphorHit);
    }

    public bool checkAvailable(Semaphor semaphor)
    {
        return !Sections[semaphor].Any();
    }

}
