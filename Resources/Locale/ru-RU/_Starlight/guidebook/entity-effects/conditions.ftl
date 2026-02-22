entity-condition-guidebook-unknown-reagent = неизвестный реагент
entity-condition-guidebook-blood-reagent-threshold =
    { $max ->
        [2147483648] в кровотоке есть по крайней мере { NATURALFIXED($min, 2) }ед { $reagent }
       *[other]
            { $min ->
                [0] в кровотоке есть не более { NATURALFIXED($max, 2) }ед { $reagent }
               *[other] в кровотоке есть от { NATURALFIXED($min, 2) }ед до { NATURALFIXED($max, 2) }ед { $reagent }
            }
    }
