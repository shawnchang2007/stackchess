using System.Collections.Generic;
using UnityEngine;

namespace StackChess
{
    public sealed class StackChessPrototype : MonoBehaviour
    {
        private const float CellSize = 1f;
        private const float BoardLeft = -GameState.BoardWidth * 0.5f;
        private const float BoardBottom = -GameState.BoardHeight * 0.5f;

        private GameState state;
        private Sprite squareSprite;
        private Transform boardRoot;
        private Transform markerRoot;
        private PlayerId planningPlayer = PlayerId.PlayerOne;
        private PlanningAction planningAction = PlanningAction.Move;
        private int selectedUnitId = -1;
        private bool needsRedraw = true;
        private Vector2 logScroll;

        private readonly Color p1Color = new Color(0.25f, 0.55f, 1f);
        private readonly Color p2Color = new Color(1f, 0.35f, 0.28f);
        private readonly Color fogColor = new Color(0.08f, 0.08f, 0.09f);
        private readonly Color visibleA = new Color(0.46f, 0.50f, 0.47f);
        private readonly Color visibleB = new Color(0.39f, 0.43f, 0.41f);
        private readonly Color selectedColor = new Color(1f, 0.92f, 0.25f);

        private void Awake()
        {
            EnsureCamera();
            squareSprite = CreateSquareSprite();
            state = StackChessRules.CreateNewGame();
            boardRoot = new GameObject("Board").transform;
            markerRoot = new GameObject("Markers").transform;
            Redraw();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SwitchPlanningPlayer();
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }

            if (needsRedraw)
            {
                Redraw();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("Stack Chess Prototype v0.1");
            GUILayout.Label("Turn " + state.TurnNumber + " | Viewing: " + PlayerLabel(planningPlayer));
            GUILayout.Label("Influence P1 " + state.Influence[PlayerId.PlayerOne] + " / P2 " + state.Influence[PlayerId.PlayerTwo]);

            if (state.Winner != PlayerId.None)
            {
                GUILayout.Space(8);
                GUILayout.Label(PlayerLabel(state.Winner) + " wins by Influence.");
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Player 1"))
            {
                planningPlayer = PlayerId.PlayerOne;
                selectedUnitId = -1;
                needsRedraw = true;
            }

            if (GUILayout.Button("Player 2"))
            {
                planningPlayer = PlayerId.PlayerTwo;
                selectedUnitId = -1;
                needsRedraw = true;
            }
            GUILayout.EndHorizontal();

            bool submitted = state.SubmittedPlayers.Contains(planningPlayer);
            GUILayout.Label(submitted ? "Orders submitted" : "Planning orders");

            DrawSelectedUnitPanel(submitted);
            DrawActionButtons(submitted);
            DrawSubmitPanel(submitted);
            DrawControlPointPanel();
            DrawCommandPanel();
            DrawLogPanel();
            GUILayout.EndArea();
        }

        private void DrawSelectedUnitPanel(bool submitted)
        {
            GUILayout.Space(8);
            UnitModel selected = state.GetUnit(selectedUnitId);
            if (selected == null || !selected.IsAlive)
            {
                GUILayout.Label("Selected: none");
                return;
            }

            GUILayout.Label(
                "Selected: " + selected.Definition.Label + selected.Id +
                " " + selected.Type +
                " HP " + selected.Health + "/" + selected.Definition.MaxHealth +
                " @ " + selected.Position +
                (selected.CarryingResource ? " carrying R" : ""));

            if (selected.Type == UnitType.Tank)
            {
                GUILayout.Label("Facing: " + selected.Facing);
            }

            if (submitted)
            {
                GUILayout.Label("This player already submitted orders.");
            }
        }

        private void DrawActionButtons(bool submitted)
        {
            GUI.enabled = !submitted && state.Winner == PlayerId.None;
            GUILayout.Space(8);
            GUILayout.Label("Action");

            GUILayout.BeginHorizontal();
            ActionButton("Move", PlanningAction.Move);
            ActionButton("Attack", PlanningAction.Attack);
            ActionButton("Mine", PlanningAction.Mine);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            ActionButton("Drop", PlanningAction.DropResource);
            ActionButton("Repair", PlanningAction.Repair);
            ActionButton("Turn", PlanningAction.Turn);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            ActionButton("Build W", PlanningAction.BuildWorker);
            ActionButton("Build I", PlanningAction.BuildInfantry);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            ActionButton("Build Car", PlanningAction.BuildArmoredCar);
            ActionButton("Build Tank", PlanningAction.BuildTank);
            GUILayout.EndHorizontal();

            if (planningAction == PlanningAction.Turn)
            {
                GUILayout.Label("Tank/units turn direction");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("N")) QueueTurn(Direction.North);
                if (GUILayout.Button("E")) QueueTurn(Direction.East);
                if (GUILayout.Button("S")) QueueTurn(Direction.South);
                if (GUILayout.Button("W")) QueueTurn(Direction.West);
                GUILayout.EndHorizontal();
            }

            GUI.enabled = true;
            GUILayout.Label("Current action: " + planningAction);
        }

