using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using PDollarGestureRecognizer;
using Unity.Profiling;
using Unity.Barracuda;
using System.Threading.Tasks;
using System.Linq;
using Photon.Pun;

[System.Serializable]
public class PointCollection
{
    // 新增属性
    public bool IsEnhanced { get; private set; }
    public HashSet<string> DirtyRoles { get; } = new HashSet<string>();
    public List<Point> Points { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }

    public Dictionary<Vector2Int, List<Point>> grid;
    public float gridSize;

    public string RecognizedGestureName { get; private set; }

    // 新增状态标记
    public bool IsDirty { get; private set; } = true;
    
    public void MarkClean()
    {
        DirtyRoles.Clear();
        IsDirty = false;
    }
    public void MarkDirty() => IsDirty = true;
    // 新增 debugColor 字段，保证集合拥有自己的颜色且不会改变
    public Color debugColor;
    


    public PointCollection(Point initialPoint, float gridSize, bool isEnhanced)
    {
        this.IsEnhanced = isEnhanced;
        this.gridSize = gridSize;
        Points = new List<Point> { initialPoint };
        grid = new Dictionary<Vector2Int, List<Point>>();
        MinX = MaxX = initialPoint.X;
        MinY = MaxY = initialPoint.Y;
        // 初始化识别结果
        RecognizedGestureName = null;
        AddPointToGrid(initialPoint);
    }

     // 添加角色标记方法
    public void MarkDirtyByRole(string roleId)
    {
        DirtyRoles.Add(roleId);
        MarkDirty();
    }

    public void StoreRecognitionResult(string gestureName)
    {
        RecognizedGestureName = gestureName;
    }

    private Vector2Int GetGridKey(Point point)
    {
        int x = Mathf.FloorToInt(point.X / gridSize);
        int y = Mathf.FloorToInt(point.Y / gridSize);
        return new Vector2Int(x, y);
    }

    public void AddPointToGrid(Point point)
    {
        Vector2Int key = GetGridKey(point);
        if (!grid.ContainsKey(key))
            grid.Add(key, new List<Point>());
        grid[key].Add(point);
    }

    public void AddPoint(Point point)
    {
        Points.Add(point);
        ExpandBounds(point);
        AddPointToGrid(point);
        MarkDirty(); // 标记为需要识别
    }
    public IEnumerable<Point> GetPotentialNearbyPoints(Point point, float radius)
    {
        Vector2Int centerKey = GetGridKey(point);
        int gridRadius = Mathf.CeilToInt(radius / gridSize);
        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        {
            for (int dy = -gridRadius; dy <= gridRadius; dy++)
            {
                Vector2Int key = new Vector2Int(centerKey.x + dx, centerKey.y + dy);
                if (grid.TryGetValue(key, out var pointsInGrid))
                {
                    foreach (var p in pointsInGrid)
                        yield return p;
                }
            }
        }
    }
    // 非分配版本：将结果写入 resultList（调用前需 Clear()）
    public void GetPotentialNearbyPointsNonAlloc(Point point, float radius, List<Point> resultList)
    {
        resultList.Clear();
        Vector2Int centerKey = GetGridKey(point);
        int gridRadius = Mathf.CeilToInt(radius / gridSize);
        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        {
            for (int dy = -gridRadius; dy <= gridRadius; dy++)
            {
                Vector2Int key = new Vector2Int(centerKey.x + dx, centerKey.y + dy);
                if (grid.TryGetValue(key, out var pointsInGrid))
                {
                    resultList.AddRange(pointsInGrid);
                }
            }
        }
    }

    public void ExpandBounds(Point point)
    {
        MinX = Mathf.Min(MinX, point.X);
        MaxX = Mathf.Max(MaxX, point.X);
        MinY = Mathf.Min(MinY, point.Y);
        MaxY = Mathf.Max(MaxY, point.Y);
    }

    public void RecalculateBounds()
    {
        if (Points.Count == 0) return;
        MinX = MaxX = Points[0].X;
        MinY = MaxY = Points[0].Y;
        foreach (var point in Points)
        {
            ExpandBounds(point);
        }
    }
}

public class StrokeRecognizor : MonoBehaviourPunCallbacks
{
    [Header("Miracle Effect")]
    public MiracleEffectHandler miracleEffectHandler;
    public float singleEraseRadius = 1.5f; // 可调

