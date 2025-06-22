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
/// Refactored gesture processor: GPU resource setup and model loading moved to MonoBehaviour lifecycle,
/// ensuring all Unity API calls occur on the main thread. Manual cleanup prevents GC finalizer crashes.
/// </summary>
public class NeuralGestureProcessor : IDisposable
{
    private Model _runtimeModel;
    private IWorker _worker;
    private Dictionary<int, string> _classLabels = new Dictionary<int, string>();
    private const int InputSize = 512;
    private const int PointRadius = 9;

    // GPU fields
    private RenderTexture _renderTexture;
    private ComputeShader _drawComputeShader;
    private ComputeBuffer _pointsBuffer;
    private int _drawKernel;
    private const int ThreadGroupSize = 8;

    /// <summary>
    /// Default constructor: no Unity API calls here!
    /// </summary>
    public NeuralGestureProcessor() { }

    /// <summary>
    /// Initialize RenderTexture and ComputeShader. Must be called from Awake/Start on the main thread.
    /// </summary>
    public void InitializeGPUResources(ComputeShader drawShader)
    {
        // Create or reuse RenderTexture
        if (_renderTexture == null)
        {
            _renderTexture = new RenderTexture(InputSize, InputSize, 0, RenderTextureFormat.R8)
            {
                enableRandomWrite = true,
                autoGenerateMips = false
            };
            _renderTexture.Create();
        }

        // Assign and compile compute shader
        if (_drawComputeShader == null)
        {
            _drawComputeShader = drawShader;
            _drawKernel = _drawComputeShader.FindKernel("CSMain");
            _drawComputeShader.GetKernelThreadGroupSizes(_drawKernel, out _, out _, out _);
        }
    }

    /// <summary>
    /// Load the NNModel and labels, create Barracuda worker. Must be called on main thread.
    /// </summary>
    public void LoadModel(NNModel modelAsset, TextAsset labelJson)
    {
        var runtimeModel = ModelLoader.Load(modelAsset);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);

        var wrapper = JsonUtility.FromJson<LabelWrapper>(labelJson.text);
        foreach (var item in wrapper.labels)
        {
            int id = int.Parse(item.key);
            _classLabels[id] = item.value.Replace("_images", "");
        }
    }

    // JSON classes
    [Serializable]
    private class LabelItem { public string key; public string value; }
    [Serializable]
    private class LabelWrapper { public List<LabelItem> labels; }

    public async Task WarmupAsync()
    {
        // 构造一个简单的伪手势（例如只包含一个点）
        List<Point> dummyPoints = new List<Point> { new Point(0, 0,0) };
        Gesture dummyGesture = new Gesture(dummyPoints.ToArray());

        // 调用识别方法进行预热（这里使用异步方法）
        var result = await RecognizeAsync(dummyGesture);
        Debug.Log($"预热完成，伪识别结果：{result.className}，置信度：{result.confidence}");
    }

    /// <summary>
    /// Recognize a gesture asynchronously. Call only after InitializeGPUResources and LoadModel.
    /// </summary>
    public async Task<(string className, float confidence)> RecognizeAsync(Gesture gesture)
    {
        CreateProcessedTextureGPU(gesture);
        float[] processedData = await ReadRenderTextureDataAsync(_renderTexture);

        using (var inputTensor = new Tensor(1, InputSize, InputSize, 1, processedData))
        {
            _worker.Execute(inputTensor);
            var outputTensor = _worker.PeekOutput();
            return PostprocessOutput(outputTensor);
        }
    }

    public async Task<(string className, float confidence)> RecognizeTextureAsync(RenderTexture rt)
    {
        // 1) 将 RT 中数据读回 CPU 并归一化到 [-1,1]
        var data = await ReadRenderTextureDataAsync(rt);

        // 2) 构造 Barracuda Tensor 并执行
        using (var input = new Tensor(1, rt.height, rt.width, 1, data))
        {
            _worker.Execute(input);
            var output = _worker.PeekOutput();
            // 3) 调用已有的后处理逻辑
            return PostprocessOutput(output);
        }
    }

    public async Task<Dictionary<string, float>> RecognizeTextureAllAsync(RenderTexture rt)  
    {  
        // 1) 读取数据  
        var data = await ReadRenderTextureDataAsync(rt);  
        int w = rt.width, h = rt.height;  

        // 2) 推理  
        using (var input = new Tensor(1, h, w, 1, data))  
        {  
            _worker.Execute(input);  
            var output = _worker.PeekOutput();  
            var logits = output.ToReadOnlyArray();  

            // 3) 计算 softmax  
            float max = logits.Max();  
            float sumExp = logits.Select(l => Mathf.Exp(l - max)).Sum();  
            var dict = new Dictionary<string, float>();  
            for (int i = 0; i < logits.Length; i++)  
            {  
                float prob = Mathf.Exp(logits[i] - max) / sumExp;  
                if (_classLabels.TryGetValue(i, out var label))  
                    dict[label] = prob;  
                else  
                    dict[$"idx_{i}"] = prob;  
            }  
            return dict;  
        }  
    }  



    private void CreateProcessedTextureGPU(Gesture gesture)
    {
        // Clear
        var cmd = new CommandBuffer();
        cmd.SetRenderTarget(_renderTexture);
        cmd.ClearRenderTarget(true, true, Color.black);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Dispose();

        // Normalize points
        var points = NormalizePoints(new List<Point>(gesture.Points), InputSize);
        var pointArray = points.ConvertAll(p => new Vector2(p.x * InputSize, p.y * InputSize)).ToArray();

        // Release old and set new buffer
        _pointsBuffer?.Release();
        _pointsBuffer = new ComputeBuffer(pointArray.Length, sizeof(float) * 2);
        _pointsBuffer.SetData(pointArray);

        // Dispatch
        _drawComputeShader.SetBuffer(_drawKernel, "_Points", _pointsBuffer);
        _drawComputeShader.SetInt("_PointCount", pointArray.Length);
        _drawComputeShader.SetInt("_Radius", PointRadius);
        _drawComputeShader.SetTexture(_drawKernel, "_Output", _renderTexture);
        int tg = Mathf.CeilToInt(InputSize / (float)ThreadGroupSize);
        _drawComputeShader.Dispatch(_drawKernel, tg, tg, 1);
    }
     // 在 NormalizePoints 方法中添加并行优化
