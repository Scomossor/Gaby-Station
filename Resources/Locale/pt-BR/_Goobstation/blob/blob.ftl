ent-SpawnPointGhostBlob = Blob Spawn
    .suffix = DEBUG, Gerador de Papel de Fantasma
    .desc = { ent-MarkerBase.desc }
ent-MobBlobPod = Cápsula de Blob
    .desc = Um lutador comum do blob. Ele pode zombificar cadáveres.
ent-MobBlobBlobbernaut = Blobbernaut
    .desc = Lutador de elite do blob. Ele tem grande poder.
ent-BaseBlob = Blob básico.
    .desc = { "" }
ent-NormalBlobTile = Tile Regular de Blob
    .desc = Uma parte comum do blob necessária para a construção de tiles mais avançados.
ent-CoreBlobTile = Núcleo do Blob
    .desc = A parte mais importante do blob. Destruir o núcleo fará com que todas as outras partes morram.
ent-FactoryBlobTile = Fábrica de Blob
    .desc = Gera Cápsulas de Blob e Blobbernauts ao longo do tempo.
ent-ResourceBlobTile = Blob de Recursos
    .desc = Produz recursos para o blob, sendo uma parte importante do seu crescimento.
ent-NodeBlobTile = Nodo do Blob
    .desc = Uma versão menor do núcleo que permite colocar tiles especiais de blob ao seu redor.
ent-StrongBlobTile = Tile Forte de Blob
    .desc = Uma versão reforçada do tile regular. Não permite passagem de ar e protege contra danos mecânicos.
ent-ReflectiveBlobTile = Tiles Reflexivos do Blob
    .desc = Refletem lasers, mas não protegem tão bem contra danos mecânicos.
    .desc = { "" }

objective-issuer-blob = Blob
ghost-role-information-blobbernaut-name = Blobbernaut
ghost-role-information-blobbernaut-description = Você é um Blobbernaut. Sua missão é defender o núcleo do blob.
ghost-role-information-blob-name = Blob
ghost-role-information-blob-description = Você é um Blob. Consuma a estação.
roles-antag-blob-name = Blob
roles-antag-blob-objective = Domine a estação.
guide-entry-blob = Blob

# Popups
blob-target-normal-blob-invalid = Tipo de blob incorreto, selecione um blob normal.
blob-target-factory-blob-invalid = Tipo de blob incorreto, selecione uma fábrica de blob.
blob-target-node-blob-invalid = Tipo de blob incorreto, selecione um nodo de blob.
blob-target-close-to-resource = Muito próximo de outro blob de recurso.
blob-target-nearby-not-node = Nenhum nodo ou blob de recurso próximo.
blob-target-close-to-node = Muito próximo de outro nodo.
blob-target-already-produce-blobbernaut = Esta fábrica já produziu um blobbernaut.
blob-cant-split = Você não pode dividir o núcleo do blob.
blob-not-have-nodes = Você não tem nodos.
blob-not-enough-resources = Recursos insuficientes.
blob-help = Só Deus pode te ajudar.
blob-swap-chem = Em desenvolvimento.
blob-mob-attack-blob = Você não pode atacar um blob.
blob-get-resource = +{ $point }
blob-spent-resource = -{ $point }
blobberaut-not-on-blob-tile = Você está morrendo fora dos tiles de blob.
carrier-blob-alert = Você tem { $second } segundos restantes antes da transformação.
blob-mob-zombify-second-start = { $pod } começa a transformar você em um zumbi.
blob-mob-zombify-third-start = { $pod } começa a transformar { $target } em um zumbi.
blob-mob-zombify-second-end = { $pod } transformou você em um zumbi.
blob-mob-zombify-third-end = { $pod } transformou { $target } em um zumbi.
blobberaut-factory-destroy = fábrica destruída
blob-target-already-connected = já conectado

# UI
blob-chem-swap-ui-window-name = Trocar Químicos
blob-chem-reactivespines-info = Espinhos Reativos
                                Causa 25 de dano bruto.
blob-chem-blazingoil-info = Óleo Flamejante
                            Causa 15 de dano por queimadura e incendeia os alvos.
                            Deixa você vulnerável à água.
blob-chem-regenerativemateria-info = Matéria Regenerativa
                                    Causa 6 de dano bruto e 15 de dano tóxico.
                                    O núcleo do blob regenera vida 10 vezes mais rápido que o normal e gera 1 recurso extra.
blob-chem-explosivelattice-info = Malha Explosiva
                                    Causa 5 de dano por queimadura e explode o alvo, causando 10 de dano bruto.
                                    Esporos explodem ao morrer.
                                    Você se torna imune a explosões.
                                    Você sofre 50% mais dano por queimaduras e choques elétricos.
blob-chem-electromagneticweb-info = Rede Eletromagnética
                                    Causa 20 de dano por queimadura e tem 20% de chance de causar um pulso EMP ao atacar.
                                    Tiles de blob causam um pulso EMP ao serem destruídos.
                                    Você sofre 25% mais dano bruto e de calor.
blob-alert-out-off-station = O blob foi removido porque foi encontrado fora da estação!

# Announcment
blob-alert-recall-shuttle = A nave de emergencia não pode ser enviado enquanto houver um risco biológico nível 5 na estação.
blob-alert-detect = Confirmado surto de risco biológico nível 5 na estação. Todo os tripulantes devem conter o surto. A nave de emergencia não podem ser enviados devido aos riscos de contaminação.
blob-alert-critical = Nível crítico de risco biológico, códigos de autenticação nuclear foram enviados para a estação. O Comando Central ordena que qualquer tripulante restante ative o mecanismo de autodestruição.
blob-alert-critical-NoNukeCode = Nível crítico de risco biológico. O Comando Central ordena que qualquer tripulante restante procure abrigo e aguarde o resgate.

