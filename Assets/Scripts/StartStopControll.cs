using UnityEngine;

public class StartStopControll : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TrainMenu trainMenu;

    private Renderer renderer_;
    void Start()
    {
        renderer_ = GetComponent<Renderer>();
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
                renderer_.material.SetColor("_Color",Color.red);
            }else{
                train.setSpeed(train.maxSpeed);
                renderer_.material.SetColor("_Color",Color.green);
            }
        }
    }
}
