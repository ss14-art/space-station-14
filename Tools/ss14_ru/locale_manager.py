import os
import sys
import shutil
import logging
import subprocess
from datetime import datetime

try:
    from fluent.syntax import FluentParser, FluentSerializer, ast
except ImportError:
    print("[CRITICAL] Отсутствуют необходимые библиотеки. Выполните 'pip install -r requirements.txt'")
    sys.exit(1)

# =====================================================================
# КОНФИГУРАЦИЯ ПУТЕЙ
# =====================================================================
def find_top_level_dir(start_dir):
    current_dir = start_dir
    while True:
        dir_files = os.listdir(current_dir)
        if 'SpaceStation14.slnx' in dir_files or 'SpaceStation14.sln' in dir_files:
            return current_dir
        parent_dir = os.path.dirname(current_dir)
        if parent_dir == current_dir:
            print("[CRITICAL] Не удалось найти корень проекта (маркер: SpaceStation14.slnx или SpaceStation14.sln)")
            sys.exit(1)
        current_dir = parent_dir

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ROOT_DIR = find_top_level_dir(SCRIPT_DIR)
EN_DIR = os.path.join(ROOT_DIR, "Resources", "Locale", "en-US")
RU_DIR = os.path.join(ROOT_DIR, "Resources", "Locale", "ru-RU")
LOGS_DIR = os.path.join(SCRIPT_DIR, "logs")
BACKUPS_DIR = os.path.join(SCRIPT_DIR, "backups")
REPORT_FILE = os.path.join(SCRIPT_DIR, "untranslated_report.txt")

for directory in [LOGS_DIR, BACKUPS_DIR]:
    os.makedirs(directory, exist_ok=True)

# =====================================================================
# НАСТРОЙКА ЛОГИРОВАНИЯ
# =====================================================================
log_filename = os.path.join(LOGS_DIR, f"manager_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log")
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler(log_filename, encoding='utf-8'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)

parser = FluentParser()
serializer = FluentSerializer(with_junk=True)

# =====================================================================
# ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
# =====================================================================
def get_id(node):
    if isinstance(node, (ast.Message, ast.Term)):
        return node.id.name
    return None

def get_pattern_text(pattern):
    if not pattern:
        return ""
    dummy_msg = ast.Message(id=ast.Identifier('d'), value=pattern)
    serialized_str = FluentSerializer(with_junk=False).serialize(ast.Resource([dummy_msg]))
    if "=" in serialized_str:
        return serialized_str.split("=", 1)[1].strip()
    return ""

def parse_value_to_pattern(text):
    indented_text = text.replace('\n', '\n    ')
    dummy_ftl = f"dummy = {indented_text}\n"
    ast_tree = parser.parse(dummy_ftl)
    if ast_tree.body and isinstance(ast_tree.body[0], ast.Message):
        return ast_tree.body[0].value
    return None

def create_backup():
    """Создает ZIP-архив текущего состояния папки ru-RU."""
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_name = os.path.join(BACKUPS_DIR, f"backup_ru_{timestamp}")
    try:
        shutil.make_archive(backup_name, 'zip', RU_DIR)
        logger.info(f"Резервная копия создана: {backup_name}.zip")
        return True
    except Exception as e:
        logger.error(f"Ошибка создания резервной копии: {e}")
        return False

# =====================================================================
# ОСНОВНЫЕ МОДУЛИ
# =====================================================================
def run_external_script(script_name):
    script_path = os.path.join(SCRIPT_DIR, script_name)
    if not os.path.exists(script_path):
        logger.error(f"Скрипт {script_name} не найден.")
        return False
    
    logger.info(f"Запуск внешнего скрипта: {script_name}...")
    try:
        subprocess.run([sys.executable, script_path], cwd=SCRIPT_DIR, check=True)
        logger.info(f"Скрипт {script_name} успешно завершен.")
        return True
    except subprocess.CalledProcessError as e:
        logger.error(f"Скрипт {script_name} завершился с ошибкой: {e}")
        return False

