using UnityEngine;

public class InspectorView : MonoBehaviour
{
    public string text = "ここに入力";

    public void OnButtonClick()
    {
        Debug.Log("ボタンが押されました: " + text);
    }
}
