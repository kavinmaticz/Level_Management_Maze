using UnityEngine;
using System;

public class DemoManager : MonoBehaviour
{
    public delegate void OnPlayerDie();

    public OnPlayerDie onPlayerDie;
    public event Action onPlayerDieAction;
    public event EventHandler<Data> onPlayerDieEventHandler;
    void Start()
    {
        
        onPlayerDieEventHandler?.Invoke(this,new Data { coin = 20, health = 52});
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FunctionOne()
    {
        print("FUNCTION 1");
    }

    void FunctionTwo()
    {
        print("FUNCTION 2");
    }

    void FunctionThree()
    {
        print("FUNCTION 3");
    }


}

[Serializable]
public class Data : EventArgs
{
    public int health;
    public float coin;
}
