using UnityEngine;

public class CursorDesign : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture; // 커서로 사용할 텍스처
    void Start()
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto); // 커서 설정
    }


}
