generator-clogged = {THE($generator)} desliga abruptamente!

portable-generator-verb-start = Iniciar gerador
portable-generator-verb-start-msg-unreliable = Inicie o gerador. Isso pode exigir algumas tentativas.
portable-generator-verb-start-msg-reliable = Inicie o gerador.
portable-generator-verb-start-msg-unanchored = O gerador deve ser ancorado primeiro!
portable-generator-verb-stop = Pare o gerador
portable-generator-start-fail = Você puxou a corda, mas ele não iniciou.
portable-generator-start-success = Você puxa a corda e ele deu sinal de vida.

portable-generator-ui-title = Gerador Portátil
portable-generator-ui-status-stopped = Parado:
portable-generator-ui-status-starting = Iniciando:
portable-generator-ui-status-running = Rodando:
portable-generator-ui-start = Iniciar
portable-generator-ui-stop = Parar
portable-generator-ui-target-power-label = Potencia Alvo (kW):
portable-generator-ui-efficiency-label = Eficiência:
portable-generator-ui-fuel-use-label = Uso de combustível:
portable-generator-ui-fuel-left-label = Combustível restante:
portable-generator-ui-clogged = Contaminantes detectados no tanque de combustível!
portable-generator-ui-eject = Ejetar
portable-generator-ui-eta = (~{ $minutes } min)
portable-generator-ui-unanchored = Não ancorado
portable-generator-ui-current-output = Saída atual: {$voltage}
portable-generator-ui-network-stats = Rede:
portable-generator-ui-network-stats-value = { POWERWATTS($supply) } / { POWERWATTS($load) }
portable-generator-ui-network-stats-not-connected = Não conectado

power-switchable-generator-examine = A saída de energia está definida para {$voltage}.
power-switchable-generator-switched = Saída trocada para {$voltage}!

power-switchable-voltage = { $voltage ->
    [HV] [color=orange]HV[/color]
    [MV] [color=yellow]MV[/color]
    *[LV] [color=green]LV[/color]
}
power-switchable-switch-voltage = Trocar para {$voltage}

fuel-generator-verb-disable-on = Desligue o gerador primeiro!
