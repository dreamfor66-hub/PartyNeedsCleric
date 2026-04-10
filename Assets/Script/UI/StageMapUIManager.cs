using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageMapUIManager : MonoBehaviour
{
    public static StageMapUIManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] RectTransform root;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform content;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] StageMapNodeView nodeTemplate;
    [SerializeField] RectTransform lineTemplate;

    readonly List<GameObject> spawned = new();
    Action<StageMapNode> onNodeSelected;
    int highestFloor = 1;
    int lowestFloor = 0;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    public static StageMapUIManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        return FindFirstObjectByType<StageMapUIManager>(FindObjectsInactive.Include);
    }

    public void ShowMap(StageMap map, StageMapNode currentNode, IReadOnlyList<StageMapNode> reachableNodes, IReadOnlyList<StageMapNode> visitedNodes, Action<StageMapNode> onSelected)
    {
        if (!ValidateReferences())
            return;

        onNodeSelected = onSelected;
        root.gameObject.SetActive(true);
        ClearSpawned();

        var reachableSet = new HashSet<StageMapNode>(reachableNodes ?? Array.Empty<StageMapNode>());
        var visitedSet = new HashSet<StageMapNode>(visitedNodes ?? Array.Empty<StageMapNode>());

        if (titleText != null)
            titleText.text = currentNode == null ? "Choose Your First Room" : $"Floor {currentNode.floorNumber} Clear";
        if (bodyText != null)
            bodyText.text = currentNode == null ? "Scroll up and pick a connected room." : "Choose one of the highlighted connected rooms.";

        BuildMapVisuals(map, currentNode, reachableSet, visitedSet);
        ScrollToNode(currentNode ?? map.entryNode ?? map.startNodes.FirstOrDefault());
    }

    public void Hide()
    {
        if (root != null)
            root.gameObject.SetActive(false);
        onNodeSelected = null;
    }

    bool ValidateReferences()
    {
        if (root == null)
            root = GetComponent<RectTransform>();
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
        if (content == null)
            content = transform.Find("Panel/Viewport/Content") as RectTransform;
        if (nodeTemplate == null)
            nodeTemplate = GetComponentInChildren<StageMapNodeView>(true);
        if (lineTemplate == null)
            lineTemplate = transform.Find("Panel/Viewport/Content/LineTemplate") as RectTransform;
        if (titleText == null)
        {
            var title = transform.Find("Panel/Header/Title");
            if (title != null) titleText = title.GetComponent<TMP_Text>();
        }
        if (bodyText == null)
        {
            var body = transform.Find("Panel/Header/Body");
            if (body != null) bodyText = body.GetComponent<TMP_Text>();
        }

        if (scrollRect != null)
        {
            if (scrollRect.viewport == null)
                scrollRect.viewport = transform.Find("Panel/Viewport") as RectTransform;
            if (scrollRect.content == null)
                scrollRect.content = content;
        }

        if (root != null && scrollRect != null && content != null && nodeTemplate != null && lineTemplate != null)
            return true;

        Debug.LogError("StageMapUIManager scene references are not fully assigned. Please wire the scene templates instead of relying on runtime creation.");
        return false;
    }

    void BuildMapVisuals(StageMap map, StageMapNode currentNode, HashSet<StageMapNode> reachableSet, HashSet<StageMapNode> visitedSet)
    {
        if (map == null)
            return;

        highestFloor = map.nodes.Count > 0 ? map.nodes.Max(x => x.floorNumber) : 1;
        lowestFloor = map.nodes.Count > 0 ? map.nodes.Min(x => x.floorNumber) : 0;
        int maxColumn = map.nodes.Count > 0 ? map.nodes.Max(x => x.column) : 0;
        float width = Mathf.Max(content.rect.width, content.sizeDelta.x);
        float height = Mathf.Max(content.rect.height, content.sizeDelta.y);
        var nodeRects = new Dictionary<StageMapNode, RectTransform>();

        foreach (var node in map.nodes.OrderBy(x => x.floorNumber))
        {
            var nodeView = Instantiate(nodeTemplate, content);
            nodeView.gameObject.SetActive(true);
            var rect = (RectTransform)nodeView.transform;
            rect.anchoredPosition = GetAnchoredPosition(node, maxColumn, width, height);
            nodeRects[node] = rect;

            bool selectable = reachableSet.Contains(node);
            nodeView.Bind(
                node,
                ResolveNodeColor(node, currentNode, reachableSet, visitedSet),
                ResolveNodeLabel(node),
                selectable,
                () => onNodeSelected?.Invoke(node));

            spawned.Add(nodeView.gameObject);
        }

        foreach (var node in map.nodes)
        {
            for (int i = 0; i < node.nextNodes.Count; i++)
            {
                if (!nodeRects.TryGetValue(node, out var from) || !nodeRects.TryGetValue(node.nextNodes[i], out var to))
                    continue;

                var line = Instantiate(lineTemplate, content);
                line.gameObject.SetActive(true);
                SetupLine(line, from.anchoredPosition, to.anchoredPosition, ResolveLineColor(node, node.nextNodes[i], currentNode, reachableSet, visitedSet));
                line.SetAsFirstSibling();
                spawned.Add(line.gameObject);
            }
        }
    }

    Vector2 GetAnchoredPosition(StageMapNode node, int maxColumn, float width, float height)
    {
        float leftPadding = 24f;
        float rightPadding = 24f;
        float bottomPadding = 28f;
        float topPadding = 32f;

        float usableWidth = Mathf.Max(1f, width - leftPadding - rightPadding);
        float usableHeight = Mathf.Max(1f, height - bottomPadding - topPadding);

        float x = maxColumn <= 0
            ? leftPadding + usableWidth * 0.5f
            : leftPadding + usableWidth * node.column / maxColumn;

        float normalizedFloor = (node.floorNumber - lowestFloor) / (float)Mathf.Max(1, highestFloor - lowestFloor);
        float y = -(height - bottomPadding - usableHeight * normalizedFloor);
        return new Vector2(x, y);
    }

    static void SetupLine(RectTransform rect, Vector2 from, Vector2 to, Color color)
    {
        var image = rect.GetComponent<Image>();
        if (image != null)
            image.color = color;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);

        Vector2 delta = to - from;
        rect.sizeDelta = new Vector2(delta.magnitude, rect.sizeDelta.y <= 0f ? 3f : rect.sizeDelta.y);
        rect.anchoredPosition = from;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    Color ResolveNodeColor(StageMapNode node, StageMapNode currentNode, HashSet<StageMapNode> reachableSet, HashSet<StageMapNode> visitedSet)
    {
        if (node == currentNode)
            return new Color(0.97f, 0.83f, 0.33f, 1f);
        if (node.isEntry)
            return new Color(0.78f, 0.82f, 0.9f, 1f);
        if (reachableSet.Contains(node))
            return ResolveRoomBaseColor(node.roomType);
        if (visitedSet.Contains(node))
            return new Color(0.38f, 0.52f, 0.62f, 1f);
        return new Color(0.24f, 0.27f, 0.33f, 1f);
    }

    Color ResolveLineColor(StageMapNode from, StageMapNode to, StageMapNode currentNode, HashSet<StageMapNode> reachableSet, HashSet<StageMapNode> visitedSet)
    {
        if ((currentNode == null && reachableSet.Contains(to)) || (currentNode == from && reachableSet.Contains(to)))
            return new Color(0.92f, 0.92f, 0.96f, 0.8f);

        if (visitedSet.Contains(from) && visitedSet.Contains(to))
            return new Color(0.42f, 0.54f, 0.66f, 0.75f);

        return new Color(0.26f, 0.29f, 0.35f, 0.55f);
    }

    static Color ResolveRoomBaseColor(StageRoomType roomType)
    {
        return roomType switch
        {
            StageRoomType.Elite => new Color(0.74f, 0.32f, 0.21f, 1f),
            StageRoomType.Boss => new Color(0.69f, 0.16f, 0.20f, 1f),
            _ => new Color(0.22f, 0.60f, 0.76f, 1f),
        };
    }

    static string ResolveNodeLabel(StageMapNode node)
    {
        if (node.isEntry)
            return "S";

        return node.roomType switch
        {
            StageRoomType.Elite => "E",
            StageRoomType.Boss => "B",
            _ => "N",
        };
    }

    void ScrollToNode(StageMapNode node)
    {
        if (node == null || scrollRect == null)
            return;

        float scroll = node.floorNumber <= lowestFloor
            ? 0f
            : Mathf.Clamp01((float)(node.floorNumber - lowestFloor) / Mathf.Max(1, highestFloor - lowestFloor));
        scrollRect.verticalNormalizedPosition = scroll;
    }

    void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();
    }
}
