#!/usr/bin/env sh

if [ "$(dirname $0)" != "." ]; then
    cd "$(dirname $0)"
fi

echo "Инициализация среды и проверка зависимостей..."
pip install -r requirements.txt --quiet --no-warn-script-location

echo "Запуск менеджера локализации..."
python3 locale_manager.py