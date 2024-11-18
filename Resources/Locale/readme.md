# Traduções
Essa é a pasta referente a todas as traduções do jogo (elas ficam em pt-BR), caso não exista o jogo pegara a tradução em ingles que fica na pasta en-US

## Como ajudar?
Simples!! Faça uma fork do repositorio, e escolha uma das pastas (ou subpastas) que ficam dentro da pasta en-US e traduza os textos dos arquivos, após isso é so abrir uma PR
Recomendamos que faça isso por pasta, ou seja, traduzir uma pasta inteira por vez para nao virar bagunça (caso não tenha traduzido algum arquivo da pasta, você pode avisar na descrição da PR)

## Que arquivos estranhos o que eu faço?!!!! ME AJUDAAAAAAAAAAAAAAA!!!!!!!!!!!!!!!!!!!!!!!!!!!!
Claro, aqui vai duas explicações de como traduzir esses arquivos!!!

### Explicação legal:
Os arquivos seguem esse padrão:
lobby-state-round-start-countdown-text = A rodada começa em: {$timeLeft}
tudo que está atras do = voce NÃO deve modificar, apenas após isso, alem disso alguns dos arquivos podem conter coisas dentro de chave como {$timeleft} o que tiver dentro dessas chaves NÃO devem ser traduzidos, pois ira quebrar a tradução

### Explicação nerdola: (você nao precisa ler essa)
Os arquivos seguem esse padrão:
lobby-state-round-start-countdown-text = A rodada começa em: {$timeLeft}
Tudo que está atras do "=" é um ID da tradução, que é o que o jogo usa pra saber qual é o texto de certa tradução, esses ids nao podem conter maisuculas nem espaços por isso eles são desse jeito.
Tudo após a = é texto da tradução, o que escapa disso sao textos dentro de {} que contem um $ na frente (ou seja {$timeLeft}) que o jogo usa para subistiuir por um favor/variavel nesse caso o jogo subistituo o texto {$timeLeft} pelo tempo que falta para o round começar.
