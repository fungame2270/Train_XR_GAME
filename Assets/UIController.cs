using UnityEngine;

public class UIController : MonoBehaviour
{

    public GameObject TrainSpawnerPrefab;
    private GameObject trainSpawner = null;

    public GameObject TrainMenuPrefab;
    private GameObject trainMenu = null;

    public Transform spawnLocation;

    public 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleTrainSpawner(){
        if(trainSpawner != null){
            closeTrainSpawner();
        }else{
            trainSpawner = Instantiate(TrainSpawnerPrefab);
            trainSpawner.transform.position = spawnLocation.position;
        }
    }

    public void closeTrainSpawner(){
        if(trainSpawner != null){
            Destroy(trainSpawner);
            trainSpawner = null;
        }
    }

    public void spawnTrainMenu(RailCart train){
        if(trainMenu != null){
            Destroy(trainMenu);
        }
        trainMenu = Instantiate(TrainMenuPrefab);
        TrainMenu script = trainMenu.GetComponent<TrainMenu>();
        script.train = train;
        trainMenu.transform.position = spawnLocation.transform.position + new Vector3(0.1f,0.1f,0.1f);
    }
}
