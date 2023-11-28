using System;
using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
public class ForestGenerator : MonoBehaviour
{
    public int activeTreeCount = 500;
    public float radius;
    public GameObject shadowObject;
    public float shadowHeight = 1.0f;
    public bool useShadowInstance = true;
    public List<ElementTree> treeElements;
    private const int k = 30;  // Maximum number of attempts before marking a sample as inactive.
    float radius2;  // radius squared
    List<TreeState> listTrees = new List<TreeState>();
    List<TreeState> listActiveTrees = new List<TreeState>();
    // Start is called before the first frame update
    void Awake()
    { 
        foreach (ElementTree tree in treeElements)
        {
            for (int i = 0; i < tree.count; i++)
            {
                GameObject gb = Instantiate(tree.prefab, transform);
                if (useShadowInstance == true && shadowObject != null)
                {
                    GameObject shadow = Instantiate(shadowObject, gb.transform);
                    shadow.transform.localPosition = new Vector3(0, shadowHeight, 0);
                }
                TreeState ts = gb.GetComponent<TreeState>();
                ts.isActive = false;
                ts.tree.SetActive(false);
                listTrees.Add(ts);
            }
        }
    }
    private void Start()
    {
        StartCoroutine(ActivateTreeObject());
    }
    private void OnDestroy()
    {
        StopCoroutine(ActivateTreeObject());
    }
    public IEnumerator ActivateTreeObject()
    {
        if (PlayerManager.I.player != null)
        {
            radius2 = radius * radius; 
            yield return null;
        }
        while(true)
        {
            TreeState ts = GetInactiveTree();
            if (ts == null)
            {
                yield return null;
                continue;
            }
            if (listActiveTrees.Count == 0)
            {
                Vector3 treePos = PlayerManager.I.player.transform.position - PlayerManager.I.player.transform.forward * radius * 0.5f;
                ts.ActivateTree(true);
                ts.transform.position = new Vector3(treePos.x, TerrainManager.I.GetTerrainHeight(treePos.x, treePos.z), treePos.z);
                listActiveTrees.Add(ts);
                yield return null;
                continue;
            }
            int i = (int)(UnityEngine.Random.value * (float)(listActiveTrees.Count - 1)); 
            if (listActiveTrees[i].isActive == false)
            {
                listActiveTrees[i] = listActiveTrees[listActiveTrees.Count - 1];
                listActiveTrees.RemoveAt(listActiveTrees.Count - 1); 
                continue;
            }
            Vector3 sample = listActiveTrees[i].transform.position; 
            bool found = false;
            for (int j = 0; j < k; ++j)
            {
                float angle = 2 * Mathf.PI * UnityEngine.Random.value;
                float r = Mathf.Sqrt(UnityEngine.Random.value * 3 * radius2 + radius2);
                Vector3 candidate = sample + r * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                if ((candidate - PlayerManager.I.player.transform.position).magnitude < 2.0f) 
                    continue;
                if (IsFarEnough(candidate))
                {
                    found = true;
                    ts.ActivateTree(true);
                    ts.transform.position = new Vector3(candidate.x, TerrainManager.I.GetTerrainHeight(candidate.x, candidate.z), candidate.z);
                    listActiveTrees.Add(ts); 
                    break;
                }
            }
            if (!found)
            {
                listActiveTrees[i] = listActiveTrees[listActiveTrees.Count - 1];
                listActiveTrees.RemoveAt(listActiveTrees.Count - 1);
            }
            yield return new WaitUntil(() => { return TimeManager.I.isPause == false; });
        }
        yield return null;
    }
    private bool IsFarEnough(Vector3 sample)
    { 
        foreach (TreeState tree in listTrees)
        {
            if (tree.isActive == false) continue;
            Vector3 dist = sample - tree.transform.position;
            dist.y = 0;
            if (dist.sqrMagnitude < radius2) return false;
        } 
        return true; 
    }
    TreeState GetInactiveTree()
    {
        foreach (TreeState tree in listTrees)
        {
            if (tree.isActive == false)
            {
                return tree;
            }
        }
        return null;
    }
}
[Serializable]
public class ElementTree
{
    public string name;
    public GameObject prefab;
    public int count;
}