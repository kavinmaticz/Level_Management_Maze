using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    [SerializeField] protected GameObject _interactableIndicatorIcon;
    protected GameObject _player;
    protected bool _isInteractable;
    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player").gameObject;
        
        _isInteractable = false;
        _interactableIndicatorIcon.SetActive(false);
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
        if (_isInteractable && Input.anyKey)
        {
           // Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == _player)
        {
            _isInteractable = true;
            _interactableIndicatorIcon.SetActive(true);
            Interact();
        }
           
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == _player)
        {
            _isInteractable = false;
            _interactableIndicatorIcon.SetActive(false);
        }
            
    }

    public abstract void Interact();
}
