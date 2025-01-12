using UnityEngine;

public class StartStopControll : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TrainMenu trainMenu;

    private Material material;
    void Start()
    {
        material = GetComponent<Material>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider collider){
        if(collider.name == "ControllerGrabLocation"){
            RailCart train = trainMenu.train;
            if(train.getSpeed() == train.maxSpeed){
                train.setSpeed(0);
                material.color = new Color(1,0,0,1);
            }else{
                train.setSpeed(train.maxSpeed);
                material.color = new Color(0,1,0,1);
            }
        }
    }
}
