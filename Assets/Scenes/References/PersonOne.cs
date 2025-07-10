using TMPro;
using UnityEngine;

public class PersonOne : MonoBehaviour, IScorabled
{
    [SerializeField] TMP_Text scoreTxt;
    [SerializeField] int _score;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateScrore()
    {
        _score += 10;
        scoreTxt.text = "ONE : " + _score.ToString();
    }
}
