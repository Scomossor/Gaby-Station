# SPDX-FileCopyrightText: 2026 Project Dumont
#
# SPDX-License-Identifier: AGPL-3.0-or-later

ent-ComputerScienceXenobiology = xenobiology console
    .desc = Remotely monitors and handles slimes through linked cameras.

ent-XenobiologyConsoleComputerCircuitboard = xenobiology console computer board
    .desc = A computer printed circuit board for a xenobiology console.

ent-SurveillanceCameraScienceXenobiology = camera
    .desc = A science surveillance camera linked to xenobiology consoles.
    .suffix = Science, Xenobiology

ent-XenobiologyConsoleEye = xenobiology console camera
    .desc = The remote viewpoint of a xenobiology console.

ent-ActionXenobiologyConsoleExit = Disconnect from console
    .desc = Return control to your body.

ent-ActionXenobiologyConsolePlaceMonkey = Place monkey
    .desc = Spend stored monkey biomass to place a monkey at the cursor position.

ent-ActionXenobiologyConsoleRecycleMonkey = Recycle monkey
    .desc = Recycle the closest dead monkey into biomass.

ent-ActionXenobiologyConsoleGrabSlime = Store slime
    .desc = Store the closest slime or transfer its corpse to a linked grinder.

ent-ActionXenobiologyConsoleReleaseSlimes = Release slimes
    .desc = Release every stored slime at the cursor position.

ent-ActionXenobiologyConsoleAnalyzeSlime = Analyze slime
    .desc = Analyze the closest slime to the cursor.

ent-ActionXenobiologyConsoleShowShortcuts = Show shortcuts
    .desc = Print the console mouse shortcuts in chat.

xenobiology-console-in-use = The console is already in use.
xenobiology-console-no-access = You do not have access to this console.
xenobiology-console-no-cameras = There are no active xenobiology cameras linked to this console.
xenobiology-console-connected = Console connected.
xenobiology-console-cube-inserted = Monkey cube inserted. Biomass: {$amount}.
xenobiology-console-no-biomass = There is not enough monkey biomass.
xenobiology-console-monkey-placed = Monkey placed. Biomass: {$amount}.
xenobiology-console-no-monkey = There is no monkey close enough to the cursor.
xenobiology-console-monkey-alive = The monkey must be dead before recycling.
xenobiology-console-monkey-recycled = Monkey recycled. Biomass: {$amount}.
xenobiology-console-slime-storage-full = Slime storage is full ({$amount}).
xenobiology-console-no-slime = There is no slime close enough to the cursor.
xenobiology-console-slime-grab-failed = The slime could not be stored.
xenobiology-console-slime-grabbed = Slime stored ({$amount}/{$capacity}).
xenobiology-console-slime-sent-to-grinder = Slime corpse transferred to the linked grinder.
xenobiology-console-no-stored-slimes = The console is not holding any slimes.
xenobiology-console-slimes-released = Slimes released: {$amount}.
xenobiology-console-shortcuts-chat =
    Xenobiology console shortcuts:
    Shift + click a slime: store it.
    Shift + click the floor: release all stored slimes.
    Ctrl + click a slime: analyze it.
    Ctrl + click a dead monkey: recycle it.
    Ctrl + click the floor: place a monkey.

signal-port-name-xenobiology-slime-transfer = Slime transfer
signal-port-description-xenobiology-slime-transfer = Transfers collected slime corpses to a linked grinder.
signal-port-name-xenobiology-slime-receiver = Slime receiver
signal-port-description-xenobiology-slime-receiver = Receives slime corpses from a xenobiology console.
