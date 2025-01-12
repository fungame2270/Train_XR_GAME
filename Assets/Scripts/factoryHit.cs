using UnityEngine;

public class factoryHit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider colider){
        if(colider.tag == "trainEngine"){
            Debug.Log("Train Hit Station");
            RailCart railcart = colider.gameObject.GetComponent<RailCart>();
            if(railcart.lastStop != this){
                railcart.setSpeed(0);
                StartCoroutine(railcart.CallFunctionAfterDelay(10));
                railcart.lastStop = this;
            }

        }
    }
}
