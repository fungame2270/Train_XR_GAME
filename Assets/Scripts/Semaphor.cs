using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Splines;

public class Semaphor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GrabInteractable grabInteractable;

    private bool grabbed;

    private SplineContainer container;

    private Spline currentSpline;

    private float splinePosition;

    private TrainSemaphorController trainSemaphorController;
    
    void Start()
    {
        container = FindAnyObjectByType<SplineContainer>();
        currentSpline = null;
        splinePosition = 0f;
        trainSemaphorController = FindFirstObjectByType<TrainSemaphorController>();
    }

    // Update is called once per frame
    void Update(){
         if(grabInteractable != null && grabInteractable.State == InteractableState.Select){
            if(grabbed) return;
            if(currentSpline != null){
                trainSemaphorController.unregisterSemaphor(this);
            }
            currentSpline = null;
            grabbed = true;
            return;
        }

        if(grabbed == true){
            findClosestSpline();
            trainSemaphorController.registerSemaphor(this);
            grabbed = false;

            Vector3 position = currentSpline.EvaluatePosition(splinePosition);
            Vector3 tangent = currentSpline.EvaluateTangent(splinePosition);
            Vector3 up = currentSpline.EvaluateUpVector(splinePosition);

            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent, up);
        }
    }

    private void findClosestSpline()
    {
        if (container == null || transform == null)
        {
            Debug.LogError("SplineContainer or target Transform is not assigned.");
            return;
        }

        float closestDistance = float.MaxValue;
        Spline closestSpline = null;

        // Iterate through all splines in the container
        foreach (var spline in container.Splines)
        {
            float distance = FindClosestDistanceOnSpline(spline, transform.position,out float exit_t);
            Debug.Log($"distance:{distance}");

            if (distance < 0.1 && distance < closestDistance)
            {
                closestDistance = distance;
                closestSpline = spline;
                splinePosition = exit_t;
            }
        }

        if (closestSpline != null)
        {
            Debug.Log($"Closest Spline Found! Closest Distance: {closestDistance}");
            currentSpline = closestSpline;
        }
        else
        {
            Debug.Log("No splines found in the container.");
        }
    }

    private float FindClosestDistanceOnSpline(Spline spline, Vector3 targetPosition,out float exit_t)
    {
        exit_t = 0f;
        float stepSize = 0.05f;

        float closestDistance = float.MaxValue;

        float splineLength = spline.GetLength();

        int numberOfSteps = Mathf.CeilToInt(splineLength / stepSize);

        // Sample points along the spline
        for (int i = 0; i <= numberOfSteps; i++)
        {
            float t = (float)i / numberOfSteps; // Normalized parameter (0 to 1)
            Vector3 sampledPosition = spline.EvaluatePosition(t); // Evaluate the position at 't'
            float distance = Vector3.Distance(targetPosition, sampledPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                exit_t = t;
            }
        }

        return closestDistance;
    }

    public float getSplinePos(){
        return splinePosition;
    }

    public Spline getCurrentSpline(){
        return currentSpline;
    }
}
