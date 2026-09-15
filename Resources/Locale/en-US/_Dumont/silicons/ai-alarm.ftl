station-ai-alarm-sender = Internal Monitoring

station-ai-alarm-kind-atmos = Atmospheric
station-ai-alarm-kind-fire = Fire

station-ai-alarm-warning = { $kind } warning in { $area }.
station-ai-alarm-danger = { $kind } DANGER in { $area }.

station-ai-door-knock = { $who } has no access to a door in { $area }.

# AI internal alert monitor
ai-alerts-ui-title = Internal Monitoring
ai-alerts-ui-all-clear = No active alerts.
ai-alerts-ui-count = { $count ->
    [one] 1 active alert.
   *[other] { $count } active alerts.
}
ai-alerts-ui-severity-warning = WARNING
ai-alerts-ui-severity-danger = DANGER
ai-alerts-ui-row-title-alarm = [color={ $color }][bold]{ $kind } { $severity }[/bold][/color]
ai-alerts-ui-row-desc-alarm = { $area }
ai-alerts-ui-row-title-door = [color={ $color }][bold]Door request[/bold][/color]
ai-alerts-ui-row-desc-door = { $who } has no access, { $area }.
ai-alerts-ui-row-title-waypoint = [color={ $color }][bold]Waypoint[/bold][/color]
ai-alerts-ui-row-desc-waypoint = { $area }
ai-alerts-ui-jump = Jump

# AI -> borg law synchronisation
station-ai-law-sync-detected = WARNING: AI lawset change detected. Synchronisation with cyborg units in progress.
station-ai-law-sync-applied = Cyborg units have been synchronised to the AI's current lawset.

# Waypoints
station-ai-waypoint-set = Waypoint marked: { $area }.
station-ai-waypoint-wrap = [color=#5ed7aa][{ $channel }] { $message }[/color]
ai-alerts-ui-dismiss = Hide
ai-alerts-ui-remove = Remove
station-ai-waypoint-removed = Waypoint removed: { $area }.
station-ai-waypoint-remove-verb = Remove waypoint
