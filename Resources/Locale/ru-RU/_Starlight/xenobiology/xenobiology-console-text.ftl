# The selectors in the case of 1 just don't work for some reason.
# Guess we're always going for plural?

xenobiology-console-monkey-cube-inserted =
    Спасибо за вставку кубика обезьяны! Теперь в консоли { $cubes } { $cubes ->
        [1] кубик
        [few] кубика
       *[other] кубиков
    }.
xenobiology-console-mutation-potion-inserted =
    Спасибо за вставку зелья мутации! Теперь в консоли { $potions } { $potions ->
        [1] зелье
        [few] зелья
       *[other] зелий
    }.
xenobiology-console-stabilizer-potion-inserted =
    Спасибо за вставку стабилизирующего зелья! Теперь в консоли { $potions } { $potions ->
        [1] зелье
        [few] зелья
       *[other] зелий
    }.
xenobiology-console-slime-picked-up = { GENDER($user) ->
    [female] Подобрала
   *[male] Подобрал
} { $name }.
xenobiology-console-slime-picked-up-fail-full = Не удалось подобрать { $name }. Попробуйте выложить немного слаймов.
xenobiology-console-slime-picked-up-fail-none-found = Слаймы не найдены. Попробуйте подойти ближе к одному.
xenobiology-console-slime-placed-down = { GENDER($user) ->
    [female] Выложила
   *[male] Выложил
} { $name }.
xenobiology-console-slime-placed-down-fail-none-stored = Нет сохранённых слаймов. Попробуйте подобрать одного.
xenobiology-console-monkey-placed =
    Выложил обезьяну. Теперь у вас { $cubes } { $cubes ->
        [1] кубик
        [few] кубика
       *[other] кубиков
    }.
xenobiology-console-monkey-placed-fail-empty = Недостаточно кубиков обезьяны ({ $cubes }). Попробуйте вставить один или переработать уже съеденных обезьян.
xenobiology-console-monkey-recycled = { GENDER($user) ->
    [female] Переработала
   *[male] Переработал
} { $monkeys } { $monkeys ->
        [1] обезьяну
        [few] обезьяны
       *[other] обезьян
    }. Теперь у вас { $cubes } { $cubes ->
        [1] кубик
        [few] кубика
       *[other] кубиков
    }.
xenobiology-console-monkey-recycled-failed-none = Не найдено обезьян для переработки. Попробуйте подойти ближе или убедитесь, что они достаточно повреждены.
xenobiology-console-mutation-potion-applied = Применено зелье мутации к { $name }. Теперь шанс мутации: { $chance }.
xenobiology-console-mutation-potion-applied-failed-empty = Нет зелий мутации. Попробуйте вставить одно.
xenobiology-console-stabilizer-potion-applied = Применено стабилизирующее зелье к { $name }. Теперь шанс мутации: { $chance }.
xenobiology-console-stabilizer-potion-applied-failed-empty = Нет стабилизирующих зелий. Попробуйте вставить одно.
