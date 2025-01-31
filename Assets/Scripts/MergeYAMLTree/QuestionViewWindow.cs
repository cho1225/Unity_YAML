using MergeYamlTree;
using UnityEditor;
using UnityEngine;

public class QuestionViewWindow : EditorWindow
{
    private string text = "";
    private string response = "青色にしたい場合は、以下の色を選択するべきです:\nm_Color: {r: 0, g: 0, b: 1, a: 1}\nこのコードでは、RGB 値が {r: 0, g: 0, b: 1} となっており、青色を表しています。\nr: 1, g: 0, b: 0 は赤色ですので、青色にするには後者の値を選択してください。";
    private const string API_KEY = ""; // OpenAI APIキーを設定
    private Vector2 scrollPos;

    [MenuItem("Tools/Question")]
    private static void Open() => GetWindow<QuestionViewWindow>(nameof(QuestionViewWindow));

    private void OnGUI()
    {
        GUILayout.Label("ChatGPT 質問ウィンドウ", EditorStyles.boldLabel);

        // スクロール可能なテキスト入力フィールド
        GUILayout.Label("質問:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));

        var style = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true
        };

        text = EditorGUILayout.TextArea(text, style);
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("送信", GUILayout.Height(30)))
        {
            Debug.Log("送信");
        }

        GUILayout.Label("回答:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(response, MessageType.Info);
    }
}
