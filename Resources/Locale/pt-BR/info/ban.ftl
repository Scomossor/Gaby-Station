# ban
cmd-ban-desc = Bane alguém.
cmd-ban-help = Uso: ban <nome ou ID do usuário> <razão> [duração em minutos, deixe vazio ou 0 para ban permanente]
cmd-ban-player = Não foi possível encontrar um usuário com esse nome.
cmd-ban-invalid-minutes = {$minutes} não é uma quantidade válida de minutos!
cmd-ban-invalid-severity = {$severity} não é uma gravidade válida!
cmd-ban-invalid-arguments = Número de argumentos inválido
cmd-ban-hint = <nome/ID do usuário>
cmd-ban-hint-reason = <razão>
cmd-ban-hint-duration = [duração]
cmd-ban-hint-severity = [gravidade]

cmd-ban-hint-duration-1 = Permanenente
cmd-ban-hint-duration-2 = 1 dia
cmd-ban-hint-duration-3 = 3 dias
cmd-ban-hint-duration-4 = 1 semana
cmd-ban-hint-duration-5 = 2 semanas
cmd-ban-hint-duration-6 = 1 mês

# ban panel
cmd-banpanel-desc = Abre o painel de ban
cmd-banpanel-help = Uso: banpanel [nome de usuário ou guid]
cmd-banpanel-server = Isso não pode ser usado a partir do console do servidor
cmd-banpanel-player-err = O jogador especificado não foi encontrado

# listbans
cmd-banlist-desc = Lista os bans ativos de um usuário.
cmd-banlist-help = Uso: banlist <nome ou ID do usuário>
cmd-banlist-empty = Sem bans ativos encontrados para {$user}
cmd-banlistF-hint = <nome/ID do usuário>

cmd-ban_exemption_update-desc = Defina uma isenção para um tipo de banimento de um jogador.
cmd-ban_exemption_update-help = Uso: ban_exemption_update <player> <flag> [<flag> [...]]
    Especifique vários indicadores para conceder a um jogador múltiplos indicadores de isenção de banimento.
    Para remover todas as isenções, execute este comando e forneça "Nenhum" como único sinalizador.

cmd-ban_exemption_update-nargs = Pelo menos 2 argumentos esperados
cmd-ban_exemption_update-locate = Não foi possível localizar o jogador '{$player}'.
cmd-ban_exemption_update-invalid-flag = Indicador inválido '{$flag}'.
cmd-ban_exemption_update-success = Indicador de isenção de banimento atualizados para '{$player}' ({$uid}).
cmd-ban_exemption_update-arg-player = <player>
cmd-ban_exemption_update-arg-flag = <flag>

cmd-ban_exemption_get-desc = Mostrar isenções de banimento para um determinado jogador.
cmd-ban_exemption_get-help = Uso: ban_exemption_get <player>

cmd-ban_exemption_get-nargs = Esperado exatamente 1 argumento
cmd-ban_exemption_get-none = O usuário não está isento de banimentos.
cmd-ban_exemption_get-show = O usuário está isento das seguintes indicadores de banimento: {$flags}.
cmd-ban_exemption_get-arg-player = <player>

# Ban panel
ban-panel-title = Painel de ban
ban-panel-player = Jogador
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Razão
ban-panel-last-conn = Usar IP e HWID da última conexão?
ban-panel-submit = Banir
ban-panel-confirm = Tem certeza?
ban-panel-tabs-basic = Info básica
ban-panel-tabs-reason = Razão
ban-panel-tabs-players = Lista de Jogadores
ban-panel-tabs-role = Info de role ban
ban-panel-no-data = Você deve fornecer um usuário, IP ou HWID para banir
ban-panel-invalid-ip =  O endereço IP não pôde ser parseado. Por favor, tente novamente.
ban-panel-select = Selecione tipo
ban-panel-server = Ban de servidor
ban-panel-role = Role ban
ban-panel-minutes = Minutos
ban-panel-hours = Horas
ban-panel-days = Dias
ban-panel-weeks = Semanas
ban-panel-months = Meses
ban-panel-years = Anos
ban-panel-permanent = Permanente
ban-panel-ip-hwid-tooltip = Deixe o vazio e marque a caixa de seleção abaixo para usar os detalhes da última conexão
ban-panel-severity = Gravidade:
ban-panel-erase = Apagar mensagens de bate-papo e o jogador da rodada

# Ban string
server-ban-string = {$admin} criou um ban de servidor de gravidade {$severity} que expira em {$expires} para [{$name}, {$ip}, {$hwid}], com razão: {$reason}
server-ban-string-no-pii = {$admin} criou um ban de servidor de gravidade {$severity} que expira em {$expires} para {$name} com razão: {$reason}
server-ban-string-never = nunca