        private void DrawSubmitPanel(bool submitted)
        {
            GUILayout.Space(8);
            GUI.enabled = state.Winner == PlayerId.None;
            if (!submitted && GUILayout.Button("Submit " + PlayerLabel(planningPlayer) + " Orders"))
            {
                state.SubmittedPlayers.Add(planningPlayer);
                selectedUnitId = -1;
                state.AddEvent(PlayerLabel(planningPlayer) + " submitted orders.");
                needsRedraw = true;
            }

            GUI.enabled = StackChessRules.CanResolve(state);
            if (GUILayout.Button("Resolve Turn"))
            {
                StackChessRules.ResolveTurn(state);
                selectedUnitId = -1;
                needsRedraw = true;
            }

            GUI.enabled = true;
            if (GUILayout.Button("Reset Current Orders"))
            {
                if (!state.SubmittedPlayers.Contains(planningPlayer))
                {
                    state.PlannedCommands[planningPlayer].Clear();
                    state.AddEvent(PlayerLabel(planningPlayer) + " reset orders.");
                    needsRedraw = true;
                }
            }
        }

        private void DrawControlPointPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("Control Points");
            for (int i = 0; i < state.ControlPoints.Count; i++)
            {
                ControlPoint point = state.ControlPoints[i];
                GUILayout.Label("CP " + (i + 1) + " " + point.Position + " " + point.Owner);
            }
        }

