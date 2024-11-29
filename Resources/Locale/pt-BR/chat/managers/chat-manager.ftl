### UI

chat-manager-max-message-length = Sua mensagem excedeu o limite de {$maxMessageLength} caractéres
chat-manager-ooc-chat-enabled-message = Chat OOC foi habilitado.
chat-manager-ooc-chat-disabled-message = Chat OOC foi desabilitado.
chat-manager-looc-chat-enabled-message = Chat LOOC foi habilitado.
chat-manager-looc-chat-disabled-message = Chat LOOC foi desabilitado.
chat-manager-dead-looc-chat-enabled-message = Jogadores mortos agora podem usar LOOC.
chat-manager-dead-looc-chat-disabled-message = Jogadores mortos não podem mais usar LOOC.
chat-manager-crit-looc-chat-enabled-message = Jogadores em crit agora podem usar LOOC.
chat-manager-crit-looc-chat-disabled-message = Jogadores em crit não podem mais usar LOOC.
chat-manager-admin-ooc-chat-enabled-message = Chat Admin OOC foi habilitado.
chat-manager-admin-ooc-chat-disabled-message = Chat Admin OOC foi desabilitado.

chat-manager-max-message-length-exceeded-message = Sua mensagem excedeu o limite de {$limit} caractéres
chat-manager-no-headset-on-message = Você não tem um headset ligado!
chat-manager-no-radio-key = Nenhuma chave de canal selecionada!
chat-manager-no-such-channel = Não existe canal com a chave '{$key}'!
chat-manager-whisper-headset-on-message = Você não pode sussurrar no rádio!

chat-manager-server-wrap-message = [bold]{$message}[/bold]
chat-manager-sender-announcement-wrap-message = [font size=14][bold]Anúncio de {$sender}:[/font][font size=12]
                                                {$message}[/bold][/font]
chat-manager-entity-say-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent]{$message}[/BubbleContent]"[/font]
chat-manager-entity-say-bold-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent][bold]{$message}[/bold][/BubbleContent]"[/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic][BubbleHeader][Name]{$entityName}[/Name][/BubbleHeader] sussurra,"[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Alguém[/BubbleHeader] sussurra, "[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]

# THE() is not used here because the entity and its name can technically be disconnected if a nameOverride is passed...
chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] a {$entityName} {$message}[/italic]
     [true] {$entityName} {$message}[/italic]
    }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}

chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold][BubbleHeader]{$playerName}[/BubbleHeader]:[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]([BubbleHeader]{$userName}[/BubbleHeader]):[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]

chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}

chat-manager-dead-channel-name = MORTO
chat-manager-admin-channel-name = ADMIN

chat-manager-rate-limited = Você está enviando mensagens muito rapidamente!
chat-manager-rate-limit-admin-announcement = O jogador { $player } violou os limites de taxa de chat. Observe-os se esta for uma ocorrência regular.

## Speech verbs for chat

chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..

chat-speech-verb-default = diz
chat-speech-verb-exclamation = exclama
chat-speech-verb-exclamation-strong = grita
chat-speech-verb-question = pergunta
chat-speech-verb-stutter = gagueja
chat-speech-verb-mumble = murmura

chat-speech-verb-insect-1 = chia
chat-speech-verb-insect-2 = gorjea
chat-speech-verb-insect-3 = clica

chat-speech-verb-winged-1 = vibra
chat-speech-verb-winged-2 = agitar
chat-speech-verb-winged-3 = zumbe

chat-speech-verb-slime-1 = borrifa
chat-speech-verb-slime-2 = borbulha
chat-speech-verb-slime-3 = escorre

chat-speech-verb-plant-1 = farfalha
chat-speech-verb-plant-2 = balança
chat-speech-verb-plant-3 = range

chat-speech-verb-robotic-1 = afirma
chat-speech-verb-robotic-2 = bipa

chat-speech-verb-reptilian-1 = chia
chat-speech-verb-reptilian-2 = bufa
chat-speech-verb-reptilian-3 = magoa

chat-speech-verb-skeleton-1 = chocalha
chat-speech-verb-skeleton-2 = estala
chat-speech-verb-skeleton-3 = range

chat-speech-verb-vox-1 = guincha
chat-speech-verb-vox-2 = pia
chat-speech-verb-vox-3 = cui

chat-speech-verb-canine-1 = late
chat-speech-verb-canine-2 = ladre
chat-speech-verb-canine-3 = uivos

chat-speech-verb-small-mob-1 = guincha
chat-speech-verb-small-mob-2 = pia

chat-speech-verb-large-mob-1 = ruge
chat-speech-verb-large-mob-2 = rosna

chat-speech-verb-monkey-1 = macaquea
chat-speech-verb-monkey-2 = guincha

chat-speech-verb-cluwne-1 = ri
chat-speech-verb-cluwne-2 = gargalha
chat-speech-verb-cluwne-3 = dá risada

chat-speech-verb-ghost-1 = reclama
chat-speech-verb-ghost-2 = suspira
chat-speech-verb-ghost-3 = zumbe
chat-speech-verb-ghost-4 = murmura

chat-speech-verb-electricity-1 = estalando
chat-speech-verb-electricity-2 = zumbidas
chat-speech-verb-electricity-3 = gritos
