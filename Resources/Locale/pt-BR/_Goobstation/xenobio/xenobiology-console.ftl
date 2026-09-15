# SPDX-FileCopyrightText: 2026 Project Dumont
#
# SPDX-License-Identifier: AGPL-3.0-or-later

ent-ComputerScienceXenobiology = console de xenobiologia
    .desc = Monitora e manipula slimes remotamente por meio das câmeras vinculadas.

ent-XenobiologyConsoleComputerCircuitboard = placa do console de xenobiologia
    .desc = Uma placa de circuito impresso para um console de xenobiologia.

ent-SurveillanceCameraScienceXenobiology = câmera
    .desc = Uma câmera de vigilância científica vinculada aos consoles de xenobiologia.
    .suffix = Ciência, Xenobiologia

ent-XenobiologyConsoleEye = câmera do console de xenobiologia
    .desc = O ponto de visão remoto de um console de xenobiologia.

ent-ActionXenobiologyConsoleExit = Desconectar do console
    .desc = Retorna o controle ao seu corpo.

ent-ActionXenobiologyConsolePlaceMonkey = Posicionar macaco
    .desc = Consome biomassa de macaco e posiciona um macaco no indicador.

ent-ActionXenobiologyConsoleRecycleMonkey = Reciclar macaco
    .desc = Recicla o macaco morto mais próximo em biomassa.

ent-ActionXenobiologyConsoleGrabSlime = Armazenar slime
    .desc = Armazena o slime mais próximo ou transfere seu corpo para um triturador vinculado.

ent-ActionXenobiologyConsoleReleaseSlimes = Soltar slimes
    .desc = Solta todos os slimes armazenados na posição do indicador.

ent-ActionXenobiologyConsoleAnalyzeSlime = Analisar slime
    .desc = Analisa o slime mais próximo do indicador.

ent-ActionXenobiologyConsoleShowShortcuts = Mostrar atalhos
    .desc = Exibe no chat os atalhos de mouse do console.

xenobiology-console-in-use = O console já está em uso.
xenobiology-console-no-access = Você não tem acesso a este console.
xenobiology-console-no-cameras = Não há câmeras de xenobiologia ativas vinculadas a este console.
xenobiology-console-connected = Console conectado.
xenobiology-console-cube-inserted = Cubo de macaco inserido. Biomassa: {$amount}.
xenobiology-console-no-biomass = Não há biomassa de macaco suficiente.
xenobiology-console-monkey-placed = Macaco posicionado. Biomassa: {$amount}.
xenobiology-console-no-monkey = Não há um macaco perto o suficiente do indicador.
xenobiology-console-monkey-alive = O macaco precisa estar morto para ser reciclado.
xenobiology-console-monkey-recycled = Macaco reciclado. Biomassa: {$amount}.
xenobiology-console-slime-storage-full = O armazenamento de slimes está cheio ({$amount}).
xenobiology-console-no-slime = Não há um slime perto o suficiente do indicador.
xenobiology-console-slime-grab-failed = Não foi possível armazenar o slime.
xenobiology-console-slime-grabbed = Slime armazenado ({$amount}/{$capacity}).
xenobiology-console-slime-sent-to-grinder = Corpo de slime transferido para o triturador vinculado.
xenobiology-console-no-stored-slimes = O console não está armazenando slimes.
xenobiology-console-slimes-released = Slimes soltos: {$amount}.
xenobiology-console-shortcuts-chat =
    Atalhos do console de xenobiologia:
    Shift + clique em um slime: armazená-lo.
    Shift + clique no chão: soltar todos os slimes armazenados.
    Ctrl + clique em um slime: analisá-lo.
    Ctrl + clique em um macaco morto: reciclá-lo.
    Ctrl + clique no chão: posicionar um macaco.

signal-port-name-xenobiology-slime-transfer = Transferência de slimes
signal-port-description-xenobiology-slime-transfer = Transfere corpos de slimes coletados para um triturador vinculado.
signal-port-name-xenobiology-slime-receiver = Receptor de slimes
signal-port-description-xenobiology-slime-receiver = Recebe corpos de slimes de um console de xenobiologia.
