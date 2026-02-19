markings-used = Используемые черты
-markings-selection =
    { $selectable ->
        [0] У вас не осталось доступных черт.
        [one] Вы можете выбрать ещё одну черту.
        [few] Вы можете выбрать ещё { $selectable } черты.
       *[other] Вы можете выбрать ещё { $selectable } черт.
    }
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
markings-reorder = Изменить порядок
humanoid-marking-modifier-respect-limits = Соблюдать лимиты
humanoid-marking-modifier-respect-group-sex = Соблюдать ограничения группы и пола
markings-unused = Неиспользуемые черты
markings-add = Добавить черту
markings-remove = Убрать черту
markings-rank-up = Вверх
markings-organ-Torso = Туловище
markings-organ-Head = Голова
markings-organ-ArmLeft = Левая рука
markings-organ-ArmRight = Правая рука
markings-organ-HandRight = Правая кисть
markings-organ-HandLeft = Левая кисть
markings-organ-LegLeft = Левая нога
markings-organ-LegRight = Правая нога
markings-organ-FootLeft = Левая стопа
markings-organ-FootRight = Правая стопа
markings-organ-Eyes = Глаза
markings-layer-Special = Специальное
markings-layer-Tail = Хвост
markings-layer-Tail-Moth = Крылья
markings-layer-Hair = Причёска
markings-layer-FacialHair = Растительность на лице
markings-layer-UndergarmentTop = Майка
markings-layer-UndergarmentBottom = Трусы
markings-layer-Chest = Грудь
markings-layer-Head = Голова
markings-layer-Snout = Морда
markings-layer-SnoutCover = Морда (внешняя часть)
markings-layer-HeadSide = Голова (сбоку)
markings-layer-HeadTop = Голова (сверху)
markings-layer-Eyes = Глаза
markings-layer-RArm = Правая рука
markings-layer-LArm = Левая рука
markings-layer-RHand = Правая кисть
markings-layer-LHand = Левая кисть
markings-layer-RLeg = Правая нога
markings-layer-LLeg = Левая нога
markings-layer-RFoot = Правая стопа
markings-layer-LFoot = Левая стопа
markings-layer-Overlay = Оверлей
markings-layer-TailOverlay = Оверлей хвоста
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
