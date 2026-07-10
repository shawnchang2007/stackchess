using System.Collections.Generic;

namespace StackChess
{
    public sealed class FogState
    {
        public readonly HashSet<GridPosition> VisibleCells = new HashSet<GridPosition>();
        public readonly Dictionary<int, UnitModel> LastKnownEnemies = new Dictionary<int, UnitModel>();

        public void ClearVisibleCells()
        {
            VisibleCells.Clear();
        }
    }
}

