using UnityEngine;
using UnityEngine.InputSystem;
public class ArenaController : MonoBehaviour
{
    public int counter = 0;
    public PlayerInputManager managerxd;

    public void Join(PlayerInput input)
    {
        TEST manager = input.GetComponent<TEST>();
        manager.testCamera.targetDisplay = counter;
        counter++;
    }
}
