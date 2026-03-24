# Fulgur Fangs Docs

Основная документация проекта теперь хранится в папке `docs` и разбита по темам.

## Структура

- `docs/project/overview.md`
  Базовый контекст проекта, source of truth и активная структура мода.
- `docs/electricity/network-mvp.md`
  Текущая архитектура электричества, роли зданий и базовые правила симуляции.
- `docs/electricity/consumers-and-workshops.md`
  Паттерн электрических потребителей и шаблон для новых электрических мастерских.
- `docs/electricity/ui-and-selection.md`
  Правила UI карточек зданий, скрытие механического блока и подсветка сети.
- `docs/electricity/range-overlays.md`
  Практика по range overlay и грабли с сетками радиуса.
- `docs/content/implemented-mechanics.md`
  Реализованные игровые механики и уже доступный контент фракции.
- `docs/content/resource-chains.md`
  Целевой дизайн ресурсных, электрических и энергетических цепочек фракции.
- `docs/content/building-models-and-slots.md`
  Практика по подмене `Timbermesh`, слотам моделей и тестовой интеграции новых зданий.
- `docs/debugging/troubleshooting.md`
  Чеклист диагностики, подтвержденные фиксы и рабочие правила изменений.

## Быстрые маршруты

- Если добавляешь новое электрическое производственное здание:
  начни с `docs/electricity/consumers-and-workshops.md`.
- Если меняешь UI карточек электрических зданий:
  смотри `docs/electricity/ui-and-selection.md`.
- Если добавляешь ranged-здание:
  сначала смотри `docs/electricity/range-overlays.md`.
- Если меняешь или импортируешь 3D-модель здания:
  сначала смотри `docs/content/building-models-and-slots.md`.
- Если планируешь новые ресурсы, топливо или производственные цепочки:
  сначала смотри `docs/content/resource-chains.md`.
- Если что-то ломается в игре:
  начни с `docs/debugging/troubleshooting.md`.
