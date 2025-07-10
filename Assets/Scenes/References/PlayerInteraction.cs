using UnityEngine;

public class PlayerInteraction : InteractableBase
{
    public override void Interact()
    {
        Debug.Log(gameObject.name);
    }

    
   
}
