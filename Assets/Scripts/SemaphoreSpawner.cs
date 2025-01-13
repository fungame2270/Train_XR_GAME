using Oculus.Interaction;
using UnityEngine;

public class SemaphoreSpawner : MonoBehaviour
{
    private bool grabbed;

    private GrabInteractable grabInteractable;

    public GameObject childGrab;

    private UIController uiController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabInteractable = childGrab.GetComponent<GrabInteractable>();
        uiController = FindAnyObjectByType<UIController>();
    }

    // Update is called once per frame
    void Update(){
        if(grabInteractable != null && grabInteractable.State == InteractableState.Select){
            transform.parent = null;
            uiController.closeTrainSpawner();
            Destroy(this);
        }
    }
}
