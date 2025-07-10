using TMPro;
using UnityEngine;

public class PersonTwo : MonoBehaviour, IScorabled
{
    [SerializeField] TMP_Text scoreTxt;
    [SerializeField] int _score;
    

    private void Awake()
    {
        
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateScrore()
    {
        _score += 15;
        scoreTxt.text = "TWO : " + _score.ToString();

        //PlayuerManager.instance.myInt = 1;
    }

  
}
