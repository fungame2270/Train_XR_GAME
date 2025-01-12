using UnityEngine;

public class ColliderCheckerTrain : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public RailCart train;
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider collider){
        if(collider.name == "ColliderWagon"){
            Debug.Log($"Collide{collider.name}");
            train.setWagon(collider.gameObject.GetComponent<ColliderWagonChecker>().wagon);
        }
    }

}