    public GestureEffectManager gestureEffectManager;
    [Header("Collision Settings")]
    public LayerMask paintableLayerMask;
    [Header("Particle Settings")]
    public ParticleSystem lightParticleSystem; // 预配置的粒子系统（确保使用世界坐标发射）
    public bool enableParticleEffects = true;
    public int maxParticlesPerFrame = 50; // 防止单帧过多粒子
    public int emitInterval = 6;

    private ParticleSystem.EmitParams[] emitParamsPool;
    private int currentEmitIndex = 0;

    private List<Gesture> trainingSet = new List<Gesture>();
     // 分离两种集合
    private List<PointCollection> enhancedCollections = new List<PointCollection>();
    public List<PointCollection> trueCollections = new List<PointCollection>();

    private int strokeId = -1;
    private Vector3 virtualKeyPosition = Vector2.zero;
    
    public Camera recognizorCamera;

    // 新增异步识别队列
    private Queue<PointCollection> _pendingRecognition = new Queue<PointCollection>();
    private bool _isRecognizing = false;

    private float _eraseAutoRecognizeCooldown = 1.0f;

    private float _lastEraseAutoRecognizeTime = -1f;
    private Coroutine _pendingAutoRecognizeCoroutine;
    private bool _isAutoRecognizeRunning = false;

    //public TextManager textManager;
    
    public delegate void GestureRecognizedHandler(string gestureName, List<PointCollection> collections);
    public static event GestureRecognizedHandler OnGestureRecognized;
    
    private float fusionRegionSize = 3.0f;

