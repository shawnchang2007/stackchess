using System.Collections.Generic;
using UnityEngine;

namespace StackChess
{
    public static class StackChessRules
    {
        public static GameState CreateNewGame()
        {
            GameState state = new GameState();

            state.AddUnit(PlayerId.PlayerOne, UnitType.Worker, new GridPosition(1, 5), Direction.East);
            state.AddUnit(PlayerId.PlayerOne, UnitType.Infantry, new GridPosition(1, 4), Direction.East);
            state.AddUnit(PlayerId.PlayerTwo, UnitType.Worker, new GridPosition(10, 6), Direction.West);
            state.AddUnit(PlayerId.PlayerTwo, UnitType.Infantry, new GridPosition(10, 7), Direction.West);

            AddStack(state, new GridPosition(1, 6), 3);
            AddStack(state, new GridPosition(10, 5), 3);

            state.Mines[new GridPosition(0, 2)] = 5;
            state.Mines[new GridPosition(2, 9)] = 5;
            state.Mines[new GridPosition(11, 9)] = 5;
            state.Mines[new GridPosition(9, 2)] = 5;
            state.Mines[new GridPosition(5, 2)] = 5;
            state.Mines[new GridPosition(6, 9)] = 5;

            state.ControlPoints.Add(new ControlPoint(new GridPosition(5, 3)));
            state.ControlPoints.Add(new ControlPoint(new GridPosition(6, 5)));
            state.ControlPoints.Add(new ControlPoint(new GridPosition(5, 8)));

            RefreshFog(state);
            state.AddEvent("Game started.");
            return state;
        }

        public static PlayerId OtherPlayer(PlayerId player)
        {
            return player == PlayerId.PlayerOne ? PlayerId.PlayerTwo : PlayerId.PlayerOne;
        }

        public static void QueueCommand(GameState state, PlayerId player, GameCommand command)
        {
            UnitModel unit = state.GetUnit(command.UnitId);
            if (unit == null || !unit.IsAlive || unit.Owner != player)
            {
                return;
            }

            if (state.SubmittedPlayers.Contains(player))
            {
                return;
            }

            state.PlannedCommands[player][command.UnitId] = command;
        }

        public static bool CanResolve(GameState state)
        {
            return state.SubmittedPlayers.Contains(PlayerId.PlayerOne) &&
                   state.SubmittedPlayers.Contains(PlayerId.PlayerTwo) &&
                   state.Winner == PlayerId.None;
        }

        public static void ResolveTurn(GameState state)
        {
            if (!CanResolve(state))
            {
                return;
            }

            RefreshFog(state);
            ResolveAttacks(state);
            RemoveDeadUnits(state);
            ResolveMovementAndTurns(state);
            RemoveDeadUnits(state);
            ResolveEconomy(state);
            RemoveDeadUnits(state);
            ResolveControlPoints(state);
            RefreshFog(state);

            state.PlannedCommands[PlayerId.PlayerOne].Clear();
            state.PlannedCommands[PlayerId.PlayerTwo].Clear();
            state.SubmittedPlayers.Clear();
            state.TurnNumber++;
        }

        public static bool IsAttackValid(UnitModel attacker, GridPosition target)
        {
            UnitDefinition definition = attacker.Definition;
            if (definition.AttackRange <= 0)
            {
                return false;
            }

            int distance = attacker.Position.ManhattanDistance(target);
            if (distance < 1 || distance > definition.AttackRange)
            {
                return false;
            }

            if (!definition.UsesFacing)
            {
                return true;
            }

            return IsInsideFacingCone(attacker.Position, target, attacker.Facing);
        }

        public static bool IsMoveValid(GameState state, UnitModel unit, GridPosition target)
        {
            if (!state.IsInside(target))
            {
                return false;
            }

            return unit.Position.ManhattanDistance(target) <= unit.Definition.MoveRange;
        }

        public static bool IsVisibleTo(GameState state, PlayerId player, GridPosition position)
        {
            return state.Fog[player].VisibleCells.Contains(position);
        }

