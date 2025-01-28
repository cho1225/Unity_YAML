using MergeYamlTree;
using UnityEditor;
using UnityEngine;

public class QuestionViewWindow : EditorWindow
{
    private string text = "";
    private string response = "ここに回答が表示されます";
    private const string API_KEY = "your-api-key-here"; // OpenAI APIキーを設定

    private static void Open() => GetWindow<QuestionViewWindow>(nameof(QuestionViewWindow));

    private void OnGUI()
    {
        GUILayout.Label("ChatGPT 質問ウィンドウ", EditorStyles.boldLabel);

        text = EditorGUILayout.TextField("質問:", text);

        if (GUILayout.Button("送信"))
        {
            Debug.Log("送信");
        }

        GUILayout.Label("回答:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(response, MessageType.Info);
    }
}
