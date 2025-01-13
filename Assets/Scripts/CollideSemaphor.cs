using UnityEngine;

public class CollideSemaphor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private TrainSemaphorController trainSemaphorController; 

    public RailCart railCart;
    void Start()
    {
        trainSemaphorController = FindAnyObjectByType<TrainSemaphorController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider collider){
        if(collider.name == "Cube"){
            SemaphorColliderStatus status = collider.gameObject.GetComponent<SemaphorColliderStatus>();
            Semaphor semaphore = status.semaphore;
            bool available = trainSemaphorController.checkAvailable(semaphore);
            if(available){
                railCart.setSpeed(railCart.maxSpeed);
            }else{
                railCart.setSpeed(0);
            }
        }
    }
}