        private static void ResolveAttacks(GameState state)
        {
            List<UnitModel> attackers = new List<UnitModel>(state.AliveUnits);
            Dictionary<int, int> pendingDamage = new Dictionary<int, int>();

            for (int i = 0; i < attackers.Count; i++)
            {
                UnitModel attacker = attackers[i];
                GameCommand command = GetCommand(state, attacker);
                if (command == null || command.Type != CommandType.Attack)
                {
                    continue;
                }

                if (!IsAttackValid(attacker, command.Target))
                {
                    state.AddEvent(attacker.Definition.Label + attacker.Id + " attack was invalid.");
                    continue;
                }

                UnitModel target = state.GetAliveUnitAt(command.Target);
                if (target == null || target.Owner == attacker.Owner)
                {
                    state.AddEvent(attacker.Definition.Label + attacker.Id + " fired at empty ground.");
                    continue;
                }

                if (!pendingDamage.ContainsKey(target.Id))
                {
                    pendingDamage[target.Id] = 0;
                }

                pendingDamage[target.Id] += attacker.Definition.AttackDamage;
                state.AddEvent(attacker.Definition.Label + attacker.Id + " hit " + target.Definition.Label + target.Id + ".");
            }

            foreach (KeyValuePair<int, int> pair in pendingDamage)
            {
                UnitModel target = state.GetUnit(pair.Key);
                if (target != null)
                {
                    target.Health -= pair.Value;
                }
            }
        }

        private static void ResolveMovementAndTurns(GameState state)
        {
            Dictionary<int, GridPosition> desiredMoves = new Dictionary<int, GridPosition>();
            Dictionary<GridPosition, int> destinationCounts = new Dictionary<GridPosition, int>();

            foreach (UnitModel unit in state.AliveUnits)
            {
                GameCommand command = GetCommand(state, unit);
                if (command == null)
                {
                    continue;
                }

                if (command.Type == CommandType.Turn)
                {
                    unit.Facing = command.Facing;
                    state.AddEvent(unit.Definition.Label + unit.Id + " turned " + command.Facing + ".");
                    continue;
                }

                if (command.Type != CommandType.Move)
                {
                    continue;
                }

                if (!IsMoveValid(state, unit, command.Target))
                {
                    state.AddEvent(unit.Definition.Label + unit.Id + " move was invalid.");
                    continue;
                }

                desiredMoves[unit.Id] = command.Target;
                if (!destinationCounts.ContainsKey(command.Target))
                {
                    destinationCounts[command.Target] = 0;
                }

                destinationCounts[command.Target]++;
            }

            foreach (KeyValuePair<int, GridPosition> pair in desiredMoves)
            {
                UnitModel unit = state.GetUnit(pair.Key);
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                GridPosition target = pair.Value;
                if (destinationCounts[target] > 1)
                {
                    state.AddEvent(unit.Definition.Label + unit.Id + " was blocked by traffic.");
                    continue;
                }

                UnitModel occupant = state.GetAliveUnitAt(target);
                if (occupant != null && !desiredMoves.ContainsKey(occupant.Id))
                {
                    state.AddEvent(unit.Definition.Label + unit.Id + " was blocked.");
                    continue;
                }

                unit.Position = target;
                state.AddEvent(unit.Definition.Label + unit.Id + " moved to " + target + ".");
                TryAutoPickup(state, unit);
            }
        }

        private static void ResolveEconomy(GameState state)
        {
            List<UnitModel> workers = new List<UnitModel>();
            foreach (UnitModel unit in state.AliveUnits)
            {
                if (unit.Type == UnitType.Worker)
                {
                    workers.Add(unit);
                }
            }

            for (int i = 0; i < workers.Count; i++)
            {
                UnitModel worker = workers[i];
                GameCommand command = GetCommand(state, worker);
                if (command == null)
                {
                    continue;
                }

                if (command.Type == CommandType.Mine)
                {
                    ResolveMine(state, worker, command.Target);
                }
                else if (command.Type == CommandType.DropResource)
                {
                    ResolveDrop(state, worker, command.Target);
                }
                else if (command.Type == CommandType.Build)
                {
                    ResolveBuild(state, worker, command.BuildType, command.Target);
                }
                else if (command.Type == CommandType.Repair)
                {
                    ResolveRepair(state, worker, command.Target);
                }
            }
        }

        private static void ResolveMine(GameState state, UnitModel worker, GridPosition minePosition)
        {
            if (worker.CarryingResource || !worker.Position.IsAdjacentTo(minePosition) || !state.Mines.ContainsKey(minePosition))
            {
                state.AddEvent("Worker " + worker.Id + " could not mine.");
                return;
            }

            state.Mines[minePosition]--;
            worker.CarryingResource = true;
            state.AddEvent("Worker " + worker.Id + " mined one resource.");

            if (state.Mines[minePosition] <= 0)
            {
                state.Mines.Remove(minePosition);
                state.AddEvent("Mine depleted at " + minePosition + ".");
            }
        }

        private static void ResolveDrop(GameState state, UnitModel worker, GridPosition target)
        {
            if (!worker.CarryingResource || !IsSameOrAdjacent(worker.Position, target) || !state.IsInside(target))
            {
                state.AddEvent("Worker " + worker.Id + " could not drop.");
                return;
            }

            int stack = GetStack(state, target);
            if (stack >= GameState.MaxResourceStack)
            {
                state.AddEvent("Resource stack is full at " + target + ".");
                return;
            }

            AddStack(state, target, 1);
            worker.CarryingResource = false;
            state.AddEvent("Worker " + worker.Id + " dropped one resource.");
        }

