using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGrid : MonoBehaviour
{
    public int xSize, ySize;
    public float LarguraDoces = 1f;
    private GameObject[] Doces;
    private GridItem[,] _itens;
    private GridItem ItensSelecionadosRecorrente;
    public static int MinimoParaMatch = 3;
    public float DelayEntreMatches = 0.2f;
    public bool PodeJogar;

    void Start()
    {
        PodeJogar = true;
        GetCandies();
        FillGrid();
        ClearGrid();
        GridItem.OnMouseOverItemEventHandler += OnMouseOverItem;
    }

       void OnDisable ()
    {
        GridItem.OnMouseOverItemEventHandler -= OnMouseOverItem;
    }

    void FillGrid()
    {
        _itens = new GridItem [xSize, ySize];
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                _itens[x, y] = InstantiateCandy(x, y);
            }
        }
    }
    void ClearGrid()
    {
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                MatchInfo matchInfo = GetMatchInformation(_itens[x, y]);
                if (matchInfo.validMatch)
                {
                    Destroy(_itens[x, y].gameObject);
                    _itens[x, y] = InstantiateCandy(x, y);
                    y--;
                }
            }
        }
    }


    GridItem InstantiateCandy(int x, int y)
    {
        GameObject randomCandy = Doces [Random.Range (0, Doces.Length)];
        GridItem newCandy = ((GameObject)Instantiate(randomCandy, new Vector3(x * LarguraDoces, y), Quaternion.identity)).GetComponent<GridItem>();
        newCandy.OnItemPositionChanged(x, y);
        return newCandy;
    }

    void OnMouseOverItem(GridItem item)
    {
        if (ItensSelecionadosRecorrente == item)
        {
            return;
        }
        if (ItensSelecionadosRecorrente == null)
        {
            ItensSelecionadosRecorrente = item;
        }
        else
        {
            float xDiff = Mathf.Abs(item.x - ItensSelecionadosRecorrente.x);
            float yDiff = Mathf.Abs(item.y - ItensSelecionadosRecorrente.y);
            if (xDiff + yDiff == 1)
            {
                StartCoroutine(TryMatch(ItensSelecionadosRecorrente, item));
            }
            else
            {
                Debug.LogError("Esses Itens estão longe demais");
            }
            ItensSelecionadosRecorrente = null;
        }
    }

    
    IEnumerator TryMatch (GridItem a, GridItem b)
    {
        PodeJogar = false;
        yield return StartCoroutine(Swap(a, b));
        MatchInfo matchA = GetMatchInformation(a);
        MatchInfo matchB = GetMatchInformation(b);
        if (!matchA.validMatch && !matchB.validMatch)
        {
            yield return StartCoroutine(Swap(a, b));
            yield break;
        }
        if (matchA.validMatch)
        {
            yield return StartCoroutine(DestroyItems(matchA.match));
            yield return new WaitForSeconds(DelayEntreMatches);
            yield return StartCoroutine(UpdateGridAfterMatch(matchA));
        }
        else if (matchB.validMatch)
        {
            yield return StartCoroutine(DestroyItems(matchB.match));
            yield return new WaitForSeconds(DelayEntreMatches);
            yield return StartCoroutine(UpdateGridAfterMatch(matchB));
        }
        PodeJogar = true;
    }

    IEnumerator UpdateGridAfterMatch (MatchInfo match)
    {
        if (match.matchStartingY == match.matchEndingY)
        {
            for (int x = match.matchStartingX; x <= match.matchEndingX; x++)
            {
                for (int y = match.matchStartingY; y < ySize - 1; y++)
                {
                    GridItem upperIndex = _itens[x, y + 1];
                    GridItem current = _itens[x, y];
                    _itens[x, y] = upperIndex;
                    _itens[x, y + 1] = current;
                    _itens[x, y].OnItemPositionChanged(_itens[x, y].x, _itens[x, y].y - 1);
                }
                _itens [x, ySize - 1] = InstantiateCandy(x, ySize - 1);
            }
        }
        else if (match.matchEndingX == match.matchStartingX)
        {
            int matchHeight = 1 + (match.matchEndingY - match.matchStartingY);
            for (int y = match.matchStartingY + matchHeight; y <= ySize - 1; y++)
            {
                GridItem lowerIndex = _itens[match.matchStartingX, y - matchHeight];
                GridItem current = _itens[match.matchStartingX, y];
                _itens[match.matchStartingX, y - matchHeight] = current;
                _itens[match.matchStartingX, y] = lowerIndex;
            }
            for (int y = 0; y < ySize - matchHeight; y++)
            {
                _itens[match.matchStartingX, y].OnItemPositionChanged(match.matchStartingX, y);
            }
            for (int i = 0; i < match.match.Count; i++)
            {
                _itens[match.matchStartingX, (ySize - 1) - i] = InstantiateCandy(match.matchStartingX, (ySize - 1) - i);
            }
        }
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                MatchInfo matchInfo = GetMatchInformation(_itens[x, y]);
                if (matchInfo.validMatch)
                {
                    //yield return new WaitForSeconds(delayBetweenMatches);
                    yield return StartCoroutine(DestroyItems(matchInfo.match));
                    yield return new WaitForSeconds(DelayEntreMatches);
                    yield return StartCoroutine(UpdateGridAfterMatch(matchInfo));
                }
            }
        }
    }

    IEnumerator DestroyItems (List<GridItem> items)
    {
        foreach (GridItem i in items)
        {
            yield return StartCoroutine(i.transform.Scale(Vector3.zero, 0.05f));
            Destroy(i.gameObject);
        }
    }
    IEnumerator Swap (GridItem a, GridItem b)
    {
        ChangeRigidbodyStatus(false);
        float movDuration = 0.1f;
        Vector3 aPosition = a.transform.position;
        StartCoroutine(a.transform.Move (b.transform.position, movDuration));
        StartCoroutine(b.transform.Move (aPosition, movDuration));
        yield return new WaitForSeconds(movDuration);
        SwapIndices(a, b);
        ChangeRigidbodyStatus(true);
    }

    void SwapIndices (GridItem a, GridItem b)
    {
        GridItem tempA = _itens[a.x, a.y];
        _itens[a.x, a.y] = b;
        _itens[b.x, b.y] = tempA;
        int bOldX = b.x; int bOldY = b.y;
        b.OnItemPositionChanged(a.x, a.y);
        a.OnItemPositionChanged(bOldX, bOldY);
    }
    List<GridItem>SearchHorizontally (GridItem item)
    {
        List<GridItem> vItems = new List<GridItem> { item };
        int left = item.x - 1;
        int right = item.x + 1;
        while (left >= 0 && _itens[left, item.y].id == item.id)
        {
            vItems.Add(_itens[left, item.y]);
            left--;
        }
        while (right < xSize && _itens [right, item.y].id == item.id)
        {
            vItems.Add(_itens[right, item.y]);
            right++;
        }
        return vItems;
    }

    List<GridItem> SearchVertically(GridItem item)
    {
        List<GridItem> vItems = new List<GridItem> { item };
        int lower = item.y - 1;
        int upper = item.y + 1;
        while (lower >= 0 && _itens[item.x, lower].id == item.id)
        {
            vItems.Add(_itens[item.x, lower]);
            lower--;
        }
        while (upper < ySize && _itens[item.x, upper].id == item.id)
        {
            vItems.Add(_itens[item.x, upper]);
            upper++;
        }
        return vItems;
    }

    MatchInfo GetMatchInformation (GridItem item)
    {
        MatchInfo m = new MatchInfo();
        m.match = null;
        List<GridItem> hMatch = SearchHorizontally(item);
        List<GridItem> vMatch = SearchVertically(item);
        if (hMatch.Count >= MinimoParaMatch && hMatch.Count > vMatch.Count)
        {
            m.matchStartingX = GetMinimunX(hMatch);
            m.matchEndingX = GetMaximunX(hMatch);
            m.matchStartingY = m.matchEndingY = hMatch[0].y;
            m.match = hMatch;
        }
        else if (vMatch.Count >= MinimoParaMatch)
        {
            m.matchStartingY = GetMinimunY(vMatch);
            m.matchEndingY = GetMaximunY(vMatch);
            m.matchStartingX = m.matchEndingX = vMatch[0].x;
            m.match = vMatch;
        }
        return m;
    }

    int GetMinimunX (List<GridItem> items)
    {
        float[] indices = new float[items.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = items[i].x;
        }
        return (int)Mathf.Min(indices);
    }

    int GetMaximunX(List<GridItem> items)
    {
        float[] indices = new float[items.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = items[i].x;
        }
        return (int)Mathf.Max(indices);
    }

    int GetMinimunY(List<GridItem> items)
    {
        float[] indices = new float[items.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = items[i].y;
        }
        return (int)Mathf.Min(indices);
    }

    int GetMaximunY(List<GridItem> items)
    {
        float[] indices = new float[items.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = items[i].y;
        }
        return (int)Mathf.Max(indices);
    }
    void GetCandies()
    {
        Doces = Resources.LoadAll<GameObject>("Prefabs");
        for (int i = 0; i < Doces.Length; i++)
        {
            Doces[i].GetComponent<GridItem>().id = i;
        }
    }
    void ChangeRigidbodyStatus (bool status)
    {
        foreach (GridItem g in _itens)
        {
            g.GetComponent<Rigidbody2D>().isKinematic = !status;
        }
    }
}
