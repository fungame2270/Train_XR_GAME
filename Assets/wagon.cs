using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Collections;
using Unity.VisualScripting;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction;

[RequireComponent(typeof(Rigidbody))]
public class Wagon : MonoBehaviour
{
    public SplineContainer container;

    public Spline currentSpline;
    private Rigidbody rb;
    private float speed;
    public float maxSpeed; // Set your desired max speed
    private float acceleration = 1f; // Adjust this value
    private float deceleration = 1f; // Adjust this value

    public factoryHit lastStop;

    private float splinePosition = 0f;

    private GrabInteractable grabInteractable;

    public GameObject childInteract;

    public GameObject frontWagon = null;
    public GameObject BackWagon = null;

    private bool trainCollider = true;

    private bool grabbed = false;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = childInteract.GetComponent<GrabInteractable>();
        Debug.Log("Created Wagon");
        Debug.Log(rb);
        speed = 0f;
    }

    public void setSpeed(float receivedSpeed){
        speed = receivedSpeed;
        if(BackWagon != null){
            Wagon back = BackWagon.GetComponent<Wagon>();
            back.setSpeed(receivedSpeed);
        }
    }

    public void setSplinePosition(float t){
        float fixedDistance = 0.2f;
        if(currentSpline == null){
            return;
        }
        float splineLength = currentSpline.GetLength();
        float currentDistance = t * splineLength;
        float secondWagonDistance = currentDistance - fixedDistance;
        secondWagonDistance = Mathf.Clamp(secondWagonDistance, 0f, splineLength);
        splinePosition = secondWagonDistance / splineLength;

    }
    
    public void disableTrainColider(){
        trainCollider = false;
    }

    public bool getTrainColider(){
        return trainCollider;
    }

    private void FixedUpdate()
    {   
        if(grabInteractable != null && grabInteractable.State == InteractableState.Select)
        {
            trainCollider = false;
            grabbed = true;
            currentSpline = null;
            setSpeed(0);
            RailCart rail = frontWagon.GetComponent<RailCart>();
            rail.backWagon = null;
            Debug.Log($"Change Spline of wagon{currentSpline}");
            return;
        }

        if(grabbed == true)
        {
            trainCollider = true;
            findClosestSpline();
            grabbed = false;
        }

        if (currentSpline == null) return;

        // Ensure speed stays within valid bounds
        speed = Mathf.Clamp(speed, 0f, maxSpeed);

        // Update the spline position based on speed
        splinePosition += speed * Time.deltaTime / currentSpline.GetLength();

        // Wrap the spline position to stay within bounds
        if (splinePosition > 1f) splinePosition -= 1f;

        // Get the position and tangent along the spline
        Vector3 position = currentSpline.EvaluatePosition(splinePosition);
        Vector3 tangent = currentSpline.EvaluateTangent(splinePosition);
        Vector3 up = currentSpline.EvaluateUpVector(splinePosition);

        // Lerp the train position for smooth motion
        transform.position = Vector3.Lerp(transform.position, position, 0.5f);

        // Align the train with the tangent
        transform.rotation = Quaternion.LookRotation(tangent, up);

        // Set Rigidbody velocity in the direction of movement
        rb.linearVelocity = tangent.normalized * speed; 
    }

    private void findClosestSpline()
    {
        if (container == null || transform == null)
        {
            return;
        }

        float closestDistance = float.MaxValue;
        Spline closestSpline = null;

        // Iterate through all splines in the container
        foreach (var spline in container.Splines)
        {
            float distance = FindClosestDistanceOnSpline(spline, transform.position,out float exit_t);

            if (distance < 0.1 && distance < closestDistance)
            {
                closestDistance = distance;
                closestSpline = spline;
                splinePosition = exit_t;
            }
        }

        if (closestSpline != null)
        {
            currentSpline = closestSpline;
            Debug.Log($"find Closest Spline at{closestDistance}");
        }
    }

    private float FindClosestDistanceOnSpline(Spline spline, Vector3 targetPosition,out float exit_t)
    {
        exit_t = 0f;
        float stepSize = 0.1f;

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

    public IEnumerator CallFunctionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified time
        speed = maxSpeed;
    }
}