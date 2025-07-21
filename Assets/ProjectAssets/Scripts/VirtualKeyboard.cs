using UnityEngine;
using TMPro;

public class VirtualKeyboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;

    public void TypeCharacter(string character)
    {
        if (inputField.characterLimit > 0 && inputField.text.Length >= inputField.characterLimit)
        {
            return;
        }

        inputField.text = inputField.text + character;
    }

    public void DeleteLastCharacter()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Remove(inputField.text.Length - 1);
        }
    }
}