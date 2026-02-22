markings-used = Используемые черты
markings-limits = { $required ->
    [true] { $count ->
        [-1] Выберите хотя бы одну черту.
        [0] Вы не можете выбирать черты, но каким-то образом должны? Это баг.
        [one] Выберите одну черту.
       *[other] Выберите от одной до {$count} черт. { -markings-selection(selectable: $selectable) }
    }
    *[false] { $count ->
        [-1] Выберите любое количество черт.
        [0] Вы не можете выбирать черты.
        [one] Выберите не более одной черты.
       *[other] Выберите не более {$count} черт. { -markings-selection(selectable: $selectable) }
    }
}
markings-unused = Неиспользуемые черты
markings-add = Добавить черту
markings-remove = Убрать черту
markings-rank-up = Вверх
markings-rank-down = Вниз
markings-search = Поиск
marking-points-remaining = Черт осталось: { $points }
marking-used = { $marking-name }
marking-used-forced = { $marking-name } (Принудительно)
marking-slot-add = Добавить
marking-slot-remove = Удалить
marking-slot = Слот { $number }
humanoid-marking-modifier-force = Принудительно
humanoid-marking-modifier-ignore-species = Игнорировать вид
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:

# Categories

markings-category-Special = Специальное
markings-category-Hair = Причёска
markings-category-FacialHair = Растительность на лице
markings-category-Head = Голова
markings-category-HeadTop = Голова (верх)
markings-category-HeadSide = Голова (бок)
markings-category-Snout = Морда
markings-category-SnoutCover = Морда (внешняя)
markings-category-UndergarmentTop = Нижнее бельё (верх)
markings-category-UndergarmentBottom = Нижнее бельё (низ)
markings-category-Chest = Грудь
markings-category-Arms = Руки
markings-category-Legs = Ноги
markings-category-Tail = Хвост
markings-category-Overlay = Оверлей
markings-category-Eyes = Глаза
