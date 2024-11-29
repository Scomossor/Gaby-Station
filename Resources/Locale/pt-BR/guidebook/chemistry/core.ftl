guidebook-reagent-effect-description =
    {$chance ->
        [1] { $effect }
        *[other] Tem um { NATURALPERCENT($chance, 2) } chance para { $effect }
    }{ $conditionCount ->
        [0] .
        *[other] {" "}quando { $conditions }.
    }

guidebook-reagent-name = [bold][color={$color}]{CAPITALIZE($name)}[/color][/bold]
guidebook-reagent-recipes-header = Receita
guidebook-reagent-recipes-reagent-display = [bold]{$reagent}[/bold] \[{$ratio}\]
guidebook-reagent-sources-header = Fontes
guidebook-reagent-sources-ent-wrapper = [bold]{$name}[/bold] \[1\]
guidebook-reagent-sources-gas-wrapper = [bold]{$name} (gas)[/bold] \[1\]
guidebook-reagent-effects-header = Efeitos
guidebook-reagent-effects-metabolism-group-rate = [bold]{$group}[/bold] [color=gray]({$rate} unidades por segundo)[/color]
guidebook-reagent-physical-description = [italic]Parece ser {$description}.[/italic]
guidebook-reagent-recipes-mix-info = {$minTemp ->
    [0] {$hasMax ->
            [true] {CAPITALIZE($verb)} abaixo de {NATURALFIXED($maxTemp, 2)}K
            *[false] {CAPITALIZE($verb)}
        }
    *[other] {CAPITALIZE($verb)} {$hasMax ->
            [true] entre {NATURALFIXED($minTemp, 2)}K e {NATURALFIXED($maxTemp, 2)}K
            *[false] acima de {NATURALFIXED($minTemp, 2)}K
        }
}