        private void DrawCommandPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("Queued Orders");
            GUILayout.TextArea(state.DescribeCommands(planningPlayer), GUILayout.Height(84));
        }

        private void DrawLogPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("Event Log");
            logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(170));
            for (int i = 0; i < state.EventLog.Count; i++)
            {
                GUILayout.Label(state.EventLog[i]);
            }
            GUILayout.EndScrollView();
        }

        private void ActionButton(string label, PlanningAction action)
        {
            Color previousColor = GUI.backgroundColor;
            if (planningAction == action)
            {
                GUI.backgroundColor = new Color(0.95f, 0.82f, 0.28f);
            }

            if (GUILayout.Button(label))
            {
                planningAction = action;
            }

            GUI.backgroundColor = previousColor;
        }

        private void HandleLeftClick()
        {
            if (Input.mousePosition.x <= 380f)
            {
                return;
            }

            GridPosition cell;
            if (!TryGetMouseGridPosition(out cell))
            {
                return;
            }

            UnitModel clickedUnit = state.GetAliveUnitAt(cell);
            if (clickedUnit != null && clickedUnit.Owner == planningPlayer && !state.SubmittedPlayers.Contains(planningPlayer))
            {
                selectedUnitId = clickedUnit.Id;
                needsRedraw = true;
                return;
            }

            if (selectedUnitId < 0 || state.SubmittedPlayers.Contains(planningPlayer) || state.Winner != PlayerId.None)
            {
                return;
            }

            QueueTargetedCommand(cell);
        }

        private void QueueTargetedCommand(GridPosition target)
        {
            UnitModel selected = state.GetUnit(selectedUnitId);
            if (selected == null || !selected.IsAlive || selected.Owner != planningPlayer)
            {
                return;
            }

            GameCommand command = new GameCommand
            {
                UnitId = selected.Id,
                Target = target,
                Type = CommandType.None
            };

            if (planningAction == PlanningAction.Move)
            {
                command.Type = CommandType.Move;
            }
            else if (planningAction == PlanningAction.Attack)
            {
                command.Type = CommandType.Attack;
            }
            else if (planningAction == PlanningAction.Mine)
            {
                command.Type = CommandType.Mine;
            }
            else if (planningAction == PlanningAction.DropResource)
            {
                command.Type = CommandType.DropResource;
            }
            else if (planningAction == PlanningAction.Repair)
            {
                command.Type = CommandType.Repair;
            }
            else if (IsBuildAction(planningAction))
            {
                command.Type = CommandType.Build;
                command.BuildType = BuildTypeFromAction(planningAction);
            }

            if (command.Type != CommandType.None)
            {
                StackChessRules.QueueCommand(state, planningPlayer, command);
                state.AddEvent(PlayerLabel(planningPlayer) + " queued " + command.Type + " for " + selected.Definition.Label + selected.Id + ".");
                needsRedraw = true;
            }
        }

        private void QueueTurn(Direction direction)
        {
            UnitModel selected = state.GetUnit(selectedUnitId);
            if (selected == null || !selected.IsAlive || selected.Owner != planningPlayer)
            {
                return;
            }

            StackChessRules.QueueCommand(state, planningPlayer, new GameCommand
            {
                UnitId = selected.Id,
                Type = CommandType.Turn,
                Facing = direction
            });
            state.AddEvent(PlayerLabel(planningPlayer) + " queued turn for " + selected.Definition.Label + selected.Id + ".");
            needsRedraw = true;
        }

        private void Redraw()
        {
            ClearChildren(boardRoot);
            ClearChildren(markerRoot);
            DrawBoard();
            DrawControlPoints();
            DrawResourcesAndMines();
            DrawUnits();
            needsRedraw = false;
        }

        private void DrawBoard()
        {
            for (int x = 0; x < GameState.BoardWidth; x++)
            {
                for (int y = 0; y < GameState.BoardHeight; y++)
                {
                    GridPosition position = new GridPosition(x, y);
                    bool visible = StackChessRules.IsVisibleTo(state, planningPlayer, position);
                    Color color = visible ? (((x + y) % 2 == 0) ? visibleA : visibleB) : fogColor;

                    UnitModel selected = state.GetUnit(selectedUnitId);
                    if (selected != null && selected.IsAlive && selected.Position == position)
                    {
                        color = selectedColor;
                    }

                    CreateSquare("Cell " + x + "," + y, position, color, -1, boardRoot, 0.96f);
                }
            }
        }

        private void DrawControlPoints()
        {
            for (int i = 0; i < state.ControlPoints.Count; i++)
            {
                ControlPoint point = state.ControlPoints[i];
                Color color = new Color(0.95f, 0.82f, 0.22f, 0.65f);
                if (point.Owner == ControlOwner.PlayerOne)
                {
                    color = new Color(p1Color.r, p1Color.g, p1Color.b, 0.65f);
                }
                else if (point.Owner == ControlOwner.PlayerTwo)
                {
                    color = new Color(p2Color.r, p2Color.g, p2Color.b, 0.65f);
                }

                CreateSquare("ControlPoint", point.Position, color, 0, markerRoot, 0.52f);
                CreateText("CP", point.Position, Color.black, 2, 0.22f, markerRoot, new Vector3(0f, -0.18f, 0f));
            }
        }

        private void DrawResourcesAndMines()
        {
            foreach (KeyValuePair<GridPosition, int> pair in state.ResourceStacks)
            {
                CreateText("R" + pair.Value, pair.Key, new Color(0.4f, 1f, 0.45f), 5, 0.24f, markerRoot, new Vector3(0f, 0.23f, 0f));
            }

            foreach (KeyValuePair<GridPosition, int> pair in state.Mines)
            {
                CreateText("M" + pair.Value, pair.Key, new Color(0.7f, 0.95f, 1f), 5, 0.24f, markerRoot, new Vector3(0f, -0.23f, 0f));
            }
        }

        private void DrawUnits()
        {
            foreach (UnitModel unit in state.AliveUnits)
            {
                bool friendly = unit.Owner == planningPlayer;
                bool visible = StackChessRules.IsVisibleTo(state, planningPlayer, unit.Position);
                if (!friendly && !visible)
                {
                    continue;
                }

                Color color = unit.Owner == PlayerId.PlayerOne ? p1Color : p2Color;
                CreateSquare("Unit " + unit.Id, unit.Position, color, 4, markerRoot, 0.68f);
                DrawUnitLabel(unit, Color.white, false);
            }

            FogState fog = state.Fog[planningPlayer];
            foreach (KeyValuePair<int, UnitModel> pair in fog.LastKnownEnemies)
            {
                UnitModel lastKnown = pair.Value;
                UnitModel live = state.GetUnit(lastKnown.Id);
                if (live == null || !live.IsAlive || StackChessRules.IsVisibleTo(state, planningPlayer, live.Position))
                {
                    continue;
                }

                CreateSquare("LastKnown " + lastKnown.Id, lastKnown.Position, new Color(0.85f, 0.85f, 0.85f, 0.35f), 3, markerRoot, 0.58f);
                DrawUnitLabel(lastKnown, Color.black, true);
            }
        }

        private void DrawUnitLabel(UnitModel unit, Color textColor, bool lastKnown)
        {
            string label = unit.Definition.Label + unit.Id;
            if (unit.Type == UnitType.Tank)
            {
                label += DirectionGlyph(unit.Facing);
            }

            if (unit.CarryingResource)
            {
                label += "+R";
            }

            if (lastKnown)
            {
                label += "?";
            }

            CreateText(label, unit.Position, textColor, 6, 0.22f, markerRoot, Vector3.zero);
            CreateText(unit.Health.ToString(), unit.Position, Color.white, 6, 0.16f, markerRoot, new Vector3(0f, -0.28f, 0f));
        }

        private GameObject CreateSquare(string name, GridPosition position, Color color, int sortingOrder, Transform parent, float scale)
        {
            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent);
            instance.transform.position = GridToWorld(position);
            instance.transform.localScale = new Vector3(CellSize * scale, CellSize * scale, 1f);
            SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return instance;
        }

        private TextMesh CreateText(string text, GridPosition position, Color color, int sortingOrder, float size, Transform parent, Vector3 offset)
        {
            GameObject instance = new GameObject("Text " + text);
            instance.transform.SetParent(parent);
            instance.transform.position = GridToWorld(position) + offset + new Vector3(0f, 0f, -0.1f);
            TextMesh mesh = instance.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            return mesh;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Vector3 GridToWorld(GridPosition position)
        {
            return new Vector3(BoardLeft + position.X + 0.5f, BoardBottom + position.Y + 0.5f, 0f);
        }

        private bool TryGetMouseGridPosition(out GridPosition position)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(world.x - BoardLeft);
            int y = Mathf.FloorToInt(world.y - BoardBottom);
            position = new GridPosition(x, y);
            return state.IsInside(position);
        }

        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.transform.position = new Vector3(1.1f, 0f, -10f);
            camera.backgroundColor = new Color(0.11f, 0.12f, 0.13f);
        }

        private void SwitchPlanningPlayer()
        {
            planningPlayer = StackChessRules.OtherPlayer(planningPlayer);
            selectedUnitId = -1;
            needsRedraw = true;
        }

        private static bool IsBuildAction(PlanningAction action)
        {
            return action == PlanningAction.BuildWorker ||
                   action == PlanningAction.BuildInfantry ||
                   action == PlanningAction.BuildArmoredCar ||
                   action == PlanningAction.BuildTank;
        }

        private static UnitType BuildTypeFromAction(PlanningAction action)
        {
            if (action == PlanningAction.BuildWorker)
            {
                return UnitType.Worker;
            }

            if (action == PlanningAction.BuildInfantry)
            {
                return UnitType.Infantry;
            }

            if (action == PlanningAction.BuildArmoredCar)
            {
                return UnitType.ArmoredCar;
            }

            return UnitType.Tank;
        }

        private static string PlayerLabel(PlayerId player)
        {
            if (player == PlayerId.PlayerOne)
            {
                return "Player One";
            }

            if (player == PlayerId.PlayerTwo)
            {
                return "Player Two";
            }

            return "No Player";
        }

        private static string DirectionGlyph(Direction direction)
        {
            if (direction == Direction.North)
            {
                return "^";
            }

            if (direction == Direction.South)
            {
                return "v";
            }

            if (direction == Direction.East)
            {
                return ">";
            }

            return "<";
        }
    }
}

