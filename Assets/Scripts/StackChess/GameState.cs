using System.Collections.Generic;
using System.Text;

namespace StackChess
{
    public sealed class GameState
    {
        public const int BoardWidth = 12;
        public const int BoardHeight = 12;
        public const int MaxResourceStack = 5;
        public const int InfluenceToWin = 30;

        public int TurnNumber = 1;
        public int NextUnitId = 1;
        public PlayerId Winner = PlayerId.None;

        public readonly List<UnitModel> Units = new List<UnitModel>();
        public readonly Dictionary<GridPosition, int> ResourceStacks = new Dictionary<GridPosition, int>();
        public readonly Dictionary<GridPosition, int> Mines = new Dictionary<GridPosition, int>();
        public readonly List<ControlPoint> ControlPoints = new List<ControlPoint>();
        public readonly Dictionary<PlayerId, int> Influence = new Dictionary<PlayerId, int>();
        public readonly Dictionary<PlayerId, FogState> Fog = new Dictionary<PlayerId, FogState>();
        public readonly Dictionary<PlayerId, Dictionary<int, GameCommand>> PlannedCommands =
            new Dictionary<PlayerId, Dictionary<int, GameCommand>>();

        public readonly HashSet<PlayerId> SubmittedPlayers = new HashSet<PlayerId>();
        public readonly List<string> EventLog = new List<string>();

        public GameState()
        {
            Influence[PlayerId.PlayerOne] = 0;
            Influence[PlayerId.PlayerTwo] = 0;
            Fog[PlayerId.PlayerOne] = new FogState();
            Fog[PlayerId.PlayerTwo] = new FogState();
            PlannedCommands[PlayerId.PlayerOne] = new Dictionary<int, GameCommand>();
            PlannedCommands[PlayerId.PlayerTwo] = new Dictionary<int, GameCommand>();
        }

        public bool IsInside(GridPosition position)
        {
            return position.X >= 0 && position.X < BoardWidth && position.Y >= 0 && position.Y < BoardHeight;
        }

        public UnitModel GetUnit(int unitId)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Id == unitId)
                {
                    return Units[i];
                }
            }

            return null;
        }

        public UnitModel GetAliveUnitAt(GridPosition position)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                UnitModel unit = Units[i];
                if (unit.IsAlive && unit.Position == position)
                {
                    return unit;
                }
            }

            return null;
        }

        public IEnumerable<UnitModel> AliveUnits
        {
            get
            {
                for (int i = 0; i < Units.Count; i++)
                {
                    if (Units[i].IsAlive)
                    {
                        yield return Units[i];
                    }
                }
            }
        }

        public void AddEvent(string message)
        {
            EventLog.Insert(0, "T" + TurnNumber + " " + message);
            while (EventLog.Count > 12)
            {
                EventLog.RemoveAt(EventLog.Count - 1);
            }
        }

        public UnitModel AddUnit(PlayerId owner, UnitType type, GridPosition position, Direction facing)
        {
            UnitDefinition definition = UnitDefinition.All[type];
            UnitModel unit = new UnitModel
            {
                Id = NextUnitId++,
                Owner = owner,
                Type = type,
                Position = position,
                Health = definition.MaxHealth,
                Facing = facing,
                CarryingResource = false
            };

            Units.Add(unit);
            return unit;
        }

        public string DescribeCommands(PlayerId player)
        {
            StringBuilder builder = new StringBuilder();
            Dictionary<int, GameCommand> commands = PlannedCommands[player];
            foreach (KeyValuePair<int, GameCommand> pair in commands)
            {
                builder.AppendLine(pair.Value.ToString());
            }

            return builder.Length == 0 ? "No orders" : builder.ToString();
        }
    }
}

