using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
public class RailCart : MonoBehaviour
{
    public SplineContainer rail;

    private Spline currentSpline;
    private Rigidbody rb;
    public float speed = 0f;
    public float maxSpeed; // Set your desired max speed
    private float acceleration = 1f; // Adjust this value
    private float deceleration = 1f; // Adjust this value

    private float splinePosition = 0f;

    public void HitJunction(Spline railPassed)
    {
        currentSpline = railPassed;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpline = rail[0];  // Initial spline
        Debug.Log("Created Train");
        Debug.Log(rb);
    }

    private void FixedUpdate()
    {
        if (currentSpline == null) return;

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

    public void accel()
    {
        // Apply acceleration, but also respect the max speed
        RailCart railCart = GameObject.Find("train0").GetComponent<RailCart>();
        railCart.speed += acceleration * Time.deltaTime;
        //railCart.speed = Mathf.Clamp(railCart.speed,-maxSpeed,maxSpeed);
        Debug.Log("Accelerating");
    }

    public void deaccel()
    {
       RailCart railCart = GameObject.Find("train0").GetComponent<RailCart>();
        railCart.speed -= acceleration * Time.deltaTime;
        //railCart.speed = Mathf.Clamp(railCart.speed,-maxSpeed,maxSpeed);
        Debug.Log("Decelerating");
    }
}