def sync_locales():
    """Синхронизация ключей между EN и RU."""
    logger.info("Запуск синхронизации локализаций (EN -> RU).")
    
    if not create_backup():
        logger.warning("Синхронизация прервана из-за ошибки резервного копирования.")
        return

    en_files = [os.path.join(root, f) for root, _, files in os.walk(EN_DIR) for f in files if f.endswith('.ftl')]
    ru_files = [os.path.join(root, f) for root, _, files in os.walk(RU_DIR) for f in files if f.endswith('.ftl')]

    changes_count = 0

    # Проход 1: Добавление отсутствующих файлов и ключей
    for en_path in en_files:
        rel_path = os.path.relpath(en_path, EN_DIR)
        ru_path = os.path.join(RU_DIR, rel_path)
        
        with open(en_path, 'r', encoding='utf-8') as f:
            en_ast = parser.parse(f.read().replace('\ufeff', ''))
        
        en_keys = {get_id(node): node for node in en_ast.body if get_id(node)}

        if not os.path.exists(ru_path):
            os.makedirs(os.path.dirname(ru_path), exist_ok=True)
            with open(ru_path, 'w', encoding='utf-8') as f:
                f.write(serializer.serialize(en_ast))
            logger.info(f"Создан новый файл: {rel_path} ({len(en_keys)} ключей)")
            changes_count += 1
            continue

        with open(ru_path, 'r', encoding='utf-8') as f:
            ru_ast = parser.parse(f.read().replace('\ufeff', ''))
        
        ru_keys = set()
        new_ru_body = []
        added_keys = []
        removed_keys = []
        
        for node in ru_ast.body:
            node_id = get_id(node)
            if node_id:
                if node_id not in en_keys:
                    removed_keys.append(node_id)
                    continue
                ru_keys.add(node_id)
            new_ru_body.append(node)
            
        ru_ast.body = new_ru_body
        
        for node in en_ast.body:
            node_id = get_id(node)
            if node_id and node_id not in ru_keys:
                added_keys.append(node_id)
                ru_ast.body.append(node)
                
        if added_keys or removed_keys:
            with open(ru_path, 'w', encoding='utf-8') as f:
                f.write(serializer.serialize(ru_ast))
            if added_keys:
                logger.info(f"Обновлен [{rel_path}]: Добавлено ключей - {len(added_keys)}")
            if removed_keys:
                logger.info(f"Обновлен [{rel_path}]: Удалено устаревших ключей - {len(removed_keys)}")
            changes_count += 1

    # Проход 2: Удаление устаревших файлов
    for ru_path in ru_files:
        rel_path = os.path.relpath(ru_path, RU_DIR)
        en_path = os.path.join(EN_DIR, rel_path)
        
        if not os.path.exists(en_path):
            os.remove(ru_path)
            try:
                os.rmdir(os.path.dirname(ru_path))
            except OSError:
                pass
            logger.info(f"Удален неактуальный файл: {rel_path}")
            changes_count += 1

    if changes_count == 0:
        logger.info("Синхронизация не выявила изменений. Локали идентичны структуре EN.")
    else:
        logger.info("Синхронизация успешно завершена.")

