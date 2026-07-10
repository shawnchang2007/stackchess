namespace StackChess
{
    public sealed class GameCommand
    {
        public CommandType Type;
        public int UnitId;
        public GridPosition Target;
        public Direction Facing;
        public UnitType BuildType;

        public static GameCommand None(int unitId)
        {
            return new GameCommand { Type = CommandType.None, UnitId = unitId };
        }

        public override string ToString()
        {
            if (Type == CommandType.Turn)
            {
                return UnitId + ": " + Type + " " + Facing;
            }

            if (Type == CommandType.Build)
            {
                return UnitId + ": Build " + BuildType + " @ " + Target;
            }

            return UnitId + ": " + Type + " @ " + Target;
        }
    }
}