private static List<Vector2> NormalizePoints(List<Point> points, int outputSize)
{
    Rect boundingRect = CalculateBoundingRect(points);
    float cropSize = Mathf.Max(boundingRect.width, boundingRect.height) * 1.5f;

    Vector2[] normalizedArray = new Vector2[points.Count];
    
    // 并行计算归一化坐标
    Parallel.For(0, points.Count, i =>
    {
        Point p = points[i];
        float x = (p.X - boundingRect.center.x) / cropSize + 0.5f;
        float y = (p.Y - boundingRect.center.y) / cropSize + 0.5f;
        normalizedArray[i] = new Vector2(
            Mathf.Clamp01(x),
            Mathf.Clamp01(y)
        );
    });

    return new List<Vector2>(normalizedArray);
}

private static Rect CalculateBoundingRect(List<Point> points)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in points)
        {
            minX = Mathf.Min(minX, p.X);
            maxX = Mathf.Max(maxX, p.X);
            minY = Mathf.Min(minY, p.Y);
            maxY = Mathf.Max(maxY, p.Y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }


    private async Task<float[]> ReadRenderTextureDataAsync(RenderTexture rt)
    {
        int width = rt.width;
        int height = rt.height;

        // 发起异步读回
        var req = AsyncGPUReadback.Request(rt, 0, TextureFormat.R8);
        while (!req.done) await Task.Yield();

        var raw = req.GetData<byte>();  // 长度 == width*height
        float[] data = new float[width * height];

        // 并行遍历真实的宽高
        await Task.Run(() =>
        {
            Parallel.For(0, height, y =>
            {
                int flippedY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    float v = raw[flippedY * width + x] / 255f;
                    data[y * width + x] = (v - 0.5f) / 0.5f;
                }
            });
        });

        // （可选）保存到文件，与宽高一致
        var lines = data.Select(v => v.ToString("F6")).ToArray();
        string path = Path.Combine(Application.persistentDataPath, "debug_unity.txt");
        File.WriteAllLines(path, lines);

        return data;
    }


    private (string, float) PostprocessOutput(Tensor output)
    {
        var logits = output.ToReadOnlyArray();
        float max = logits.Max();
        float sum = logits.Sum(l => Mathf.Exp(l - max));
        float[] probs = logits.Select(l => Mathf.Exp(l - max) / sum).ToArray();

        int idx = Array.IndexOf(probs, probs.Max());
        string name = _classLabels.TryGetValue(idx, out var n) ? n : "Unknown";
        return (name, Mathf.Clamp01(probs[idx]));
    }

    /// <summary>
    /// Explicit cleanup: releases GPU buffers on main thread to avoid finalizer crashes.
    /// </summary>
    public void Dispose()
    {
        _worker?.Dispose();
        _pointsBuffer?.Release();
        _renderTexture?.Release();
    }

    public static void SavePointCollectionAsImage(PointCollection collection, string path, int size = 512)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.R8, false);

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.black;
        tex.SetPixels(pixels);

        List<Vector2> points = NormalizePoints(collection.Points, size);
        foreach (var p in points)
        {
            int x = Mathf.Clamp((int)(p.x * size), 0, size - 1);
            int y = Mathf.Clamp((int)(p.y * size), 0, size - 1);
            DrawPoint(tex, x, y, PointRadius, Color.white);
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }
    private static void DrawPoint(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int px = Mathf.Clamp(cx + x, 0, tex.width - 1);
                    int py = Mathf.Clamp(cy + y, 0, tex.height - 1);
                    tex.SetPixel(px, py, color);
                }
            }
        }
    }
}
