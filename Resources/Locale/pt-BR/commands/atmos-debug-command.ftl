cmd-atvrange-desc = Define o intervalo de depuração do atmos (como dois pontos flutuantes, início [vermelho] e fim [azul])
cmd-atvrange-help = Uso: {$command} <start> <end>
cmd-atvrange-error-start = Flutuador inválido START
cmd-atvrange-error-end = Flutuador inválido END
cmd-atvrange-error-zero = A escala não pode ser zero, pois isso causaria uma divisão por zero no AtmosDebugOverlay.

cmd-atvmode-desc = Define o modo de depuração do atmos. Isso redefinirá automaticamente a escala.
cmd-atvmode-help = Uso: {$command} <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]
cmd-atvmode-error-invalid = Modo inválido
cmd-atvmode-error-target-gas = Um gás alvo deve ser fornecido para este modo.
cmd-atvmode-error-out-of-range = ID do gás não analisável ou fora do intervalo.
cmd-atvmode-error-info = Nenhuma informação adicional é necessária para este modo.

cmd-atvcbm-desc = Mudanças de vermelho/verde/azul para escala de cinza
cmd-atvcbm-help = Uso: {$command} <true/false>
cmd-atvcbm-error = Sinalizador inválido