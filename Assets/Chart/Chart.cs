using UnityEngine;
using System.Collections.Generic;
using Klak.Math;

sealed class Chart : MonoBehaviour
{
    [Space]
    [field:SerializeField] public GameObject Prefab = null;
    [field:SerializeField] public Transform Cursor = null;
    [field:SerializeField] public bool Randomize = true;
    [Space]
    [field:SerializeField] public Color[] Palette = null;
    [field:SerializeField] public float LowAlpha = 0.5f;
    [field:SerializeField] public float GapProbability = 0.3f;
    [Space]
    [field:SerializeField] public int ColumnCount = 3;
    [field:SerializeField] public int RowCount = 10;
    [Space]
    [field:SerializeField] public float CellHeight = 0.2f;
    [field:SerializeField] public Vector2 Interval = new Vector2(1, 0.02f);
    [Space]
    [field:SerializeField] public int Seed = 123;
    [Space]
    [field:SerializeField] public float CursorSpeed = 20;
    [field:SerializeField] public float ColorDelay = 0.1f;
    [field:SerializeField] public float ScanDelay = 0.3f;

    List<GameObject> _cells = new List<GameObject>();

    void BuildSequentialChart()
    {
        for (var col = 0; col < Palette.Length; col++)
        {
            var rows = Random.Range(RowCount / 2, RowCount - col - 1);

            for (var row = 0; row < rows; row += col + 1)
            {
                var h = col + 1;
                var x = col * (1 + Interval.x);
                var y = CellHeight * (row + 0.5f * h) + 0.5f * Interval.y;
                var s = CellHeight * h - Interval.y;

                var cell = Instantiate(Prefab);
                cell.transform.localPosition = new Vector3(x, -y, 0);
                cell.transform.localScale = new Vector3(1, s, 1);

                var color = Palette[col];
                color.a = LowAlpha;
                cell.GetComponent<MeshRenderer>().material.color = color;

                _cells.Add(cell);
            }
        }
    }

    void BuildRandomChart()
    {
        for (var col = 0; col < ColumnCount; col++)
        {
            var row = 0;
            var skip = false;

            while (row < RowCount)
            {
                var cidx = Random.Range(0, Palette.Length);
                cidx = Mathf.Min(cidx, RowCount - row - 1);

                var h = cidx + 1;
                var x = col * (1 + Interval.x);
                var y = CellHeight * (row + 0.5f * h) + 0.5f * Interval.y;
                var s = CellHeight * h - Interval.y;

                row += h;

                skip = !skip && Random.value < GapProbability;
                if (skip) continue;

                var cell = Instantiate(Prefab);
                cell.transform.localPosition = new Vector3(x, -y, 0);
                cell.transform.localScale = new Vector3(1, s, 1);

                var color = Palette[cidx];
                color.a = LowAlpha;
                cell.GetComponent<MeshRenderer>().material.color = color;

                _cells.Add(cell);
            }
        }

        for (var i = _cells.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i);
            (_cells[i], _cells[j]) = (_cells[j], _cells[i]);
        }
    }

    async Awaitable RunScanAnimationAsync()
    {
        var deltaAlpha = (1 - LowAlpha) / (ScanDelay * 0.2f);

        foreach (var cell in _cells)
        {
            transform.localPosition = cell.transform.localPosition;
            transform.localScale = cell.transform.localScale;

            await Awaitable.WaitForSecondsAsync(ColorDelay);

            var m = cell.GetComponent<MeshRenderer>().material;
            var color = m.color;

            while (color.a < 1)
            {
                color.a = Mathf.Min(1, color.a + deltaAlpha * Time.deltaTime);
                m.color = color;
                await Awaitable.NextFrameAsync();
            }

            await Awaitable.WaitForSecondsAsync(ScanDelay);

            while (color.a > LowAlpha)
            {
                color.a = Mathf.Max(LowAlpha, color.a - deltaAlpha * Time.deltaTime);
                m.color = color;
                await Awaitable.NextFrameAsync();
            }
        }
    }

    async void Start()
    {
        Random.InitState(Seed);
        if (Randomize) BuildRandomChart(); else BuildSequentialChart();
        while (true) await RunScanAnimationAsync();
    }

    void Update()
    {
        Cursor.localPosition = ExpTween.Step(Cursor.localPosition, transform.localPosition, CursorSpeed);
        Cursor.localScale = ExpTween.Step(Cursor.localScale, transform.localScale, CursorSpeed);
    }
}