    [Header("Debug Settings")]
    public bool enableDebugMarkers = true;
    public float debugMarkerSize = 0.05f;
    // 用于记录每个集合对应的调试标记
    private Dictionary<PointCollection, List<GameObject>> collectionDebugMarkers = new Dictionary<PointCollection, List<GameObject>>();
    private static readonly Color[] DebugColors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        new Color(1, 0.5f, 0),
        new Color(0.5f, 0, 1),
        new Color(0, 1, 0.5f),
        new Color(1, 0, 0.5f)
    };

    // 新增字段
    [Header("Neural Network Settings")]
    public NNModel neuralModel;
    public TextAsset classLabels;
    public NeuralGestureProcessorBehaviour processorBehaviour;

    [Header("Debug Settings")]
    public KeyCode debugSaveKey = KeyCode.L; // 新增调试按键


    // 复用临时变量与对象池
    private readonly List<PointCollection> _tempOverlappingCollections = new List<PointCollection>();
    private readonly List<Point> _tempNearbyPointsBuffer = new List<Point>();
    private readonly List<Point> _tempErasePointsBuffer = new List<Point>();
    private readonly List<Point> _tempRemainingPointsBuffer = new List<Point>();

    // 用于连通组件提取的并查集（Union-Find）数组（预分配复用）
    private int[] _ufParent;
    private int[] _ufRank;
    // 为避免 GC 分配，增加两个预分配数组用于组件映射
    private int[] _componentId;
    private int[] _componentMap;
    // _splitGrid 作为复用字典，key 为网格 cell（Vector2Int），value 为该 cell 中的点索引列表（从对象池中获取）
    private readonly Dictionary<Vector2Int, List<int>> _splitGrid = new Dictionary<Vector2Int, List<int>>();
    // 对象池：用于复用 List<Point> 和 List<int>
    private readonly Stack<List<Point>> _pointListPool = new Stack<List<Point>>();
    private readonly Stack<List<int>> _intListPool = new Stack<List<int>>();// 外部复用的结果集合
    private readonly List<List<Point>> _splitComponents = new List<List<Point>>();

    // 用于给新集合分配颜色的索引
    private int debugColorIndex = 0;



    private List<Point> GetPooledPointList()
    {
        if (_pointListPool.Count > 0)
            return _pointListPool.Pop();
        return new List<Point>(32); // 预设容量
    }

    private void ReleasePooledPointList(List<Point> list)
    {
        list.Clear();
        _pointListPool.Push(list);
    }

    private List<int> GetPooledIntList()
    {
        if (_intListPool.Count > 0)
            return _intListPool.Pop();
        return new List<int>(16);
    }

    private void ReleasePooledIntList(List<int> list)
    {
        list.Clear();
        _intListPool.Push(list);
    }
    

    private async void Start()
    {
        
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/10-stylus-MEDIUM/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        string[] filePaths = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (string filePath in filePaths)
            trainingSet.Add(GestureIO.ReadGestureFromFile(filePath));

        //if (textManager == null)
        //{
        //    textManager = FindObjectOfType<TextManager>();
        //    if (textManager == null) Debug.LogError("TextManager not found!");
        //}

        // 使用 Behaviour 来初始化并预热
        if (processorBehaviour != null)
        {
            // 把模型和标签赋给 Behaviour
            processorBehaviour.modelAsset   = neuralModel;
            processorBehaviour.labelJson    = classLabels;
            // 如果还需要把 ComputeShader 也传进去：
            // processorBehaviour.drawPointsShader =    ourDrawComputeShader;

            // 等待 Awake 中的 Initialize GPU / LoadModel 完成后，再预热
            await processorBehaviour.WarmupAsync();
            Debug.Log("NeuralGestureProcessorBehaviour 预热完成");
        }

        // 初始化粒子参数池
        emitParamsPool = new ParticleSystem.EmitParams[maxParticlesPerFrame];
        for (int i = 0; i < maxParticlesPerFrame; i++)
        {
            emitParamsPool[i] = new ParticleSystem.EmitParams();
        }

    }
    
    //void Update()
    //{
    //    if (Input.GetKeyDown(debugSaveKey))
    //    {
    //       // SaveAllCollectionsAsImages();
    //    }
    //}
    // 修改UpdatePoint方法
    public void UpdatePoint(Vector3 position, bool isEnhanced, string roleId)
    {
        Vector3 playerScreenPosition = recognizorCamera.WorldToScreenPoint(position);
        virtualKeyPosition = playerScreenPosition;
        Point newPoint = new Point(virtualKeyPosition.x, -virtualKeyPosition.y, strokeId);

        if (isEnhanced)
        {
            HandleEnhancedPoint(newPoint, roleId);
        }
        else
        {
            HandleTruePoint(newPoint, roleId);
        }
    }

    private void HandleEnhancedPoint(Point newPoint, string roleId)
    {
        _tempOverlappingCollections.Clear();
        foreach (var coll in enhancedCollections)
        {
            if (IsInFusionRegion(newPoint, coll))
                _tempOverlappingCollections.Add(coll);
        }

        if (_tempOverlappingCollections.Count == 0)
        {
            var newColl = new PointCollection(newPoint, fusionRegionSize, true);
            newColl.MarkDirtyByRole(roleId);
            enhancedCollections.Add(newColl);
        }
        else
        {
        for (int i = 0, cnt = _tempOverlappingCollections.Count; i < cnt; i++)
        {
            ClearDebugMarkers(_tempOverlappingCollections[i]);
            //GameObject textObject = textManager.GetTextObject(_tempOverlappingCollections[i].Points);
            //if (textObject != null) Destroy(textObject);
        }

        // 直接将第一个集合作为合并对象，合并其它重叠集合到其中
        PointCollection mergedCollection = MergeCollections(_tempOverlappingCollections);
        mergedCollection.AddPoint(newPoint);
        //MergeOverlappingPoints(ref mergedCollection, 0.1f);

        // 移除所有已合并的集合（避免重复）
        for (int i = enhancedCollections.Count - 1; i >= 0; i--)
        {
            if (_tempOverlappingCollections.Contains( enhancedCollections[i]))
                 enhancedCollections.RemoveAt(i);
        }
        // 添加合并后的集合
         enhancedCollections.Add(mergedCollection);
            mergedCollection.MarkDirtyByRole(roleId);
        }
    }


    private void HandleTruePoint(Point newPoint, string roleId)
    {
        // True集合特殊处理：始终合并到单个集合
        if (trueCollections.Count == 0)
        {
            var coll = new PointCollection(newPoint, fusionRegionSize, false);
            coll.MarkDirtyByRole(roleId);
            trueCollections.Add(coll);
        }
        else
        {
            var mainColl = trueCollections[0];
            mainColl.AddPoint(newPoint);
            mainColl.MarkDirtyByRole(roleId);
            
            // 合并到现有集合时也需要合并角色标记
            foreach (var coll in _tempOverlappingCollections)
            {
                mainColl.DirtyRoles.UnionWith(coll.DirtyRoles);
            }
        }
    }


    private PointCollection MergeCollections(List<PointCollection> collections)
    {
        // 如果没有重叠集合，直接返回一个新的空集合（或根据需求处理）
        if (collections.Count == 0)
            return new PointCollection(new Point(0, 0, 0), fusionRegionSize,true);

        // 直接使用第一个重叠集合作为基础
        PointCollection merged = collections[0];
        foreach (var coll in collections.Skip(1))
        {
            // 合并角色标记
            merged.DirtyRoles.UnionWith(coll.DirtyRoles);
            // 其他合并逻辑...
            foreach (var point in coll.Points)
            {
                merged.AddPoint(point);
            }
        }
        if(enableDebugMarkers)
            {
                ClearDebugMarkers(merged);
            }
        return merged;
    }

    private void MergeOverlappingPoints(ref PointCollection collection, float mergeThreshold)
{
    List<Point> points = collection.Points;
    int n = points.Count;
    // 构建基于 mergeThreshold 的网格
    Dictionary<Vector2Int, List<int>> mergeGrid = new Dictionary<Vector2Int, List<int>>();
    for (int i = 0; i < n; i++)
    {
        Point p = points[i];
        Vector2Int key = new Vector2Int(
            Mathf.FloorToInt(p.X / mergeThreshold),
            Mathf.FloorToInt(p.Y / mergeThreshold)
        );
        if (!mergeGrid.ContainsKey(key))
            mergeGrid[key] = new List<int>();
        mergeGrid[key].Add(i);
    }

    HashSet<int> visited = new HashSet<int>();
    List<Point> mergedPoints = new List<Point>();

    for (int i = 0; i < n; i++)
    {
        if (visited.Contains(i)) continue;
        List<int> clusterIndices = new List<int>();
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(i);
        visited.Add(i);

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();
            clusterIndices.Add(currentIndex);
            Point currentPoint = points[currentIndex];
            // 根据 mergeThreshold 计算当前点所在 cell
            Vector2Int currentKey = new Vector2Int(
                Mathf.FloorToInt(currentPoint.X / mergeThreshold),
                Mathf.FloorToInt(currentPoint.Y / mergeThreshold)
            );
            // 遍历周边 cell（共九个 cell）
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int neighborKey = new Vector2Int(currentKey.x + dx, currentKey.y + dy);
                    if (mergeGrid.TryGetValue(neighborKey, out List<int> candidateIndices))
                    {
                        foreach (int candidateIndex in candidateIndices)
                        {
                            if (visited.Contains(candidateIndex)) continue;
                            Point candidatePoint = points[candidateIndex];
                            if (Geometry.EuclideanDistance(currentPoint, candidatePoint) <= mergeThreshold)
                            {
                                visited.Add(candidateIndex);
                                queue.Enqueue(candidateIndex);
                            }
                        }
                    }
                }
            }
        }
        // 对当前聚类求平均值作为合并后的点
        float sumX = 0f, sumY = 0f;
        foreach (int idx in clusterIndices)
        {
            sumX += points[idx].X;
            sumY += points[idx].Y;
        }
        mergedPoints.Add(new Point(sumX / clusterIndices.Count, sumY / clusterIndices.Count, points[clusterIndices[0]].StrokeID));
    }

    collection.Points = mergedPoints;
    collection.RecalculateBounds();
    // 重建网格（依然使用原始的 gridSize，便于后续快速邻域查找）
    collection.grid.Clear();
    foreach (Point p in mergedPoints)
        collection.AddPointToGrid(p);
}


    private bool IsInFusionRegion(Point newPoint, PointCollection collection)
    {
        // 快速边界检查
        if (newPoint.X < collection.MinX - fusionRegionSize ||
            newPoint.X > collection.MaxX + fusionRegionSize ||
            newPoint.Y < collection.MinY - fusionRegionSize ||
            newPoint.Y > collection.MaxY + fusionRegionSize)
            return false;

        // 使用预分配的 _tempNearbyPointsBuffer 避免 yield 分配
        collection.GetPotentialNearbyPointsNonAlloc(newPoint, fusionRegionSize, _tempNearbyPointsBuffer);
        for (int i = 0, count = _tempNearbyPointsBuffer.Count; i < count; i++)
        {
            if (Geometry.EuclideanDistance(newPoint, _tempNearbyPointsBuffer[i]) < fusionRegionSize)
                return true;
        }
        return false;
    }



    public void ErasePoints(List<Vector3> collisionWorldPositions, float eraseRadius, string roleId)
{
    
    // 转换擦除坐标（保持不变）
    _tempErasePointsBuffer.Clear();
    foreach (var worldPos in collisionWorldPositions)
    {
        Vector3 screenPos = recognizorCamera.WorldToScreenPoint(worldPos);
        _tempErasePointsBuffer.Add(new Point(screenPos.x, -screenPos.y, -1));
    }

    // 处理增强集合（原有逻辑）
    ProcessEnhancedCollectionsErase(eraseRadius,roleId);
    
    // 单独处理True集合（简化逻辑）
    ProcessTrueCollectionsErase(eraseRadius);
}

