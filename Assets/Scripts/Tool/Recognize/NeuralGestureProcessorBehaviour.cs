using UnityEngine; 
using Unity.Barracuda;
using System.Collections.Generic;
using System.IO;
using PDollarGestureRecognizer;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using System.Threading;
using System;
using System.Linq;

/// <summary>
/// MonoBehaviour to manage NeuralGestureProcessor lifecycle. Attach to a GameObject.
/// </summary>
public class NeuralGestureProcessorBehaviour : MonoBehaviour
{
    public NNModel modelAsset;
    public TextAsset labelJson;
    public ComputeShader drawPointsShader;

    private NeuralGestureProcessor _processor;

    void Awake()
    {
        _processor = new NeuralGestureProcessor();
        // GPU setup on main thread
        _processor.InitializeGPUResources(drawPointsShader);
        // Model and labels on main thread
        _processor.LoadModel(modelAsset, labelJson);
    }

    void OnDestroy()
    {
        // Ensure all GPU resources are released on main thread
        _processor.Dispose();
    }

    /// <summary>
    /// Example usage: call this to classify a gesture
    /// </summary>
    public async void RecognizeGesture(Gesture gesture)
    {
        var (className, confidence) = await _processor.RecognizeAsync(gesture);
        Debug.Log($"Result: {className}, Confidence: {confidence:F2}");
    }

    public async Task WarmupAsync() {
        await _processor.WarmupAsync();
    }
    public async Task<(string name, float score)> RecognizeAsync(Gesture gesture)
    {
        return await _processor.RecognizeAsync(gesture);
    }

     /// <summary>
    /// 对给定的 RenderTexture 做推理，返回 (className, confidence)
    /// </summary>
    public async Task<(string className, float confidence)> RecognizeTextureAsync(RenderTexture rt)
    {
        return await _processor.RecognizeTextureAsync(rt);
        
    }
    public async Task<Dictionary<string, float>> RecognizeTextureAllAsync(RenderTexture rt)  
    {
        return await _processor.RecognizeTextureAllAsync(rt);
    }
}
