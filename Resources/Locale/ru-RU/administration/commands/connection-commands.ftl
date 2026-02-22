## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно разрешить пользователю обходить обычные проверки подключения.
cmd-grant_connect_bypass-help =
    Использование: grant_connect_bypass <user> [duration minutes]
    Временно предоставляет пользователю возможность обходить обычные ограничения подключения.
    Обход применяется только к этому игровому серверу и истекает через (по умолчанию) 1 час.
    Пользователь сможет присоединиться независимо от белого списка, паник-бункера или лимита игроков.
cmd-grant_connect_bypass-arg-user = <user>
cmd-grant_connect_bypass-arg-duration = [duration minutes]
cmd-grant_connect_bypass-invalid-args = Ожидалось 1 или 2 аргумента.
cmd-grant_connect_bypass-unknown-user = Не удалось найти пользователя '{ $user }'
cmd-grant_connect_bypass-invalid-duration = Неверная длительность '{ $duration }'
cmd-grant_connect_bypass-success = Успешно добавлен обход для пользователя '{ $user }'