private void ProcessEnhancedCollectionsErase(float eraseRadius, string roleID)
{
    // 1) 预计算擦除区域的包围盒
    float eraseMinX = float.MaxValue, eraseMaxX = float.MinValue;
    float eraseMinY = float.MaxValue, eraseMaxY = float.MinValue;
    foreach (var pt in _tempErasePointsBuffer)
    {
        eraseMinX = Mathf.Min(eraseMinX, pt.X - eraseRadius);
        eraseMaxX = Mathf.Max(eraseMaxX, pt.X + eraseRadius);
        eraseMinY = Mathf.Min(eraseMinY, pt.Y - eraseRadius);
        eraseMaxY = Mathf.Max(eraseMaxY, pt.Y + eraseRadius);
    }

    // 2) 用新列表保存所有“存活”或者拆分后的集合，避免遍历中修改原列表而越界:contentReference[oaicite:3]{index=3}
    List<PointCollection> newEnhanced = new List<PointCollection>();

    foreach (var coll in enhancedCollections)
    {
        // a) 包围盒初筛，无交集则保留
        if (coll.MaxX < eraseMinX || coll.MinX > eraseMaxX ||
            coll.MaxY < eraseMinY || coll.MinY > eraseMaxY)
        {
            newEnhanced.Add(coll);
            continue;
        }

        // b) 构造 updatedPoints，判断哪些点被擦除
        List<Point> updatedPoints = new List<Point>(coll.Points.Count);
        bool anyErased = false;
        float sqr = eraseRadius * eraseRadius;
        foreach (var p in coll.Points)
        {
            bool erase = false;
            foreach (var e in _tempErasePointsBuffer)
            {
                if ((p.X - e.X) * (p.X - e.X)
                  + (p.Y - e.Y) * (p.Y - e.Y) <= sqr)
                {
                    erase = true;
                    break;
                }
            }
            if (!erase) updatedPoints.Add(p);
            else anyErased = true;
        }

        // c) 如果一个点也没被擦除，直接保留
        if (!anyErased)
        {
            newEnhanced.Add(coll);
            continue;
        }

        // d) 如果所有点都被擦除，丢弃该集合并触发 AutoRecognize
        if (updatedPoints.Count == 0)
        {
            //AutoRecognize(roleID);
            continue;
        }

        // e) 部分擦除：根据连通性拆分或更新
        var components = SegmentConnectedComponents(
            updatedPoints,
            fusionRegionSize * fusionRegionSize
        );
        if (components.Count == 1)
        {
            // 直接更新原集合
            coll.Points = updatedPoints;
            coll.MarkDirty();
            coll.RecalculateBounds();
            coll.MarkDirtyByRole("DefaultRole");
            newEnhanced.Add(coll);
        }
        else
        {
            // 拆分成多个新集合
            foreach (var comp in components)
            {
                PointCollection pc = new PointCollection(comp[0], fusionRegionSize, true);
                for (int j = 1; j < comp.Count; j++)
                    pc.AddPoint(comp[j]);
                pc.MarkDirty();
                pc.MarkDirtyByRole("DefaultRole");
                newEnhanced.Add(pc);
            }
            // 根据冷却机制决定是否自动识别
            //if (Time.time - _lastEraseAutoRecognizeTime >= //_eraseAutoRecognizeCooldown)
            //{
            //    AutoRecognize(roleID);
            //    _lastEraseAutoRecognizeTime = Time.time;
            //}
            //else
            //{
            //    StartCoroutine(
            //        DelayedAutoRecognize(
            //            _eraseAutoRecognizeCooldown
            //            - (Time.time - _lastEraseAutoRecognizeTime),
            //            roleID
            //        )
            //    );
            //}
        }
    }

    // 3) 最后用新列表替换原集合，彻底避免索引失效与越界
    enhancedCollections = newEnhanced;

}


