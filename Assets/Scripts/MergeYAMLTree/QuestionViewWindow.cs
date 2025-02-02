using MergeYamlTree;
using System.Net.Http;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
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
