
### Interaction Messages

# Shown when player tries to replace light, but there is no lighs left
comp-light-replacer-missing-light = Não há lâmpadas sobrando {THE($light-replacer)}.

# Shown when player inserts light bulb inside light replacer
comp-light-replacer-insert-light = Você insere {$bulb} dentro {THE($light-replacer)}.

# Shown when player tries to insert in light replacer brolen light bulb
comp-light-replacer-insert-broken-light = Você não pode colocar lâmpadas quebradas!

# Shown when player refill light from light box
comp-light-replacer-refill-from-storage = Você enche {THE($light-replacer)} de lâmpadas.

### Examine 

comp-light-replacer-no-lights = Está vazio.
comp-light-replacer-has-lights = Ele contém o seguinte:
comp-light-replacer-light-listing = {$amount ->
    [one] [color=yellow]{$amount}[/color] [color=gray]{$name}[/color]
    *[other] [color=yellow]{$amount}[/color] [color=gray]{$name}s[/color]
}