public void EraseTruePointsAt(Vector2 screenPos)
    {
        // 构造一个临时点
        Point erasePt = new Point(screenPos.x, screenPos.y, -1);

        float r2 = singleEraseRadius * singleEraseRadius;
        foreach (var trueColl in trueCollections.ToList())
        {
            // 移除所有距离小于 radius 的点
            trueColl.Points.RemoveAll(p =>
                (p.X - erasePt.X) * (p.X - erasePt.X) +
                (p.Y - erasePt.Y) * (p.Y - erasePt.Y)
                <= r2
            );

            if (trueColl.Points.Count == 0)
            {
                trueCollections.Remove(trueColl);
            }
            else
            {
                trueColl.RecalculateBounds();
                trueColl.MarkDirty();
            }
        }
    }
    public void RemoveEnhancedCollection(PointCollection coll)
    {
        enhancedCollections.Remove(coll);
    }


private void ProcessTrueCollectionsErase(float eraseRadius)
{
    // True集合特殊处理：只擦除不分割
    float eraseRadiusSqr = eraseRadius * eraseRadius;
    
    foreach (var trueColl in trueCollections.ToList()) // 使用ToList避免修改集合时出错
    {
        List<Point> remainingPoints = new List<Point>();
        
        foreach (var p in trueColl.Points)
        {
            bool keepPoint = true;
            foreach (var erasePt in _tempErasePointsBuffer)
            {
                if (Vector2.SqrMagnitude(new Vector2(p.X - erasePt.X, p.Y - erasePt.Y)) <= eraseRadiusSqr)
                {
                    keepPoint = false;
                    break;
                }
            }
            if (keepPoint) remainingPoints.Add(p);
        }

        // 直接替换点列表，无需分割
        trueColl.Points = remainingPoints;
        
        // 如果集合为空则移除
        if (trueColl.Points.Count == 0)
        {
            trueCollections.Remove(trueColl);
        }
        else
        {
            trueColl.RecalculateBounds();
            trueColl.MarkDirty();
        }
    }
}

