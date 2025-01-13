using UnityEngine;

public class CloseTrainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    private UIController uIController;

    private Material material;
    void Start()
    {
        uIController = FindAnyObjectByType<UIController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider collider){
        if(collider.name == "ControllerGrabLocation"){
            uIController.CloseTrainMenu();
        }
    }
}
