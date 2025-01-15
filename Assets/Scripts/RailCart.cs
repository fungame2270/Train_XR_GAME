using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Collections;
using Unity.VisualScripting;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction;
using System.Threading;

[RequireComponent(typeof(Rigidbody))]
public class RailCart : MonoBehaviour
{
    public SplineContainer container;

    public Spline currentSpline;
    private Rigidbody rb;
    private float speed = 0f;
    public float maxSpeed; // Set your desired max speed
    private float acceleration = 1f; // Adjust this value
    private float deceleration = 1f; // Adjust this value

    public factoryHit lastStop;

    private float splinePosition = 0f;

    public GrabInteractable grabInteractable;

    public GameObject backWagon = null;

    public UIController uIController;

    private bool grabbed = false;

    private Spline memoSpline;

    private float memoSplinePosition = 0f;

    public BoxCollider wagonCollider;

    private TrainSemaphorController trainSemaphorController;

    private Semaphor lastSemaphor;

    public bool running;

    public OVRInput.Button TriggerButton;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpline = null;
        memoSpline = null;
        container = FindFirstObjectByType<SplineContainer>();
        trainSemaphorController = FindFirstObjectByType<TrainSemaphorController>();
        Debug.Log("Created Train");
        Debug.Log(rb);

        
    }

    public Semaphor getLastSemaphore(){
        return lastSemaphor;
    }

    public void setLastSemaphore(Semaphor semaphor){
        lastSemaphor = semaphor;
    }

    private void saveStatus(){
        memoSpline = currentSpline;
        memoSplinePosition = splinePosition;
    }

    public float getSplinePos(){
        return splinePosition;
    }

    private void openUiIfStatus(){
        if(memoSpline != null && memoSpline == currentSpline && Math.Abs(memoSplinePosition - splinePosition) < 0.2){
            uIController.spawnTrainMenu(this);
        }
    }

    public void setWagon(GameObject wagon){
        Wagon wagonS = wagon.GetComponent<Wagon>();
        wagonS.disableTrainColider();
        if(backWagon != null){
            backWagon.GetComponent<Wagon>().setWagon(wagon);
            return;
        }
        backWagon = wagon;
        wagonS.frontWagon = this.gameObject;
        wagonS.currentSpline = currentSpline;
        wagonS.setSplinePosition(splinePosition);
        wagonS.setSpeed(speed);
    }

    private void OnTriggerStay(Collider collider){
        if(collider.name == "ControllerGrabLocation"){
            if(OVRInput.GetDown(TriggerButton,OVRInput.Controller.RTouch)){
                uIController.spawnTrainMenu(this);
            }
        }
    }

    private void FixedUpdate()
    {   
        if(grabInteractable != null && grabInteractable.State == InteractableState.Select){
            if(grabbed) return;
            saveStatus();
            wagonCollider.enabled = false;
            if(currentSpline != null){
                Debug.Log("unregesteringTrain");
                trainSemaphorController.unregisterTrain(this);
            }
            Debug.Log($"semaphore:{lastSemaphor}");
            currentSpline = null;
            if (backWagon != null){
                Wagon wagon = backWagon.GetComponent<Wagon>();
                wagon.setSpeed(0);
                backWagon = null;
            }
            grabbed = true;
            return;
        }

        if(grabbed == true){
            wagonCollider.enabled = true;
            findClosestSpline();
            //openUiIfStatus();
            trainSemaphorController.registerTrain(this);
            grabbed = false;
        }
        if (currentSpline == null || container == null) return;

        // Ensure speed stays within valid bounds
        speed = Mathf.Clamp(speed, 0f, maxSpeed);

        // Update the spline position based on speed
        splinePosition += speed * Time.deltaTime / currentSpline.GetLength();

        // Wrap the spline position to stay within bounds
        if (splinePosition > 1f)
            splinePosition -= 1f;

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

    public void setSpeed(float speed){
        if(!running){
            speed = 0;
        }
        this.speed = speed;
        if(backWagon != null){
            Wagon back = backWagon.GetComponent<Wagon>();
            back.setSpeed(speed);
        }
    }

    public float getSpeed(){
        return speed;
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
        setSpeed(maxSpeed);
    }
}