/// <summary>
/// 利用空间网格和并查集将 points 按照阈值（阈值平方值为 connectivityThresholdSqr）分割成连通组件
/// </summary>
private List<List<Point>> SegmentConnectedComponents(List<Point> points, float connectivityThresholdSqr)
{
    int n = points.Count;
    if (n == 0) return new List<List<Point>>();
    int[] parent = new int[n];
    for (int i = 0; i < n; i++)
        parent[i] = i;
    // cellSize 取为阈值，即 √(connectivityThresholdSqr)
    float cellSize = Mathf.Sqrt(connectivityThresholdSqr);
    Dictionary<Vector2Int, List<int>> spatialGrid = new Dictionary<Vector2Int, List<int>>();
    for (int i = 0; i < n; i++)
    {
        Point p = points[i];
        Vector2Int key = new Vector2Int(Mathf.FloorToInt(p.X / cellSize), Mathf.FloorToInt(p.Y / cellSize));
        if (!spatialGrid.ContainsKey(key))
            spatialGrid[key] = new List<int>();
        spatialGrid[key].Add(i);
    }

    int Find(int x)
    {
        if (parent[x] != x)
            parent[x] = Find(parent[x]);
        return parent[x];
    }
    void Union(int a, int b)
    {
        int rootA = Find(a);
        int rootB = Find(b);
        if (rootA != rootB)
            parent[rootB] = rootA;
    }

    // 对于每个单元及其相邻单元中的点，进行距离检测
    foreach (var kvp in spatialGrid)
    {
        Vector2Int cell = kvp.Key;
        List<int> indices = kvp.Value;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int neighborKey = new Vector2Int(cell.x + dx, cell.y + dy);
                if (spatialGrid.TryGetValue(neighborKey, out List<int> neighborIndices))
                {
                    foreach (int i in indices)
                    {
                        foreach (int j in neighborIndices)
                        {
                            if (i < j && SquaredDistance(points[i], points[j]) < connectivityThresholdSqr)
                                Union(i, j);
                        }
                    }
                }
            }
        }
    }
    // 分组：将所有点按其根分组为连通组件
    Dictionary<int, List<Point>> components = new Dictionary<int, List<Point>>();
    for (int i = 0; i < n; i++)
    {
        int root = Find(i);
        if (!components.ContainsKey(root))
            components[root] = new List<Point>();
        components[root].Add(points[i]);
    }
    return components.Values.ToList();
}


    private List<Point> GetConnectedComponent(List<Point> points, int startIndex, bool[] visited, float thresholdSqr)
    {
        List<Point> component = new List<Point>();
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(startIndex);
        visited[startIndex] = true;
        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            component.Add(points[cur]);
            for (int i = 0; i < points.Count; i++)
            {
                if (!visited[i] && SquaredDistance(points[cur], points[i]) < thresholdSqr)
                {
                    visited[i] = true;
                    queue.Enqueue(i);
                }
            }
        }
        return component;
    }

    private float SquaredDistance(Point a, Point b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private int Find(int i)
    {
        if (_ufParent[i] != i)
            _ufParent[i] = Find(_ufParent[i]);
        return _ufParent[i];
    }

    private void Union(int i, int j)
    {
        int rootI = Find(i);
        int rootJ = Find(j);
        if (rootI == rootJ) return;
        if (_ufRank[rootI] < _ufRank[rootJ])
            _ufParent[rootI] = rootJ;
        else if (_ufRank[rootI] > _ufRank[rootJ])
            _ufParent[rootJ] = rootI;
        else
        {
            _ufParent[rootJ] = rootI;
            _ufRank[rootI]++;
        }
    }
    private IEnumerator DelayedAutoRecognize(float delay,string roleID)
    {
        yield return new WaitForSeconds(delay);

        // 再次检查是否还有更新的请求
        if (_pendingAutoRecognizeCoroutine != null)
        {
            AutoRecognize(roleID);
            _lastEraseAutoRecognizeTime = Time.time;
            _pendingAutoRecognizeCoroutine = null;
        }
    }


    // 修改后的AutoRecognize方法
    public void AutoRecognize(string roleId)
    {
        //textManager.ClearAllTexts();
        ++strokeId;
        
        // 处理增强集合,不处理True集合
        foreach (var coll in enhancedCollections.ToList())
        {
            if (coll.DirtyRoles.Contains(roleId)||roleId=="DefaultRole")
            {
                _pendingRecognition.Enqueue(coll);
            }
        }
        
        if (!_isRecognizing)
        {
            StartCoroutine(ProcessRecognitionQueue());
            if(enableDebugMarkers)
                GenerateDebugMarkersForAllCollections(); // 新增此行
        }
    }

    private IEnumerator ProcessRecognitionQueue()
    {
        _isRecognizing = true;
        
        while (_pendingRecognition.Count > 0)
        {
            var collection = _pendingRecognition.Dequeue();
            if(enableDebugMarkers)
            ClearDebugMarkers(collection);
            // 异步识别
            var recognitionTask = RecognizeCollectionAsync(collection);
            yield return new WaitUntil(() => recognitionTask.IsCompleted);

            
            // 处理结果
            var (className, confidence) = recognitionTask.Result;
            if (!string.IsNullOrEmpty(className))
            {
                Vector2 centroid = CalculateCentroid(collection.Points);
                Vector3 worldPosition = GetWorldPosition(centroid);

                // 在主线程执行Unity对象操作
                //GameObject textObject = textManager.InstantiateTextAtPosition(
                //    worldPosition,
                //    $"{className}\n{confidence:P0}",
                //    confidence,
                //    collection.Points.Count / 50f // 根据点数自动调整缩放
                //);

                collection.StoreRecognitionResult(className);             
                //textManager.RegisterText(collection.Points, textObject);
                gestureEffectManager.HandleGestureWithScale(className, collection.Points,collection.IsDirty,confidence);
                //SaveAllCollectionsAsImages();
                collection.MarkClean(); // 标记为已识别
                // 在识别成功时发射粒子
                if (enableParticleEffects)
                {
                    //EmitParticlesForCollection(collection);
                    var mainTrue = trueCollections.Count > 0 ? trueCollections[0] : null;
                    miracleEffectHandler.PlayEraseEffect(collection, mainTrue);
                }
            }
        }
        
        _isRecognizing = false;
    }

    private async Task<(string name, float score)> RecognizeCollectionAsync(PointCollection collection)
{
    if (collection.Points.Count < 10)
        return (null, 0f);

    
    Gesture candidate = new Gesture(collection.Points.ToArray());
    await Task.Yield(); // 确保后续代码在主线程上执行
    
    try
    {
        return await processorBehaviour.RecognizeAsync(candidate);
    }
    catch (Exception e)
    {
        Debug.LogError($"Recognition failed: {e}");
        return ("", 0);
    }
}

    //修改SaveAllCollectionsAsImages方法以保存所有集合
    private void SaveAllCollectionsAsImages()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "DebugImages");
        Directory.CreateDirectory(folderPath);
    
        // 保存增强集合
        for (int i = 0; i < enhancedCollections.Count; i++)
        {
            string path = Path.Combine(folderPath, 
                $"Enhanced_{i}_{DateTime.Now:yyyyMMddHHmmss}.png");
            NeuralGestureProcessor.SavePointCollectionAsImage(enhancedCollections[i], path);
        }
    
        // 保存True集合
        for (int i = 0; i < trueCollections.Count; i++)
        {
            string path = Path.Combine(folderPath,
                $"True_{i}_{DateTime.Now:yyyyMMddHHmmss}.png");
            NeuralGestureProcessor.SavePointCollectionAsImage(trueCollections[i], path);
        }
    
        Debug.Log($"共保存 {enhancedCollections.Count + trueCollections.Count} 个集合图像");
    }

    public Vector3 GetWorldPosition(Vector2 centroid)
    {
        Vector3 screenPosition = new Vector3(centroid.x, -centroid.y, recognizorCamera.nearClipPlane);
        Vector3 worldPosition = recognizorCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.y = GetCollisionYAtPosition(centroid);
        return worldPosition;
    }

    private Vector2 CalculateCentroid(List<Point> points)
    {
        float sumX = 0, sumY = 0;
        foreach (var point in points)
        {
            sumX += point.X;
            sumY += point.Y;
        }
        return new Vector2(sumX / points.Count, sumY / points.Count);
    }

    public float GetCollisionYAtPosition(Vector2 position)
    {
        Vector3 worldPosition = recognizorCamera.ScreenToWorldPoint(
            new Vector3(position.x, -position.y, recognizorCamera.nearClipPlane));
        Ray ray = new Ray(worldPosition, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, paintableLayerMask))
            return hit.point.y;
        return 0f;
    }

    private void ClearVisiblePaintables()
    {
        GameObject[] paintables = GameObject.FindGameObjectsWithTag("Paintable");
        foreach (GameObject obj in paintables)
        {
            Paintable paintable = obj.GetComponent<Paintable>();
            if (paintable != null)
                PaintManager.instance.eraseall(paintable, Vector3.zero, 999999f, 1.0f, 1.0f);
        }
    }

     // 生成调试标记：为指定集合的所有点生成标记
    private void GenerateDebugMarkers(PointCollection collection)
    {
        if (!enableDebugMarkers) return;

        List<GameObject> markers = new List<GameObject>();

        foreach (var point in collection.Points)
        {
            Vector3 screenPos = new Vector3(point.X, -point.Y, recognizorCamera.nearClipPlane + 0.1f);
            Vector3 worldPos = recognizorCamera.ScreenToWorldPoint(screenPos);
            worldPos.y = GetCollisionYAtPosition(new Vector2(point.X, point.Y)) + 0.05f;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = worldPos;
            marker.transform.localScale = Vector3.one * debugMarkerSize;
            marker.GetComponent<Renderer>().material.color = collection.debugColor;
            markers.Add(marker);
        }

        // 存储这份集合对应的调试标记（如果已有，先清理旧的）
        ClearDebugMarkers(collection);
        collectionDebugMarkers[collection] = markers;
    }

    private void GenerateDebugMarkersForAllCollections()
{
    if (!enableDebugMarkers) return;

    // 清除所有现有标记
    foreach (var coll in collectionDebugMarkers.Keys.ToList())
    {
        ClearDebugMarkers(coll);
    }

    // 生成增强集合标记（黄色系）
    foreach (var coll in enhancedCollections)
    {
        GenerateSingleCollectionMarkers(coll, Color.yellow);
    }

    // 生成True集合标记（青色系）
    foreach (var coll in trueCollections)
    {
        GenerateSingleCollectionMarkers(coll, Color.cyan);
    }
}

