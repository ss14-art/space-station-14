moff-blade-server-rack-window-title = Стойка блейд-сервера
moff-blade-server-rack-window-footer-flavor = ПРОШИВКА УСТРОЙСТВА © 2125 NANOSOFT
moff-blade-server-rack-slot-status = Слот { $index }: { $content }
moff-blade-server-rack-slot-entity-unknown = неизвестно
moff-blade-server-rack-slot-empty = пусто
moff-blade-server-rack-slot-eject = Извлечь
moff-blade-server-rack-slot-insert = Вставить
moff-blade-server-rack-slot-power-toggle = Переключить питание
moff-blade-server-rack-slot-locked-fail = Заблокировано!
moff-blade-server-rack-slot-whitelist-fail = Не подходит!
moff-blade-server-rack-examine-empty = Внутри [color=#1f8ab2]нет блейдов[/color].
moff-blade-server-rack-examine-single = Внутри только { $slot }.
moff-blade-server-rack-examine-multiple-start = Внутри:
moff-blade-server-rack-examine-multiple-slot-line = - { $slot }
moff-blade-server-rack-examine-slot = { INDEFINITE($name) } [color=#1f8ab2]{ CAPITALIZE($name) }[/color] в слоте { $index }
moff-blade-server-rack-examine-distant =
    Внутри [color=#1f8ab2]{ $numBlades } { $numBlades ->
        [1] блейд
        [few] блейда
       *[other] блейдов
    }[/color], но с такого расстояния невозможно разобрать, что { $numBlades ->
        [1] это
       *[other] они
    } собой представляет{ $numBlades ->
        [1] представляет
       *[other] представляют
    }.
moff-blade-server-frame-incompatible-board = Эта плата кажется несовместимой с рамой...
moff-blade-server-board-compatible-hint = Может быть использовано для создания [color=#1f8ab2]блейд-сервера[/color]
