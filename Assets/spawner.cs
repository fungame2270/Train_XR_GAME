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

    private int counter = 0;

    private GameObject train;
    public Transform rayStartPoint;  // Ray origin point
    public float maxRayLength = 5f;  // Maximum ray length

    private GameObject _spawnedAnchor;  // The instantiated anchor
    private SpatialAnchorCoreBuildingBlock _spatialAnchorCore;  // Spatial Anchor Core
    private SplineContainer container;
    private Spline spline;

    private List<GameObject> railModelList = new List<GameObject>();

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
        spline = container.AddSpline();
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
        bool hasHit = room.Raycast(ray, maxRayLength, out RaycastHit hit, out MRUKAnchor anchor);
        if (!hasHit){
            return;
        }
        bool snap = SplineClosestKnot(hit.point,0.10f,out float3 returnPosition);
        float3 updatePos;
        if(snap){
            updatePos = returnPosition;
        }else{
            updatePos = hit.point;
        }
        // Move the spawned anchor to the hit point
        _spawnedAnchor.transform.position = updatePos;  // Move anchor to hit point
        _spawnedAnchor.transform.rotation = Quaternion.LookRotation(hit.normal);  // Align anchor rotation with surface normal
    }

    private bool SplineClosestKnot(float3 point,float distanceRequired,out float3 returnPosition){
        float minDistance = float.PositiveInfinity;
        int nearestIndex = -1;
        returnPosition = new float3(0f,0f,0f);

        for (int i = 0; i < spline.GetLength(); i++){

            float distance = math.distance(spline[i].Position,point);
            
            if (distance < minDistance){
                minDistance = distance;
                nearestIndex = i;
            }

        }

        if (nearestIndex != -1 && minDistance < distanceRequired){
            returnPosition = spline[nearestIndex].Position;
            return true;
        }
        return false;
        
    }

    private void SpawnAnchor()
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
        bool hasHit = room.Raycast(ray, maxRayLength, out RaycastHit hit, out MRUKAnchor anchor);
        // If the raycast hit something, instantiate the spatial anchor
        if (!hasHit){
            return;
        }
        bool snap = SplineClosestKnot(hit.point,0.10f,out float3 returnPosition);
        float3 pos;
        if(snap){
            pos = returnPosition;
        }else{
            pos = hit.point;
        }

        // Check if _spatialAnchorCore is null
        if (_spatialAnchorCore == null)
        {
            Debug.LogError("_spatialAnchorCore is null!");
            return;
        }

        // Instantiate the spatial anchor at the hit point with the normal as the rotation
        // _spatialAnchorCore.InstantiateSpatialAnchor(AnchorPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        Vector3 upDirection = hit.normal;  // This is the direction you want the top of the model to point
        Vector3 forwardDirection = Vector3.Cross(Vector3.up, upDirection); // Cross product gives the side direction to make sure it's perpendicular

        // Adjust the rotation to align the model's top (Y-axis) to the normal direction
        Quaternion rotation = Quaternion.LookRotation(forwardDirection, upDirection);

        // Create the Bezier knot and add it to the spline
        BezierKnot knot = new BezierKnot(pos, 0, 0, rotation);

        spline.Add(knot);
        
        var all = new SplineRange(0,spline.Count);

        int knotsCount = spline.Count;
        if (knotsCount >= 2)
        {
            // Set the tangent mode to AutoSmooth for the last two knots
            spline.SetTangentMode(knotsCount - 1, TangentMode.AutoSmooth);
            spline.SetTangentMode(knotsCount - 2, TangentMode.AutoSmooth);
        }

        // Smoothly connect the last knot with the first knot
        if (spline.GetLength() > 1)
        {
            // Access the first and last knots
            BezierKnot firstKnot = spline[0];
            BezierKnot lastKnot = spline[knotsCount - 1];

            // Check if the positions of the first and last knots are the same
            if (math.all(lastKnot.Position == firstKnot.Position))
            {
                Debug.Log("Smoothing last knot with the first");

                // Mirror the tangents between the last and first knots
                lastKnot.TangentIn = -firstKnot.TangentOut;
                lastKnot.TangentOut = -firstKnot.TangentIn;

                // Explicitly reassign the modified knot to the spline
                spline.SetKnot(knotsCount - 1, lastKnot);
            }
        }


        spawnObjects();
    }

    private void spawnObjects(){
        float fixedDistance = 0.015f;
        Spline spline = container[0];

        float splineLength = spline.GetLength();

        int numberObjs = Mathf.CeilToInt(splineLength / fixedDistance);

        deleteRails();

        for(int i = 0; i <= numberObjs; i += 1){
            float distanceAlongSpline = i * fixedDistance; // Total distance to the current point
            float normalizedPosition = distanceAlongSpline / splineLength;

            SplineUtility.Evaluate(spline,normalizedPosition,out float3 position,out float3 tangent,out float3 upDirection);
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

        railModelList.Add(newObject);

        return newObject;

    }
    private void deleteRails(){
        foreach (GameObject item in railModelList){   
            Destroy(item);
        }

        railModelList.Clear();
    }

    public void SpawTrain(){
        train = Instantiate(trainPrefab);
        train.name = "train" + counter++;
        train.transform.position = rayStartPoint.position;
        RailCart railCart = train.GetComponent<RailCart>();
        

        railCart.rail = container;
    }
}
