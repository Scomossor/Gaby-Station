# Loading Screen

replay-loading = Carregando ({$cur}/{$total})
replay-loading-reading = Lendo Arquivos
replay-loading-processing = Processando Arquivos
replay-loading-spawning = Espanando Entidades
replay-loading-initializing = Inicializando Entidades
replay-loading-starting= Começando Entities
replay-loading-failed = Falha ao carregar o replay:
                        {$reason}
replay-loading-retry = Tente carregar com mais tolerância a exceções - _pode causar bugs_!

# Main Menu
replay-menu-subtext = Cliente de Replay
replay-menu-load = Carrega Replay Selecionado
replay-menu-select = Seleciona um Replay
replay-menu-open = Abre Pasta de Replays
replay-menu-none = Não foram encontrados replays.

# Main Menu Info Box
replay-info-title = Replay Information
replay-info-none-selected = Nenhum replay selecionado
replay-info-invalid = [color=red]Replay inválido selecionado[/color]
replay-info-info = {"["}color=gray]Selecionado:[/color]  {$name} ({$file})
                   {"["}color=gray]Tempo:[/color]   {$time}
                   {"["}color=gray]ID do Round:[/color]   {$roundId}
                   {"["}color=gray]Duração:[/color]   {$duration}
                   {"["}color=gray]ForkId:[/color]   {$forkId}
                   {"["}color=gray]Versão:[/color]   {$version}
                   {"["}color=gray]Engine:[/color]   {$engVersion}
                   {"["}color=gray]Hash de Tipo:[/color]   {$hash}
                   {"["}color=gray]Comp Hash:[/color]   {$compHash}

# Replay selection window
replay-menu-select-title = Selecionar Replay

# Replay related verbs
replay-verb-spectate = Espectar

# command
cmd-replay-spectate-help = replay_spectate [optional entity]
cmd-replay-spectate-desc = Anexa ou desapega o jogador local para uma determinada entidade uid.
cmd-replay-spectate-hint = EntityUid Opcional
