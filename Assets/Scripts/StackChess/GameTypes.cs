namespace StackChess
{
    public enum PlayerId
    {
        None = 0,
        PlayerOne = 1,
        PlayerTwo = 2
    }

    public enum UnitType
    {
        Worker,
        Infantry,
        ArmoredCar,
        Tank
    }

    public enum CommandType
    {
        None,
        Move,
        Attack,
        Turn,
        Mine,
        DropResource,
        Build,
        Repair
    }

    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public enum ControlOwner
    {
        Neutral,
        PlayerOne,
        PlayerTwo
    }

    public enum PlanningAction
    {
        Move,
        Attack,
        Turn,
        Mine,
        DropResource,
        Repair,
        BuildWorker,
        BuildInfantry,
        BuildArmoredCar,
        BuildTank
    }
}

