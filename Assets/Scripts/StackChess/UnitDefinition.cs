using System.Collections.Generic;

namespace StackChess
{
    public sealed class UnitDefinition
    {
        public readonly UnitType Type;
        public readonly string Label;
        public readonly int Cost;
        public readonly int MaxHealth;
        public readonly int MoveRange;
        public readonly int VisionRange;
        public readonly int AttackRange;
        public readonly int AttackDamage;
        public readonly int StrategicValue;
        public readonly bool UsesFacing;

        private UnitDefinition(
            UnitType type,
            string label,
            int cost,
            int maxHealth,
            int moveRange,
            int visionRange,
            int attackRange,
            int attackDamage,
            int strategicValue,
            bool usesFacing)
        {
            Type = type;
            Label = label;
            Cost = cost;
            MaxHealth = maxHealth;
            MoveRange = moveRange;
            VisionRange = visionRange;
            AttackRange = attackRange;
            AttackDamage = attackDamage;
            StrategicValue = strategicValue;
            UsesFacing = usesFacing;
        }

        public static readonly Dictionary<UnitType, UnitDefinition> All = new Dictionary<UnitType, UnitDefinition>
        {
            { UnitType.Worker, new UnitDefinition(UnitType.Worker, "W", 1, 5, 5, 3, 0, 0, 1, false) },
            { UnitType.Infantry, new UnitDefinition(UnitType.Infantry, "I", 1, 6, 3, 3, 1, 2, 1, false) },
            { UnitType.ArmoredCar, new UnitDefinition(UnitType.ArmoredCar, "C", 2, 8, 6, 4, 2, 2, 2, false) },
            { UnitType.Tank, new UnitDefinition(UnitType.Tank, "T", 3, 12, 2, 3, 4, 5, 3, true) }
        };
    }
}