def _get_untranslated_data():
    untranslated_data = {}
    en_files = [os.path.join(root, f) for root, _, files in os.walk(EN_DIR) for f in files if f.endswith('.ftl')]

    for en_path in en_files:
        rel_path = os.path.relpath(en_path, EN_DIR)
        ru_path = os.path.join(RU_DIR, rel_path)
        if not os.path.exists(ru_path): continue

        with open(en_path, 'r', encoding='utf-8') as f:
            en_ast = parser.parse(f.read().replace('\ufeff', ''))
        with open(ru_path, 'r', encoding='utf-8') as f:
            ru_ast = parser.parse(f.read().replace('\ufeff', ''))

        en_nodes = {get_id(n): n for n in en_ast.body if get_id(n)}
        ru_nodes = {get_id(n): n for n in ru_ast.body if get_id(n)}
        file_untranslated = []

        for key, ru_node in ru_nodes.items():
            if key in en_nodes:
                en_node = en_nodes[key]
                en_val, ru_val = get_pattern_text(en_node.value), get_pattern_text(ru_node.value)

                if en_val and en_val == ru_val and any(c.isalpha() for c in en_val):
                    file_untranslated.append((key, 'VALUE', en_val))

                en_attrs = {a.id.name: a for a in getattr(en_node, 'attributes', [])}
                ru_attrs = {a.id.name: a for a in getattr(ru_node, 'attributes', [])}
                
                for attr_name, en_attr in en_attrs.items():
                    if attr_name in ru_attrs:
                        ru_attr = ru_attrs[attr_name]
                        en_attr_text, ru_attr_text = get_pattern_text(en_attr.value), get_pattern_text(ru_attr.value)
                        if en_attr_text and en_attr_text == ru_attr_text and any(c.isalpha() for c in en_attr_text):
                            file_untranslated.append((key, f'ATTR:{attr_name}', en_attr_text))

        if file_untranslated:
            untranslated_data[rel_path] = file_untranslated

    return untranslated_data

