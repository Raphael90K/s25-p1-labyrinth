using UnityEngine;

public class FaceTextureSwitcher : MonoBehaviour
{
    public Renderer faceQuadRenderer;

    public void SetEmotion(string emotion)
    {
        Texture2D tex = Resources.Load<Texture2D>($"Faces/{emotion.ToLower()}");
        if (tex != null)
        {
            faceQuadRenderer.material.mainTexture = tex;
        }
        else
        {
            Debug.LogWarning($"Keine Textur für Emotion: {emotion}");
        }
    }
}