# Stack Chess

Stack Chess is a Unity 2D prototype for a simultaneous-turn tactical strategy game built around logistics, prediction, fog of war, and map control.

## Prototype v0.1

This first version focuses on validating the core loop described in [`docs/PRD_v0.1.md`](docs/PRD_v0.1.md):

- 12 x 12 grid map
- Hot-seat two-player planning
- Simultaneous turn resolution
- Workers, infantry, armored cars, and tanks
- Physical resource stacks and worker carrying
- Mining, dropping, building, and repairing
- Fog of war with last known enemy positions
- Three control points and Influence victory at 30

## How to Run

1. Open this folder with Unity 2022.3 LTS or newer.
2. Open or create any empty scene.
3. Press Play.

The prototype bootstraps itself at runtime, so no prefab setup is required.

## Controls

- Left click: select friendly unit or assign the current action target.
- Tab: switch planning/view player.
- On-screen buttons: choose actions, submit each player's orders, resolve when both players are ready.
- Tank facing uses the N/E/S/W buttons.

## First Turn Example

1. Choose Player 1, select the worker, choose Move, then click a nearby resource stack to pick up a resource automatically after movement.
2. Submit Player 1 orders.
3. Choose Player 2 and do the same.
4. Click Resolve Turn.
5. On later turns, workers can Mine adjacent mines, Drop carried resources, Build next to resource stacks, and Repair adjacent damaged friendly units.

## Current Rule Notes

- Movement uses Manhattan distance and resolves simultaneously.
- Attacks resolve before movement. Tanks only attack inside their facing cone.
- Workers can hold one resource. Moving onto a resource stack auto-picks one resource.
- Building consumes an adjacent resource stack with enough height. Costs: Worker 1, Infantry 1, Armored Car 2, Tank 3.
- Control points count friendly/enemy unit value within Manhattan distance 1.
- Fog hides enemy units outside vision and keeps their last known position as a ghost marker.

This is a rules prototype using generated square sprites and text labels. Art, animation, audio, networking, AI, and campaign systems are intentionally out of scope for v0.1.