def generate_report():
    logger.info("Поиск непереведенных строк и формирование отчета.")
    untranslated_data = _get_untranslated_data()
    
    if not untranslated_data:
        logger.info("Все строки переведены. Отчет пуст.")
        return
    
    # Считаем уникальные ключи для статистики
    total_unique_keys = 0
    for items in untranslated_data.values():
        unique_keys = set(k for k, _, _ in items)
        total_unique_keys += len(unique_keys)
        
    with open(REPORT_FILE, "w", encoding="utf-8") as f:
        f.write("============================================================\n")
        f.write(f"ОТЧЕТ О НЕПЕРЕВЕДЕННЫХ СТРОКАХ. ВСЕГО УНИКАЛЬНЫХ КЛЮЧЕЙ: {total_unique_keys}\n")
        f.write(f"ДАТА ГЕНЕРАЦИИ: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        f.write("============================================================\n\n")
        
        for path, items in untranslated_data.items():
            # Извлекаем только уникальные названия ключей с сохранением порядка
            unique_keys_in_file = []
            for key, _, _ in items:
                if key not in unique_keys_in_file:
                    unique_keys_in_file.append(key)
            
            f.write(f"Файл: {path} (Осталось: {len(unique_keys_in_file)})\n")
            f.write(f"Ключи: {', '.join(unique_keys_in_file)}\n\n")
            
    logger.info(f"Отчет сгенерирован: {REPORT_FILE}")

def interactive_translation():
    logger.info("Запуск интерактивного режима перевода.")
    data = _get_untranslated_data()
    if not data:
        print("\n[INFO] Отсутствуют строки, требующие перевода.")
        return

    # Считаем общее количество уникальных ключей для перевода
    total_keys = sum(len(set(k for k, _, _ in items)) for items in data.values())
    print(f"\n[INFO] Доступно для перевода: {total_keys} уникальных ключей.")
    print("Управление:")
    print("  [Enter] - Пропустить ключ")
    print("  [q]     - Сохранить прогресс и выйти")
    print("  [\\n]    - Использовать для обозначения переноса строки\n")

    for rel_path, items in data.items():
        ru_path = os.path.join(RU_DIR, rel_path)
        with open(ru_path, 'r', encoding='utf-8') as f:
            ru_ast = parser.parse(f.read().replace('\ufeff', ''))
            
        file_modified = False
        for key, part_type, en_text in items:
            print("-" * 60)
            print(f"FILE:     {rel_path}")
            print(f"KEY:      {key} [{part_type}]")
            print(f"ORIGINAL: {en_text}")
            
            user_input = input("TRANSLATE > ").strip()
            
            if user_input.lower() == 'q':
                if file_modified:
                    with open(ru_path, 'w', encoding='utf-8') as f:
                        f.write(serializer.serialize(ru_ast))
                    logger.info(f"Прогресс сохранен в {rel_path}.")
                return
            if user_input == "":
                continue
                
            new_pattern = parse_value_to_pattern(user_input.replace("\\n", "\n"))
            if new_pattern:
                ru_node = next((n for n in ru_ast.body if get_id(n) == key), None)
                if ru_node:
                    if part_type == 'VALUE':
                        ru_node.value = new_pattern
                    elif part_type.startswith('ATTR:'):
                        attr_name = part_type.split(':')[1]
                        for a in ru_node.attributes:
                            if a.id.name == attr_name:
                                a.value = new_pattern
                    file_modified = True
                    print("[INFO] Успешно применено.")

        if file_modified:
            with open(ru_path, 'w', encoding='utf-8') as f:
                f.write(serializer.serialize(ru_ast))
            logger.info(f"Файл обновлен: {rel_path}")

def rollback():
    """Восстанавливает папку ru-RU из последнего ZIP-архива."""
    if not os.path.exists(BACKUPS_DIR):
        logger.warning("Папка резервных копий не найдена.")
        return

    backups = sorted([f for f in os.listdir(BACKUPS_DIR) if f.endswith(".zip")], reverse=True)
    if not backups:
        logger.warning("Резервные копии не найдены.")
        return
    
    latest_backup = backups[0]
    backup_path = os.path.join(BACKUPS_DIR, latest_backup)
    
    confirm = input(f"\nВы уверены, что хотите восстановить состояние из архива '{latest_backup}'? Все текущие изменения будут потеряны. (y/n): ").strip().lower()
    if confirm != 'y':
        logger.info("Операция отката отменена пользователем.")
        return

    logger.info(f"Запуск восстановления из архива: {backup_path}")
    try:
        shutil.rmtree(RU_DIR)
        os.makedirs(RU_DIR, exist_ok=True)
        shutil.unpack_archive(backup_path, RU_DIR, 'zip')
        logger.info("Восстановление успешно завершено.")
    except Exception as e:
        logger.error(f"Критическая ошибка при восстановлении: {e}")

def run_full_pipeline():
    logger.info("Запуск полного конвейера обработки локализации.")
    if run_external_script("yamlextractor.py"):
        sync_locales()
        run_external_script("clean_duplicates.py")
        run_external_script("clean_empty.py")
        logger.info("Полный конвейер успешно завершен.")
    else:
        logger.error("Конвейер прерван из-за ошибки в yamlextractor.py.")

# =====================================================================
# ИНТЕРФЕЙС КОМАНДНОЙ СТРОКИ
# =====================================================================
def main():
    while True:
        print("\n" + "="*70)
        print(" СИСТЕМА УПРАВЛЕНИЯ ЛОКАЛИЗАЦИЕЙ SS14 (LOCALE MANAGER)".center(70))
        print("="*70)
        print("  1. Выполнить полный конвейер (Извлечение -> Синхронизация -> Очистка)")
        print("  2. Извлечь YAML прототипы (yamlextractor.py)")
        print("  3. Синхронизировать структуру ключей (EN -> RU)")
        print("  4. Удалить дубликаты (clean_duplicates.py)")
        print("  5. Удалить пустые файлы (clean_empty.py)")
        print("  6. Интерактивный ассистент перевода")
        print("  7. Сгенерировать отчет о непереведенных строках (Текстовый файл)")
        print("  8. Восстановить из резервной копии (Откат)")
        print("  0. Выход")
        print("="*70)
        
        choice = input("Выберите операцию [0-8]: ").strip()
        
        if choice == '1':
            run_full_pipeline()
        elif choice == '2':
            run_external_script("yamlextractor.py")
        elif choice == '3':
            sync_locales()
        elif choice == '4':
            run_external_script("clean_duplicates.py")
        elif choice == '5':
            run_external_script("clean_empty.py")
        elif choice == '6':
            interactive_translation()
        elif choice == '7':
            generate_report()
        elif choice == '8':
            rollback()
        elif choice == '0':
            logger.info("Завершение работы программы.")
            break
        else:
            print("[WARN] Неверный ввод. Пожалуйста, выберите существующий пункт меню.")

if __name__ == '__main__':
    main()