        private static void ResolveBuild(GameState state, UnitModel worker, UnitType buildType, GridPosition spawnPosition)
        {
            if (!state.IsInside(spawnPosition) || !worker.Position.IsAdjacentTo(spawnPosition) || state.GetAliveUnitAt(spawnPosition) != null)
            {
                state.AddEvent("Worker " + worker.Id + " could not build there.");
                return;
            }

            UnitDefinition definition = UnitDefinition.All[buildType];
            GridPosition stackPosition;
            if (!TryFindUsableAdjacentStack(state, worker.Position, definition.Cost, out stackPosition))
            {
                state.AddEvent("Worker " + worker.Id + " lacked resources for " + buildType + ".");
                return;
            }

            ConsumeStack(state, stackPosition, definition.Cost);
            Direction facing = worker.Owner == PlayerId.PlayerOne ? Direction.East : Direction.West;
            state.AddUnit(worker.Owner, buildType, spawnPosition, facing);
            state.AddEvent("Worker " + worker.Id + " built " + buildType + ".");
        }

        private static void ResolveRepair(GameState state, UnitModel worker, GridPosition unitPosition)
        {
            if (worker.CarryingResource)
            {
                state.AddEvent("Worker " + worker.Id + " cannot repair while carrying.");
                return;
            }

            UnitModel target = state.GetAliveUnitAt(unitPosition);
            if (target == null || target.Owner != worker.Owner || !worker.Position.IsAdjacentTo(unitPosition))
            {
                state.AddEvent("Worker " + worker.Id + " could not repair.");
                return;
            }

            int maxHealth = target.Definition.MaxHealth;
            if (target.Health >= maxHealth)
            {
                state.AddEvent(target.Definition.Label + target.Id + " is already healthy.");
                return;
            }

            int repairCost = Mathf.FloorToInt(target.Definition.Cost * 0.5f);
            if (repairCost > 0)
            {
                GridPosition stackPosition;
                if (!TryFindUsableAdjacentStack(state, worker.Position, repairCost, out stackPosition))
                {
                    state.AddEvent("Worker " + worker.Id + " lacked repair resources.");
                    return;
                }

                ConsumeStack(state, stackPosition, repairCost);
            }

            target.Health = maxHealth;
            state.AddEvent("Worker " + worker.Id + " repaired " + target.Definition.Label + target.Id + ".");
        }

        private static void ResolveControlPoints(GameState state)
        {
            int p1Count = 0;
            int p2Count = 0;

            for (int i = 0; i < state.ControlPoints.Count; i++)
            {
                ControlPoint point = state.ControlPoints[i];
                int p1Value = 0;
                int p2Value = 0;

                foreach (UnitModel unit in state.AliveUnits)
                {
                    if (unit.Position.ManhattanDistance(point.Position) > 1)
                    {
                        continue;
                    }

                    if (unit.Owner == PlayerId.PlayerOne)
                    {
                        p1Value += unit.Definition.StrategicValue;
                    }
                    else if (unit.Owner == PlayerId.PlayerTwo)
                    {
                        p2Value += unit.Definition.StrategicValue;
                    }
                }

                if (p1Value > p2Value)
                {
                    point.Owner = ControlOwner.PlayerOne;
                }
                else if (p2Value > p1Value)
                {
                    point.Owner = ControlOwner.PlayerTwo;
                }
                else
                {
                    point.Owner = ControlOwner.Neutral;
                }

                if (point.Owner == ControlOwner.PlayerOne)
                {
                    p1Count++;
                }
                else if (point.Owner == ControlOwner.PlayerTwo)
                {
                    p2Count++;
                }
            }

            int difference = p1Count - p2Count;
            if (difference > 0)
            {
                int gain = difference >= 2 ? 2 : 1;
                state.Influence[PlayerId.PlayerOne] += gain;
                state.AddEvent("Player One gained " + gain + " Influence.");
            }
            else if (difference < 0)
            {
                int gain = -difference >= 2 ? 2 : 1;
                state.Influence[PlayerId.PlayerTwo] += gain;
                state.AddEvent("Player Two gained " + gain + " Influence.");
            }

            if (state.Influence[PlayerId.PlayerOne] >= GameState.InfluenceToWin)
            {
                state.Winner = PlayerId.PlayerOne;
                state.AddEvent("Player One wins by Influence.");
            }
            else if (state.Influence[PlayerId.PlayerTwo] >= GameState.InfluenceToWin)
            {
                state.Winner = PlayerId.PlayerTwo;
                state.AddEvent("Player Two wins by Influence.");
            }
        }

