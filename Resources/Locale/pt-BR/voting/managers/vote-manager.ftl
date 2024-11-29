# Displayed as initiator of vote when no user creates the vote
ui-vote-initiator-server = O Servidor

## Default.Votes

ui-vote-restart-title = Reiniciar rodada
ui-vote-restart-succeeded = Reinício de rodada aceito.
ui-vote-restart-failed = Reinício de rodada negado (necessário { TOSTRING($ratio, "P0") }).
ui-vote-restart-fail-not-enough-ghost-players = Restart vote failed: Um mínimo de { $ghostPlayerRequirement }% jogadores fantasmas é necessário para iniciar uma votação de reinicialização. Atualmente, não há jogadores fantasmas suficientes.

 $ghostPlayerRequirement ?% ghost players
ui-vote-restart-yes = Sim
ui-vote-restart-no = Não
ui-vote-restart-abstain = Nulo

ui-vote-gamemode-title = Próximo modo de jogo
ui-vote-gamemode-tie = Empate na escolha do modo de jogo! Escolhendo... { $picked }
ui-vote-gamemode-win = { $winner } venceu a escolha do modo de jogo!

ui-vote-map-title = Próximo mapa
ui-vote-map-tie = Empate na escolha de mapa! Escolhendo... { $picked }
ui-vote-map-win = { $winner } venceu a escolha de mapa!
ui-vote-map-notlobby = Votar para mapas só é válido no lobby pré-round!
ui-vote-map-notlobby-time = Votar para mapas só é válido no lobby pré-round com pelo menos { $time } restante!