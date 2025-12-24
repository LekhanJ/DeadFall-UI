using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Aiming")]
    public Transform aimPivot;
    
    private Camera cam;
    private bool isLocal;
    
    void Start()
    {
        cam = Camera.main;
        isLocal = gameObject.name.Contains("_LOCAL");
        
        if (!isLocal)
        {
            // Disable input handling for remote players
            enabled = false;
        }
    }
    
    void Update()
    {
        if (!isLocal) return;
        
        HandleMovementInput();
        HandleAim();
    }
    
    void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Send input to server
        NetworkClient.Instance.SendMovementInput(h, v);
    }
    
    void HandleAim()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - transform.position).normalized;
        
        // Apply rotation locally for immediate feedback
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        aimPivot.rotation = Quaternion.Euler(0, 0, angle);
        
        // Send aim direction to server
        NetworkClient.Instance.SendAim(dir);
    }
}