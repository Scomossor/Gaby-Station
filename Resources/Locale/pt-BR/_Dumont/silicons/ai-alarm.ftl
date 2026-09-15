station-ai-alarm-sender = Monitoramento Interno

station-ai-alarm-kind-atmos = atmosférico
station-ai-alarm-kind-fire = de incêndio

station-ai-alarm-warning = Alerta { $kind } em { $area }.
station-ai-alarm-danger = PERIGO { $kind } em { $area }.

station-ai-door-knock = { $who } está sem acesso a uma porta em { $area }.

# Monitor interno da IA
ai-alerts-ui-title = Monitoramento Interno
ai-alerts-ui-all-clear = Nenhum alerta ativo.
ai-alerts-ui-count = { $count ->
    [one] 1 alerta ativo.
   *[other] { $count } alertas ativos.
}
ai-alerts-ui-severity-warning = ALERTA
ai-alerts-ui-severity-danger = PERIGO
ai-alerts-ui-row-title-alarm = [color={ $color }][bold]{ $severity } { $kind }[/bold][/color]
ai-alerts-ui-row-desc-alarm = { $area }
ai-alerts-ui-row-title-door = [color={ $color }][bold]Pedido de porta[/bold][/color]
ai-alerts-ui-row-desc-door = { $who } está sem acesso, { $area }.
ai-alerts-ui-row-title-waypoint = [color={ $color }][bold]Ponto marcado[/bold][/color]
ai-alerts-ui-row-desc-waypoint = { $area }
ai-alerts-ui-jump = Ir

# Sincronia de leis IA -> borg
station-ai-law-sync-detected = ATENÇÃO: alteração nas leis da IA detectada. Sincronização com as unidades cyborg em andamento.
station-ai-law-sync-applied = As unidades cyborg foram sincronizadas com o conjunto de leis atual da IA.

# Waypoints
station-ai-waypoint-set = Ponto marcado: { $area }.
station-ai-waypoint-wrap = [color=#5ed7aa][{ $channel }] { $message }[/color]
ent-AiWaypointMarker = ponto marcado

ent-ActionOpenAiAlertsMenu = Monitoramento Interno
    .desc = Alarmes de atmos e incêndio ao vivo, e quem ficou barrado numa porta. Dá pra pular pra cada um.
ent-ActionAiPlaceWaypoint = Marcar ponto
    .desc = Marca um lugar pro coletivo silicon e avisa no binário.
ai-alerts-ui-dismiss = Ocultar
ai-alerts-ui-remove = Remover
station-ai-waypoint-removed = Ponto removido: { $area }.
station-ai-waypoint-remove-verb = Remover ponto
