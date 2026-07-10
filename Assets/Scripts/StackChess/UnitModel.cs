namespace StackChess
{
    public sealed class UnitModel
    {
        public int Id;
        public PlayerId Owner;
        public UnitType Type;
        public GridPosition Position;
        public int Health;
        public Direction Facing;
        public bool CarryingResource;

        public UnitDefinition Definition
        {
            get { return UnitDefinition.All[Type]; }
        }

        public bool IsAlive
        {
            get { return Health > 0; }
        }

        public UnitModel CloneAsLastKnown()
        {
            return new UnitModel
            {
                Id = Id,
                Owner = Owner,
                Type = Type,
                Position = Position,
                Health = Health,
                Facing = Facing,
                CarryingResource = CarryingResource
            };
        }
    }
}

