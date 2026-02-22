@echo off
title SS14 Locale Manager

echo Инициализация среды и проверка зависимостей...
call pip install -r requirements.txt --quiet --no-warn-script-location

echo Запуск менеджера локализации...
python locale_manager.py

pause