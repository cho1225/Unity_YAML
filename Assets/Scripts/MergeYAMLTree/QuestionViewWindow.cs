using UnityEditor;
using UnityEngine;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

public class QuestionViewWindow : EditorWindow
{
    private string text = "";
    private string response = "ここに回答が表示されます";
    private const string API_KEY = ""; // OpenAI APIキーを設定
    private Vector2 scrollPos;

    private static void Open() => GetWindow<QuestionViewWindow>(nameof(QuestionViewWindow));

    private void OnGUI()
    {
        GUILayout.Label("ChatGPT 質問ウィンドウ", EditorStyles.boldLabel);

        // スクロール可能なテキスト入力フィールド
        GUILayout.Label("質問:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
        text = EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("送信", GUILayout.Height(30)))
        {
            SendToChatGPT(text);
        }

        GUILayout.Label("回答:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(response, MessageType.Info);
    }

    private async void SendToChatGPT(string question)
    {
        if (string.IsNullOrEmpty(question)) return;

        response = "送信中...";
        Repaint();

        const string apiUrl = "https://api.openai.com/v1/chat/completions";

        var requestData = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "あなたは役立つAIアシスタントです。" },
                new { role = "user", content = question }
            }
        };

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");

            string json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage responseMessage = await client.PostAsync(apiUrl, content);
            string result = await responseMessage.Content.ReadAsStringAsync();

            var responseObject = JsonConvert.DeserializeObject<dynamic>(result);
            response = responseObject?.choices[0]?.message?.content ?? "エラーが発生しました";
            Repaint();
        }
    }
}
