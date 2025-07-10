using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TheadsUnity : MonoBehaviour
{
    public bool CanCheck;
    void Start()
    {
        
    }

    // Update is called once per frame
    async void  Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            CountAsynchronous();
        }
        else if(Input.GetKeyDown(KeyCode.D))
        {
              NonAsynchronous();
        }
    }

    async void CountAsynchronous()
    {
      List<float> task = await Task.Run(() =>
        {
            int max = 50000000;

            List<float> res = new List<float>();

            for (int i = 0; i < max; i++)
            {
                float n = Mathf.Sqrt(i);

                if (n < 100)
                {
                    res.Add(n);
                }
            }

            return res;
      });


        Debug.Log(task.Count);
    }

    void NonAsynchronous()
    {
        int max = 50000000;

        List<float> res = new List<float>();

        for (int i = 0; i < max; i++)
        {
            float n = Mathf.Sqrt(i);

            if (n < 100)
            {
                res.Add(n);
            }
        }

        Debug.Log(res.Count);
    }

}
