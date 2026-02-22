command-list-langs-desc = Перечисляет языки, на которых ваша текущая сущность может говорить в данный момент.
command-list-langs-help = Использование: { $command }
command-saylang-desc = Отправить сообщение на определённом языке. Для выбора языка можно использовать либо название языка, либо его позицию в списке языков.
command-saylang-help = Использование: { $command } <id языка> <сообщение>. Пример: { $command } GalacticCommon "Привет, мир!". Пример: { $command } 1 "Привет, мир!"
command-language-select-desc = Выбрать текущий разговорный язык вашей сущности. Можно использовать либо название языка, либо его позицию в списке языков.
command-language-select-help = Использование: { $command } <id языка>. Пример: { $command } 1. Пример: { $command } GalacticCommon
command-language-spoken = Разговорный:
command-language-understood = Понимаемый:
command-language-current-entry = { $id }. { $language } - { $name } (текущий)
command-language-entry = { $id }. { $language } - { $name }
command-language-invalid-number = Номер языка должен быть от 0 до { $total }. Или используйте название языка.
command-language-invalid-language = Язык { $id } не существует или вы не можете на нём говорить.

# Toolshed

command-description-language-add = Добавляет новый язык к подключённой сущности. Два последних аргумента указывают, должен ли он быть разговорным/понимаемым. Пример: 'self language:add "Canilunzt" true true'
command-description-language-rm = Удаляет язык из подключённой сущности. Работает аналогично language:add. Пример: 'self language:rm "GalacticCommon" true true'.
command-description-language-lsspoken = Перечисляет все языки, на которых сущность может говорить. Пример: 'self language:lsspoken'
command-description-language-lsunderstood = Перечисляет все языки, которые сущность может понимать. Пример: 'self language:lssunderstood'
command-description-translator-addlang = Добавляет новый целевой язык к подключённой сущности-переводчику. См. language:add для деталей.
command-description-translator-rmlang = Удаляет целевой язык из подключённой сущности-переводчика. См. language:rm для деталей.
command-description-translator-addrequired = Добавляет новый требуемый язык к подключённой сущности-переводчику. Пример: 'ent 1234 translator:addrequired "GalacticCommon"'
command-description-translator-rmrequired = Удаляет требуемый язык из подключённой сущности-переводчика. Пример: 'ent 1234 translator:rmrequired "GalacticCommon"'
command-description-translator-lsspoken = Перечисляет все разговорные языки для подключённой сущности-переводчика. Пример: 'ent 1234 translator:lsspoken'
command-description-translator-lsunderstood = Перечисляет все понимаемые языки для подключённой сущности-переводчика. Пример: 'ent 1234 translator:lssunderstood'
command-description-translator-lsrequired = Перечисляет все требуемые языки для подключённой сущности-переводчика. Пример: 'ent 1234 translator:lsrequired'
command-language-error-this-will-not-work = Это не сработает.
command-language-error-not-a-translator = Сущность { $entity } не является переводчиком.
