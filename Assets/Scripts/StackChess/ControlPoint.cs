namespace StackChess
{
    public sealed class ControlPoint
    {
        public GridPosition Position;
        public ControlOwner Owner;

        public ControlPoint(GridPosition position)
        {
            Position = position;
            Owner = ControlOwner.Neutral;
        }
    }
}