        public static void RefreshFog(GameState state)
        {
            RefreshFogForPlayer(state, PlayerId.PlayerOne);
            RefreshFogForPlayer(state, PlayerId.PlayerTwo);
        }

        private static void RefreshFogForPlayer(GameState state, PlayerId player)
        {
            FogState fog = state.Fog[player];
            fog.ClearVisibleCells();

            foreach (UnitModel unit in state.AliveUnits)
            {
                if (unit.Owner != player)
                {
                    continue;
                }

                int range = unit.Definition.VisionRange;
                for (int x = unit.Position.X - range; x <= unit.Position.X + range; x++)
                {
                    for (int y = unit.Position.Y - range; y <= unit.Position.Y + range; y++)
                    {
                        GridPosition position = new GridPosition(x, y);
                        if (state.IsInside(position) && unit.Position.ManhattanDistance(position) <= range)
                        {
                            fog.VisibleCells.Add(position);
                        }
                    }
                }
            }

            foreach (UnitModel unit in state.AliveUnits)
            {
                if (unit.Owner == player)
                {
                    continue;
                }

                if (fog.VisibleCells.Contains(unit.Position))
                {
                    fog.LastKnownEnemies[unit.Id] = unit.CloneAsLastKnown();
                }
            }
        }

        public static int GetStack(GameState state, GridPosition position)
        {
            int stack;
            return state.ResourceStacks.TryGetValue(position, out stack) ? stack : 0;
        }

        public static void AddStack(GameState state, GridPosition position, int amount)
        {
            int current = GetStack(state, position);
            int next = Mathf.Clamp(current + amount, 0, GameState.MaxResourceStack);
            if (next <= 0)
            {
                state.ResourceStacks.Remove(position);
            }
            else
            {
                state.ResourceStacks[position] = next;
            }
        }

        private static void ConsumeStack(GameState state, GridPosition position, int amount)
        {
            AddStack(state, position, -amount);
        }

        private static void TryAutoPickup(GameState state, UnitModel unit)
        {
            if (unit.Type != UnitType.Worker || unit.CarryingResource)
            {
                return;
            }

            int stack = GetStack(state, unit.Position);
            if (stack <= 0)
            {
                return;
            }

            ConsumeStack(state, unit.Position, 1);
            unit.CarryingResource = true;
            state.AddEvent("Worker " + unit.Id + " picked up one resource.");
        }

        private static void RemoveDeadUnits(GameState state)
        {
            for (int i = 0; i < state.Units.Count; i++)
            {
                UnitModel unit = state.Units[i];
                if (unit.Health <= 0 && unit.Health > -999)
                {
                    unit.Health = -1000;
                    if (unit.CarryingResource)
                    {
                        AddStack(state, unit.Position, 1);
                    }

                    state.AddEvent(unit.Definition.Label + unit.Id + " was destroyed.");
                }
            }
        }

        private static GameCommand GetCommand(GameState state, UnitModel unit)
        {
            Dictionary<int, GameCommand> commands = state.PlannedCommands[unit.Owner];
            GameCommand command;
            return commands.TryGetValue(unit.Id, out command) ? command : null;
        }

        private static bool TryFindUsableAdjacentStack(GameState state, GridPosition workerPosition, int cost, out GridPosition stackPosition)
        {
            foreach (GridPosition position in AdjacentAndSame(workerPosition))
            {
                if (GetStack(state, position) >= cost)
                {
                    stackPosition = position;
                    return true;
                }
            }

            stackPosition = new GridPosition(-1, -1);
            return false;
        }

        private static IEnumerable<GridPosition> AdjacentAndSame(GridPosition position)
        {
            yield return position;
            yield return new GridPosition(position.X + 1, position.Y);
            yield return new GridPosition(position.X - 1, position.Y);
            yield return new GridPosition(position.X, position.Y + 1);
            yield return new GridPosition(position.X, position.Y - 1);
        }

        private static bool IsSameOrAdjacent(GridPosition a, GridPosition b)
        {
            return a == b || a.IsAdjacentTo(b);
        }

        private static bool IsInsideFacingCone(GridPosition origin, GridPosition target, Direction facing)
        {
            int dx = target.X - origin.X;
            int dy = target.Y - origin.Y;

            if (facing == Direction.North)
            {
                return dy > 0 && Mathf.Abs(dx) <= dy;
            }

            if (facing == Direction.South)
            {
                return dy < 0 && Mathf.Abs(dx) <= -dy;
            }

            if (facing == Direction.East)
            {
                return dx > 0 && Mathf.Abs(dy) <= dx;
            }

            return dx < 0 && Mathf.Abs(dy) <= -dx;
        }
    }
}