private void GenerateSingleCollectionMarkers(PointCollection collection, Color defaultColor)
{
    List<GameObject> markers = new List<GameObject>();
    
    foreach (var point in collection.Points)
    {
        Vector3 screenPos = new Vector3(point.X, -point.Y, recognizorCamera.nearClipPlane + 0.1f);
        Vector3 worldPos = recognizorCamera.ScreenToWorldPoint(screenPos);
        worldPos.y = GetCollisionYAtPosition(new Vector2(point.X, point.Y)) + 0.05f;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = worldPos;
        marker.transform.localScale = Vector3.one * debugMarkerSize;
        
        // 根据集合类型设置颜色
        marker.GetComponent<Renderer>().material.color = collection.IsEnhanced ? 
            new Color(1, 0.92f, 0.016f, 0.7f) : // 增强集合用亮黄色
            new Color(0, 1, 1, 0.7f);           // True集合用青色
        
        markers.Add(marker);
    }

    collectionDebugMarkers[collection] = markers;
}


    // 只清除指定集合对应的调试标记
    private void ClearDebugMarkers(PointCollection collection)
    {
        if (collectionDebugMarkers.TryGetValue(collection, out List<GameObject> markers))
        {
            foreach (var marker in markers)
            {
                Destroy(marker);
            }
            collectionDebugMarkers.Remove(collection);
        }
    }
}