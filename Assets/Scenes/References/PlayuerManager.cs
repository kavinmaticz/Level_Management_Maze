using UnityEngine;

public class PlayuerManager : MonoBehaviour
{
    [field: SerializeField]  int myInt;
    public static PlayuerManager instance;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        /* PersonOne personOne = collision.GetComponent<PersonOne>();
         personOne?.UpdateScrore();

         PersonTwo personTwo = collision.GetComponent<PersonTwo>();
         personTwo?.UpdateScrore();*/

        IScorabled scorable = collision.GetComponent<IScorabled>();
        scorable?.UpdateScrore();
    }

}


public interface IScorabled
{
    void UpdateScrore();
}