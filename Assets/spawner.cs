using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.BuildingBlocks;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using System.Linq;

public class spawner : MonoBehaviour
{
   
    public GameObject AnchorPrefab;  // Prefab to spawn

    public GameObject railPrefab;

    public GameObject trainPrefab;

    public GameObject wagonPrefab;

    private int counter = 0;

    private int wagoncounter = 0;
    public Transform rayStartPoint;  // Ray origin point
    public float maxRayLength = 5f;  // Maximum ray length

    private GameObject _spawnedAnchor;  // The instantiated anchor
    private SpatialAnchorCoreBuildingBlock _spatialAnchorCore;  // Spatial Anchor Core
    private SplineContainer container;
    private Spline currenteSpline;

    private RaycastHit hit;

    private Dictionary<Spline,List<GameObject>> railsModelList = new Dictionary<Spline, List<GameObject>>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize _spatialAnchorCore like before
        _spatialAnchorCore = FindFirstObjectByType<SpatialAnchorCoreBuildingBlock>();

        // Ensure _spatialAnchorCore is found
        if (_spatialAnchorCore == null)
        {
            Debug.LogError("_spatialAnchorCore is null. Make sure the SpatialAnchorCoreBuildingBlock is in the scene.");
            return;
        }

        // Instantiate the anchor prefab at a default position
        if (AnchorPrefab != null)
        {
            _spawnedAnchor = Instantiate(AnchorPrefab, rayStartPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("AnchorPrefab is not assigned!");
        }

        container = FindFirstObjectByType<SplineContainer>();
        currenteSpline = container.AddSpline();
        railsModelList.Add(currenteSpline,new List<GameObject>());
    }

    // Update is called once per frame
    void Update()
    {
        // Check if rayStartPoint is null
        if (rayStartPoint == null)
        {
            Debug.LogError("rayStartPoint is null!");
            return;
        }

        // Create the ray from rayStartPoint position and forward direction
        Ray ray = new Ray(rayStartPoint.position, rayStartPoint.forward);

        // Check if MRUK.Instance is null
        if (MRUK.Instance == null)
        {
            Debug.LogError("MRUK.Instance is null!");
            return;
        }

        // Get the current room from MRUK.Instance
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        // Check if the room is null
        if (room == null)
        {
            Debug.LogError("Room is null!");
            return;
        }

        // Perform the raycast
        bool hasHit = room.Raycast(ray, maxRayLength, out hit, out MRUKAnchor anchor);
        if (!hasHit){
            return;
        }
        bool snap = isCloseToFirstKnot(hit.point,0.10f,out float3 returnPosition);
        if(snap){
            hit.point = returnPosition;
        }
        // Move the spawned anchor to the hit point
        _spawnedAnchor.transform.position = hit.point;  // Move anchor to hit point
        _spawnedAnchor.transform.rotation = Quaternion.LookRotation(hit.normal);  // Align anchor rotation with surface normal
    }

    private bool isCloseToFirstKnot(float3 point,float distanceRequired,out float3 returnPosition){
        if (currenteSpline.Count < 3){
            returnPosition = new float3(0,0,0);
            return false;
        }
        returnPosition = new float3(0f,0f,0f);

        BezierKnot firstKnot = currenteSpline[0];

        float distance = math.distance(firstKnot.Position,point);

        if (distance < distanceRequired){
            returnPosition = firstKnot.Position;
            return true;
        }

        return false;
        
    }

    private void SpawnAnchor()
    {
        Vector3 upDirection = hit.normal;  // This is the direction you want the top of the model to point
        Vector3 forwardDirection = Vector3.Cross(Vector3.up, upDirection); // Cross product gives the side direction to make sure it's perpendicular

        // Adjust the rotation to align the model's top (Y-axis) to the normal direction
        Quaternion rotation = Quaternion.LookRotation(forwardDirection, upDirection);

        // Create the Bezier knot and add it to the currenteSpline
        BezierKnot knot = new BezierKnot(hit.point, 0, 0, rotation);

        currenteSpline.Add(knot);

        int knotsCount = currenteSpline.Count;
        if (knotsCount >= 2)
        {
            // Set the tangent mode to AutoSmooth for the last two knots
            currenteSpline.SetTangentMode(knotsCount - 1, TangentMode.AutoSmooth);
            currenteSpline.SetTangentMode(knotsCount - 2, TangentMode.AutoSmooth);
        }

        // Smoothly connect the last knot with the first knot
        if (currenteSpline.Count > 1)
        {
            // Access the first and last knots
            BezierKnot firstKnot = currenteSpline[0];
            BezierKnot lastKnot = currenteSpline[knotsCount - 1];

            // Check if the positions of the first and last knots are the same
            if (math.all(lastKnot.Position == firstKnot.Position))
            {
                Debug.Log("Smoothing last knot with the first");

                // Mirror the tangents between the last and first knots
                lastKnot.TangentIn = -firstKnot.TangentOut;
                lastKnot.TangentOut = -firstKnot.TangentIn;

                // Explicitly reassign the modified knot to the currenteSpline
                currenteSpline.SetKnot(knotsCount - 1, lastKnot);
                spawnObjects();
                createNewSpline();
                return;
            }
        }


        spawnObjects();
    }

    private void createNewSpline(){
        currenteSpline = container.AddSpline();
        railsModelList[currenteSpline] = new List<GameObject>();
    }

    private void spawnObjects(){
        float fixedDistance = 0.015f;

        float splineLength = currenteSpline.GetLength();

        int numberObjs = Mathf.CeilToInt(splineLength / fixedDistance);

        deleteRails();

        for(int i = 0; i <= numberObjs; i += 1){
            float distanceAlongSpline = i * fixedDistance; // Total distance to the current point
            float normalizedPosition = distanceAlongSpline / splineLength;

            SplineUtility.Evaluate(currenteSpline,normalizedPosition,out float3 position,out float3 tangent,out float3 upDirection);
            Vector3 globalPosition = container.transform.TransformPoint(position);

            Quaternion rotation = Quaternion.LookRotation(tangent,upDirection);

            rotation *= Quaternion.Euler(0f, 90f, 0f);

            GameObject obj = createObject();
            obj.transform.position = globalPosition;
            obj.transform.rotation = rotation;
        }

    }

    private GameObject createObject(){
        GameObject newObject = Instantiate(railPrefab);
        newObject.transform.localScale = new float3(0.25f,0.25f,0.25f);

        railsModelList[currenteSpline].Add(newObject);

        return newObject;

    }
    private void deleteRails(){
        foreach (GameObject item in railsModelList[currenteSpline]){   
            Destroy(item);
        }

        railsModelList[currenteSpline].Clear();
    }

    public void SpawTrain(){
        GameObject train = Instantiate(trainPrefab);
        train.name = "train" + counter++;
        train.transform.position = rayStartPoint.position;
        RailCart railCart = train.GetComponent<RailCart>();


        railCart.container = container;
        railCart.currentSpline = null;
        railCart.setSpeed(railCart.maxSpeed);
    }

    public void SpawnWagon(){
        GameObject wagon = Instantiate(wagonPrefab);
        wagon.name = "wagon" + wagoncounter++;
        wagon.transform.position = rayStartPoint.position;

        Wagon wagonScript = wagon.GetComponent<Wagon>();
        wagonScript.container = container;
        wagonScript.setSpeed(0);
        wagonScript.currentSpline = null;
    }
}
