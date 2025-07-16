using UnityEngine;
using UnityEngine.InputSystem;

public class TEST : MonoBehaviour
{
    public Vector2 steer;
    public bool button;
    public float accelerator;
    public bool shifter7;
    public bool l3;
    public bool bumper;
    public bool turn;
    public float wheelRed;
    public bool plus;
    public Vector2 cruzeta;
    public bool triangulo;
    public bool enter;
    public bool home;
    public Camera testCamera;
    public bool septima;


    public void Steer(InputAction.CallbackContext context)
    {
        steer = context.ReadValue<Vector2>();
    }

    public void Buttonxd(InputAction.CallbackContext context)
    {
        button = context.ReadValueAsButton();
    }

    public void Acelerador(InputAction.CallbackContext context)
    {
        accelerator = context.ReadValue<float>();
    }

    public void Shifter(InputAction.CallbackContext context)
    {
        shifter7 = context.ReadValueAsButton();
    }

    public void L3(InputAction.CallbackContext context)
    {
        l3 = context.ReadValueAsButton();
    }

    public void Bumper(InputAction.CallbackContext context)
    {
        bumper = context.ReadValueAsButton();
    }
    public void Turn(InputAction.CallbackContext context)
    {
        turn = context.ReadValueAsButton();
    }
    public void WheelRed(InputAction.CallbackContext context)
    {
        wheelRed = context.ReadValue<float>();
    }
    public void Plus(InputAction.CallbackContext context)
    {
        plus = context.ReadValueAsButton();
    }

    public void Cruzeta(InputAction.CallbackContext context)
    {
        cruzeta = context.ReadValue<Vector2>();
    }
    public void Triangulo(InputAction.CallbackContext context)
    {
        triangulo = context.ReadValueAsButton();
    }
    public void Enter(InputAction.CallbackContext context)
    {
        enter = context.ReadValueAsButton();
    }
    public void Home(InputAction.CallbackContext context)
    {
        home = context.ReadValueAsButton();
    }
    public void Septima(InputAction.CallbackContext context)
    {
        septima = context.ReadValueAsButton();
    }
}
