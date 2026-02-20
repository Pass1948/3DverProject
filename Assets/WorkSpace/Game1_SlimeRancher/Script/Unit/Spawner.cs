using UnityEngine;

public class Spawner : MonoBehaviour
{
    private GameObject slim1;
    private GameObject slim2;
    private GameObject slim3;
    private GameObject slim4;
    private GameObject slim5;

    private void Awake()
    {
        slim1 = GameManager.Resource.Load<GameObject>("Game1/Prefab/Slime1");
        slim2 = GameManager.Resource.Load<GameObject>("Game1/Prefab/Slime2");
        slim3 = GameManager.Resource.Load<GameObject>("Game1/Prefab/Slime3");
        slim4 = GameManager.Resource.Load<GameObject>("Game1/Prefab/Slime4");
        slim5 = GameManager.Resource.Load<GameObject>("Game1/Prefab/Slime5");
    }

    private void OnEnable()
    {
        GameManager.Event.Subscribe(EventType.Spawn, Spawn);
    }
    private void OnDisable()
    {
        GameManager.Event.Unsubscribe(EventType.Spawn, Spawn);
    }

    private void Spawn()
    {
        int i = Random.Range(0, 5);

        switch(i)
        {
            case 0:
                GameManager.Resource.Instantiate(slim1, transform.position, Quaternion.identity);
                break;
            case 1:
                GameManager.Resource.Instantiate(slim2, transform.position, Quaternion.identity);
                break;
            case 2:
                GameManager.Resource.Instantiate(slim3, transform.position, Quaternion.identity);
                break;
            case 3:
                GameManager.Resource.Instantiate(slim4, transform.position, Quaternion.identity);
                break;
            case 4:
                GameManager.Resource.Instantiate(slim5, transform.position, Quaternion.identity);
                break;
        }


    }



}
