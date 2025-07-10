using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    NavMeshAgent agent;
    public Transform target;
    public string moveState = "Idle";
    public Character idle;
    public Character walk;

    private string lastDirection = "";
    private string lastState = "";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        SetIdle(); // Set initial idle
        StartCoroutine(MovementCheckLoop());

        
    }

    void Update()
    {
        HandleClickOrTouch();
    }

    void HandleClickOrTouch()
    {
        if (Input.GetMouseButtonDown(0)) // mouse or single tap
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f; // ensure it's on the 2D plane

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("2D Raycast hit: " + hit.collider.name);

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(worldPos, out navHit, 1.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                    Debug.Log("Moving to NavMesh point: " + navHit.position);
                }
                else
                {
                    Debug.Log("Point not on NavMesh");
                }
            }
            else
            {
                Debug.Log("2D Raycast did not hit anything");
            }
        }
    }




    IEnumerator MovementCheckLoop()
    {
        while (true)
        {
            if (target != null)
            {
                agent.SetDestination(target.position);
            }

            Vector3 velocity = agent.velocity;

            if (velocity.sqrMagnitude > 0.01f)
            {
                moveState = "Walk";
                SetWalkDirection(velocity.normalized);
            }
            else
            {
                moveState = "Idle";
                SetIdleDirection();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void SetWalkDirection(Vector3 dir)
    {
        string direction = "";

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            direction = dir.x > 0 ? "Right" : "Left";
        else
            direction = dir.y > 0 ? "Up" : "Down";

        if (moveState != lastState || direction != lastDirection)
        {
            lastState = moveState;
            lastDirection = direction;

            DisableAll();
            ForcePlay(GetWalkObject(direction));
        }
    }

    void SetIdleDirection()
    {
        Vector3 dir = agent.desiredVelocity;
        string direction = "Down"; // default fallback

        if (dir.sqrMagnitude >= 0.01f)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                direction = dir.x > 0 ? "Right" : "Left";
            else
                direction = dir.y > 0 ? "Up" : "Down";
        }

        if (moveState != lastState || direction != lastDirection)
        {
            lastState = moveState;
            lastDirection = direction;

            DisableAll();
            ForcePlay(GetIdleObject(direction));
        }
    }

    GameObject GetWalkObject(string dir)
    {
        return dir switch
        {
            "Up" => walk.characterUp,
            "Down" => walk.characterDown,
            "Left" => walk.characterLeft,
            "Right" => walk.characterRight,
            _ => walk.characterDown
        };
    }

    GameObject GetIdleObject(string dir)
    {
        return dir switch
        {
            "Up" => idle.characterUp,
            "Down" => idle.characterDown,
            "Left" => idle.characterLeft,
            "Right" => idle.characterRight,
            _ => idle.characterDown
        };
    }

    void DisableAll()
    {
        idle.characterUp.SetActive(false);
        idle.characterDown.SetActive(false);
        idle.characterLeft.SetActive(false);
        idle.characterRight.SetActive(false);

        walk.characterUp.SetActive(false);
        walk.characterDown.SetActive(false);
        walk.characterLeft.SetActive(false);
        walk.characterRight.SetActive(false);
    }

    void SetIdle()
    {
        DisableAll();
        lastDirection = "Down";
        lastState = "Idle";
        ForcePlay(idle.characterDown);
    }

    void ForcePlay(GameObject obj)
    {
        obj.SetActive(true);
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Play(0, -1, 0);
        }
    }
}

[Serializable]
public class Character
{
    public GameObject characterUp;
    public GameObject characterDown;
    public GameObject characterLeft;
    public GameObject characterRight;
}