# Actions
blob-create-factory-action-name = Colocar Blob de Fábrica (80)
blob-create-factory-action-desc = Transforma o blob normal selecionado em um blob de fábrica, que produzirá até 3 esporos e um blobbernaut se colocado próximo a um núcleo ou nó.

blob-create-resource-action-name = Colocar Blob de Recurso (60)
blob-create-resource-action-desc = Transforma o blob normal selecionado em um blob de recurso, que gerará recursos se colocado próximo a um núcleo ou nó.

blob-create-node-action-name = Colocar Blob de Nó (50)
blob-create-node-action-desc = Transforma o blob normal selecionado em um blob de nó.
                                Um blob de nó ativará os efeitos de blobs de fábrica e recurso, curará outros blobs e se expandirá lentamente, destruindo paredes e criando blobs normais.

blob-produce-blobbernaut-action-name = Produzir um Blobbernaut (60)
blob-produce-blobbernaut-action-desc = Cria um blobbernaut na fábrica selecionada. Cada fábrica só pode fazer isso uma vez. O blobbernaut sofrerá dano fora dos tiles de blob e será curado quando estiver próximo a nós.

blob-split-core-action-name = Dividir Núcleo (400)
blob-split-core-action-desc = Você só pode fazer isso uma vez. Transforma o nó selecionado em um núcleo independente que agirá sozinho.

blob-swap-core-action-name = Realocar Núcleo (200)
blob-swap-core-action-desc = Troca a localização do seu núcleo com o nó selecionado.

blob-teleport-to-core-action-name = Voltar ao Núcleo (0)
blob-teleport-to-core-action-desc = Teleporta você para o Núcleo do Blob.

blob-teleport-to-node-action-name = Ir para o Nó (0)
blob-teleport-to-node-action-desc = Teleporta você para um nó de blob aleatório.

blob-help-action-name = Ajuda
blob-help-action-desc = Obtenha informações básicas sobre como jogar como blob.

blob-swap-chem-action-name = Trocar Químicos (70)
blob-swap-chem-action-desc = Permite que você troque seu químico atual.

blob-carrier-transform-to-blob-action-name = Transformar-se em um Blob
blob-carrier-transform-to-blob-action-desc = Destrói instantaneamente seu corpo e cria um núcleo de blob. Certifique-se de estar em um tile de chão, caso contrário, você simplesmente desaparecerá.

blob-downgrade-action-name = Rebaixar Blob (0)
blob-downgrade-action-desc = Transforma o tile selecionado de volta em um blob normal para instalar outros tipos de gaiolas.


# Ghost role
blob-carrier-role-name = Portador de Blob
blob-carrier-role-desc = Uma criatura infectada por um blob.
blob-carrier-role-rules = Você é um antagonista. Você tem 4 minutos antes de se transformar em um blob.
                        Use esse tempo para encontrar um lugar seguro na estação. Lembre-se de que você estará muito fraco logo após a transformação.
blob-carrier-role-greeting = Você é um portador de Blob. Encontre um local isolado na estação e transforme-se em um Blob. Converta a estação em uma massa e seus habitantes em seus servos. Somos todos Blobs.

# Verbs
blob-pod-verb-zombify = Blobificar
blob-verb-upgrade-to-strong = Atualizar para Blob Forte
blob-verb-upgrade-to-reflective = Atualizar para Blob Reflexivo
blob-verb-remove-blob-tile = Remover Blob

# Alerts
blob-resource-alert-name = Recursos do Núcleo
blob-resource-alert-desc = Seus recursos produzidos pelo núcleo e blobs de recursos. Use-os para expandir e criar blobs especiais.
blob-health-alert-name = Saúde do Núcleo
blob-health-alert-desc = A saúde do seu núcleo. É [color=red]fim de jogo[/color] se ela chegar a zero.

# Greeting
blob-role-greeting =
    You are blob - a parasitic space creature capable of destroying entire stations.
        Your goal is to survive and grow as large as possible.
    	You are almost invulnerable to physical damage, but heat can still hurt you.
        Use Alt+LMB to upgrade normal blob tiles to strong blob and strong blob to reflective blob.
    	Make sure to place resource blobs to generate resources.
        Keep in mind that resource blobs and factories will only work when next to node blobs or cores.
blob-zombie-greeting = You were infected and raised by a blob spore. Now you must help the blob take over the station.

# End round
blob-round-end-result =
    { $blobCount ->
        [one] Havia apenas um blob.
        *[other] Havia {$blobCount} blobs.
    }
blob-user-was-a-blob = [color=gray]{$user}[/color] era um blob.
blob-user-was-a-blob-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) era um blob.
blob-was-a-blob-named = [color=White]{$name}[/color] era um blob.
preset-blob-objective-issuer-blob = [color=#33cc00]Blob[/color]
blob-user-was-a-blob-with-objectives = [color=gray]{$user}[/color] era um blob com os seguintes objetivos:
blob-user-was-a-blob-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) era um blob com os seguintes objetivos:
blob-was-a-blob-with-objectives-named = [color=White]{$name}[/color] era um blob com os seguintes objetivos:

# Objectivies
objective-condition-blob-capture-title = Domine a estação
objective-condition-blob-capture-description = Seu único objetivo é dominar toda a estação. Você precisa ter pelo menos {$count} tiles de blob.
objective-condition-success = { $condition } | [color={ $markupColor }]Sucesso![/color]
objective-condition-fail = { $condition } | [color={ $markupColor }]Falha![/color] ({ $progress }%)
