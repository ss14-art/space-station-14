### Examine

gas-turbine-examine-stator-null = Похоже, статор отсутствует.
gas-turbine-examine-stator = Статор установлен.
gas-turbine-examine-blade-null = Похоже, лопасть турбины отсутствует.
gas-turbine-examine-blade = Лопасть турбины установлена.
turbine-spinning-0 = Лопасти не вращаются.
turbine-spinning-1 = Лопасти вращаются медленно.
turbine-spinning-2 = Лопасти вращаются.
turbine-spinning-3 = Лопасти вращаются быстро.
turbine-spinning-4 = [color=red]Лопасти вращаются бесконтрольно![/color]
turbine-damaged-0 = Состояние хорошее.[/color]
turbine-damaged-1 = Турбина выглядит немного потёртой.[/color]
turbine-damaged-2 = [color=yellow]Турбина сильно повреждена.[/color]
turbine-damaged-3 = [color=orange]Критические повреждения![/color]
turbine-ruined = [color=red]Полностью разрушена![/color]

### Popups

# Shown when an event occurs
turbine-overheat = { $owner } открывает аварийный клапан сброса перегрева!
turbine-explode = { $owner } разрывает на части!
# Shown when damage occurs
turbine-spark = { $owner } начинает искрить!
turbine-spark-stop = { $owner } перестаёт искрить.
turbine-smoke = { $owner } начинает дымить!
turbine-smoke-stop = { $owner } перестаёт дымить.
# Shown during repairs
gas-turbine-repair-fail-blade = Нужно заменить лопасть турбины перед ремонтом.
gas-turbine-repair-fail-stator = Нужно заменить статор перед ремонтом.
turbine-repair-ruined = Вы ремонтируете корпус { $target } с помощью { $tool }.
turbine-repair = Вы устраняете часть повреждений { $target } с помощью { $tool }.
turbine-no-damage = Нет повреждений для ремонта на { $target } с помощью { $tool }.
turbine-show-damage = Здоровье лопастей { $health }, Макс. здоровье лопастей { $healthMax }.
# Anchoring warnings
turbine-unanchor-warning = Нельзя открепить газовую турбину, пока лопасти вращаются!
turbine-anchor-warning = Недопустимая позиция крепления.
gas-turbine-eject-fail-speed = Нельзя извлечь детали турбины, пока она вращается!
gas-turbine-insert-fail-speed = Нельзя вставить детали турбины, пока она вращается!

### UI

# Shown when using the UI
comp-turbine-ui-tab-main = Управление
comp-turbine-ui-tab-parts = Детали
comp-turbine-ui-rpm = Об/мин
comp-turbine-ui-overspeed = ПЕРЕБОР СКОРОСТИ
comp-turbine-ui-overtemp = ПЕРЕГРЕВ
comp-turbine-ui-stalling = СРЫВ ПОТОКА
comp-turbine-ui-undertemp = НЕДОГРЕВ
comp-turbine-ui-flow-rate = Скорость потока
comp-turbine-ui-stator-load = Нагрузка статора
comp-turbine-ui-blade = Лопасть турбины
comp-turbine-ui-blade-integrity = Целостность
comp-turbine-ui-blade-stress = Напряжение
comp-turbine-ui-stator = Статор турбины
comp-turbine-ui-stator-potential = Потенциал
comp-turbine-ui-stator-supply = Питание
comp-turbine-ui-power = { POWERWATTS($power) }
comp-turbine-ui-locked-message = Управление заблокировано.
comp-turbine-ui-footer-left = Опасно: быстро движущиеся механизмы.
comp-turbine-ui-footer-right = 2.0 REV 1
