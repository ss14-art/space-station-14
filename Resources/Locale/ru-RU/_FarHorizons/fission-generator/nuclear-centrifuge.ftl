nuclear-centrifuge-insert-item = { CAPITALIZE(THE($user)) } { GENDER($user) ->
    [female] вставила
   *[male] вставил
} { THE($item) } в { THE($machine) }.
nuclear-centrifuge-wrong-item = Нельзя поместить { THE($item) } сюда, не подходит.
nuclear-centrifuge-unfit-item = { THE($item) } { GENDER($item) ->
    [female] не готова
   *[male] не готов
} к переработке.
