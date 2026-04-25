using System.Collections.Generic;

namespace SS15.Atmospherics;

public sealed class AtmosMap
{
    public int Width { get; }
    public int Height { get; }
    private readonly GasMixture[] _cells;
    private readonly GasMixture[] _nextCells;
    private readonly bool[] _blocked; // true = стена (не пропускает ни газ, ни тепло)

    public AtmosMap(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new GasMixture[width * height];
        _nextCells = new GasMixture[width * height];
        _blocked = new bool[width * height];
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i] = new GasMixture();
            _nextCells[i] = new GasMixture();
        }
    }

    /// <summary>Пометить клетку как заблокированную (стена, через которую не идёт обмен).</summary>
    public void SetBlocked(int x, int y, bool blocked)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _blocked[x + y * Width] = blocked;
    }

    public bool IsBlocked(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height && _blocked[x + y * Width];

    public GasMixture GetCell(int x, int y) =>
        _cells[x + y * Width];

    public void SetCell(int x, int y, GasMixture mix)
    {
        int idx = x + y * Width;
        _cells[idx] = mix.Clone();
    }

    /// <summary>Шаг симуляции: диффузия газов и теплопередача (корректная, однократная).</summary>
    public void Simulate(float deltaTime)
    {
        float diffusionRate = 0.2f * deltaTime;
        float heatTransferRate = 0.1f * deltaTime;

        // Копируем текущее состояние во временный массив
        for (int i = 0; i < _cells.Length; i++)
        {
            _nextCells[i].Clear();
            for (int g = 0; g < 3; g++)
                _nextCells[i].Moles[g] = _cells[i].Moles[g];
            _nextCells[i].Temperature = _cells[i].Temperature;
        }

        // Обмен только с правым и нижним соседом (каждая пара один раз)
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            int idx = x + y * Width;
            if (_blocked[idx]) continue; // стена не участвует

            // Правый сосед
            if (x < Width - 1)
            {
                int rightIdx = (x + 1) + y * Width;
                if (!_blocked[rightIdx])
                    Exchange(idx, rightIdx, diffusionRate, heatTransferRate);
            }
            // Нижний сосед
            if (y < Height - 1)
            {
                int bottomIdx = x + (y + 1) * Width;
                if (!_blocked[bottomIdx])
                    Exchange(idx, bottomIdx, diffusionRate, heatTransferRate);
            }
        }

        // Применяем результат
        for (int i = 0; i < _cells.Length; i++)
        {
            var tmp = _cells[i];
            _cells[i] = _nextCells[i];
            _nextCells[i] = tmp;
        }
    }

    private void Exchange(int idxA, int idxB, float diffusionRate, float heatTransferRate)
    {
        // Газы
        for (int g = 0; g < 3; g++)
        {
            float diff = (_cells[idxB].Moles[g] - _cells[idxA].Moles[g]) * diffusionRate;
            _nextCells[idxA].Moles[g] += diff;
            _nextCells[idxB].Moles[g] -= diff;
        }

        // Тепло: передача от горячего к холодному пропорционально разнице температур
        float tempDiff = (_cells[idxB].Temperature - _cells[idxA].Temperature) * heatTransferRate;
        _nextCells[idxA].Temperature += tempDiff;
        _nextCells[idxB].Temperature -= tempDiff;
    }
}