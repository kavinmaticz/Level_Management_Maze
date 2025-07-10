using UnityEngine;
using UnityEngine.AI;
using System;
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [SerializeField] Transform target;
    NavMeshAgent agent;
    LineRenderer lineRenderer;
    public DemoManager manager;
    void Start()
    {
       
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
       // lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
       
    }

    

    void Update()
    {
        if (target == null) return;

        agent.SetDestination(target.position);
        DrawPath(agent.path);
    }

    void DrawPath(NavMeshPath path)
    {
        if (path == null || path.corners.Length < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = path.corners.Length;

        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 corner = path.corners[i];
            corner.z = 0; // Ensure it stays 2D
            lineRenderer.SetPosition(i, corner);
        }
    }

    private void Awake()
    {
        manager.onPlayerDieEventHandler += MyData;
    }

    private void MyData(object sender, Data e)
    {
        print("Sender : " + sender.GetType() + " , Data : " + e.coin + " " + e.health);
    }
}
