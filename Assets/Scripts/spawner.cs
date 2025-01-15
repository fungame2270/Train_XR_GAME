using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.BuildingBlocks;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.VisualScripting;

public class spawner : MonoBehaviour
{
   
    public GameObject AnchorPrefab;  // Prefab to spawn

    public GameObject railPrefab;

    public GameObject trainPrefab;

    private MRUKRoom room = null;
    private float maxPreviewDistance = 0.3f;
    public GameObject wagonPrefab;

    private int counter = 0;

    private int wagoncounter = 0;
    public Transform rayStartPoint;  // Ray origin point
    public float maxRayLength = 5f;  // Maximum ray length

    private GameObject _spawnedAnchor;  // The instantiated anchor
    private SplineContainer splineContainer;
    private Spline currentSpline;

    private RaycastHit hit;

    private Dictionary<Spline,List<GameObject>> railsModelList = new Dictionary<Spline, List<GameObject>>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Instantiate the anchor that will work as the pointer
        _spawnedAnchor = Instantiate(AnchorPrefab, rayStartPoint.position, Quaternion.identity);
        // Get a reference to the spline container
        splineContainer = FindFirstObjectByType<SplineContainer>();
        // Add a first spline to the container
        currentSpline = splineContainer.AddSpline();
        // Populate models' list with this new spline
        railsModelList.Add(currentSpline,new List<GameObject>());
    }

    void Update()
    {

        // Get a reference to the room
        if (room == null){
            room = MRUK.Instance.GetCurrentRoom();
        }

        // Create the ray from rayStartPoint position and forward direction
        Ray ray = new(rayStartPoint.position, rayStartPoint.forward);

        // Check if the room is null
        if (room == null)
        {
            Debug.LogError("Room is null!");
            return;
        }

        // Perform the raycast in the room
        bool hasHit = room.Raycast(ray, maxRayLength, out hit);
        if (!hasHit){
            return;
        }

        float3 finalPostion = hit.point;
        Vector3 forwardDirection = Vector3.Cross(Vector3.up, hit.normal);
        Quaternion finalRotation = SafeLookRotation(forwardDirection, hit.normal);

        float minDist = 0.1f;
        bool snap = isCloseToFirstKnot(hit.point ,minDist , out float3 snapPostion, out quaternion snapRotation);
        if(snap){
            finalPostion = snapPostion;
            finalRotation = snapRotation;
        }

        // Move the spawned anchor to the desired position with the desired rotation
        _spawnedAnchor.transform.position = finalPostion; 
        _spawnedAnchor.transform.rotation = finalRotation;
        
        if(currentSpline.Count < 2){
            return;
        }

        BezierKnot previewKnot = currentSpline[currentSpline.Count - 1];
        BezierKnot lastKnot = currentSpline[currentSpline.Count - 2];

        float3 direction = math.normalize(finalPostion - lastKnot.Position);
        finalPostion = lastKnot.Position + direction * math.min(maxPreviewDistance, math.distance(lastKnot.Position, finalPostion));

        previewKnot.Position = finalPostion;
        previewKnot.Rotation = finalRotation;
        currentSpline.SetKnot(currentSpline.Count - 1, previewKnot);
        spawnObjects();
    }

    private Quaternion SafeLookRotation(Vector3 forward, Vector3 up)
    {
        if (forward == Vector3.zero)
        {
            Debug.LogWarning("Forward vector is zero. Using default forward.");
            forward = Vector3.forward; // Default forward
        }
        if (up == Vector3.zero)
        {
            Debug.LogWarning("Up vector is zero. Using default up.");
            up = Vector3.up; // Default up
        }
        return Quaternion.LookRotation(forward, up);
    }

    private bool isCloseToFirstKnot(float3 point,float distanceRequired, out float3 returnPosition, out quaternion returnRotation){
        returnPosition = new float3();
        returnRotation = new quaternion();
        
        if (currentSpline.Count < 3){
            return false;
        }

        BezierKnot firstKnot = currentSpline[0];

        float distance = math.distance(firstKnot.Position,point);

        if (distance < distanceRequired && math.distance(firstKnot.Position, currentSpline[currentSpline.Count - 2].Position) < maxPreviewDistance){
            returnPosition = firstKnot.Position;
            returnRotation = firstKnot.Rotation;
            return true;
        }

        return false;
        
    }

    private void SpawnAnchor()
    {
        float minDist = 0.1f;
        bool snap = isCloseToFirstKnot(hit.point, minDist , out float3 snapPostion, out quaternion snapRotation);

        if(snap && currentSpline.Count >= 3){
            BezierKnot lastKnot = currentSpline[currentSpline.Count - 1];
            BezierKnot firstKnot = currentSpline[0];
            
            // Set the tangent in and out of the last knot
            // as the inverse of the first knot
            lastKnot.TangentIn = -firstKnot.TangentOut;
            lastKnot.TangentOut = -firstKnot.TangentIn;

            // Create a new spline
            createNewSpline();

            return;
        }

        BezierKnot knot = new BezierKnot(hit.point);
        currentSpline.Add(knot);

        if (currentSpline.Count >= 2)
        {
            // Set the tangent mode to AutoSmooth for the last two knots
            currentSpline.SetTangentMode(currentSpline.Count - 1, TangentMode.AutoSmooth);
            currentSpline.SetTangentMode(currentSpline.Count - 2, TangentMode.AutoSmooth);
        }

        if (currentSpline.Count == 1){
            currentSpline.Add(knot);
        }

    }

    private void createNewSpline(){
        currentSpline = splineContainer.AddSpline();
        railsModelList[currentSpline] = new List<GameObject>();
    }

    private void spawnObjects(){
        float fixedDistance = 0.015f;

        float splineLength = currentSpline.GetLength();

        int numberObjs = Mathf.CeilToInt(splineLength / fixedDistance);

        deleteRails();

        for(int i = 0; i <= numberObjs; i += 1){
            float distanceAlongSpline = i * fixedDistance; // Total distance to the current point
            float normalizedPosition = distanceAlongSpline / splineLength;

            SplineUtility.Evaluate(currentSpline,normalizedPosition,out float3 position,out float3 tangent,out float3 upDirection);
            Vector3 globalPosition = splineContainer.transform.TransformPoint(position);

            Quaternion rotation = SafeLookRotation(tangent, upDirection);

            rotation *= Quaternion.Euler(0f, 90f, 0f);

            GameObject obj = createObject();
            obj.transform.position = globalPosition;
            obj.transform.rotation = rotation;
        }

    }

    private GameObject createObject(){
        GameObject newObject = Instantiate(railPrefab);
        newObject.transform.localScale = new float3(0.25f,0.25f,0.25f);

        railsModelList[currentSpline].Add(newObject);

        return newObject;

    }
    private void deleteRails(){
        foreach (GameObject item in railsModelList[currentSpline]){   
            Destroy(item);
        }

        railsModelList[currentSpline].Clear();
    }

    public void SpawTrain(){
        GameObject train = Instantiate(trainPrefab);
        train.name = "train" + counter++;
        train.transform.position = rayStartPoint.position;
        RailCart railCart = train.GetComponent<RailCart>();

        railCart.container = splineContainer;
        railCart.currentSpline = null;
        railCart.setSpeed(railCart.maxSpeed);
    }

    public void SpawnWagon(){
        GameObject wagon = Instantiate(wagonPrefab);
        wagon.name = "wagon" + wagoncounter++;
        wagon.transform.position = rayStartPoint.position;

        Wagon wagonScript = wagon.GetComponent<Wagon>();
        wagonScript.container = splineContainer;
        wagonScript.setSpeed(0);
        wagonScript.currentSpline = null;
    }
}
