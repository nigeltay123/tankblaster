Tankblaster: Procedural Dungeon Generation Roguelike Prototype
Overview

Tankblaster is a 2D top-down roguelike prototype developed as part of my final year project.
The project corresponds to Project Idea 6.2: Procedural Dungeon Generation in Roguelike Games from the official project template.

The aim of this prototype is to demonstrate how procedural content generation (PCG) can be used to create replayable, dynamic game levels while balancing difficulty scaling and player progression. The project combines algorithm design (Binary Space Partitioning), core gameplay systems, and simple AI to illustrate the technical and creative applications of PCG in game development.

Key Features

Procedural Dungeon Generation

Binary Space Partitioning (BSP) algorithm for room and corridor creation.

Scalable dungeon size and complexity based on level progression.

Structured layouts that balance randomness with playability.

Gameplay Systems

Player-controlled tank with movement and shooting. (WASD)(right click)

Enemy AI with patrol and engage states (using a Finite State Machine).

Player health system and visual HUD.

Progression & Upgrades

Difficulty scaling: enemy count and dungeon size increase per level.

Every 5 levels, players choose upgrades (+Health or +Damage).

Persistent upgrades applied across levels to reinforce progression.

Game Over & Restart

Proper restart handling when the player dies.

Upgrade and health systems reset appropriately.

Requirements

Unity Version: 2021.3 LTS (or newer)

Language: C#

Packages:

TextMeshPro (for HUD)

2D Tilemap Editor (for dungeon generation)
