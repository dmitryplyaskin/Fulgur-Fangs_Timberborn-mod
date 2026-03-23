Это проект мода Fulgur Fangs для игры timberborn версии v1.0

Этот мод добавляет новую фракцию в игру которая основана на электричестве и грязной воде. 
На данный момент за основу нужно взять фракцию Iron Teeth и использовать ее здания и ассеты.

Вот путь до другого мода, который создает новую фракцию в игре, его можно использовать как пример
F:\SteamLibrary\steamapps\workshop\content\1062090\3346318229

все официальный блупринты из игры находятся по пути:
F:\SteamLibrary\steamapps\common\Timberborn\Timberborn_Data\StreamingAssets\Modding\Blueprints

В проекте так же скачена официальная вики по моддингу и находится в папке timberborn-modding.wiki


путь краш репортов из игры C:\Users\dima2\OneDrive\Документы\Timberborn\Error reports
там хранятся архивы

не большой пример мода темплейта
C:\Users\dima2\OneDrive\Документы\Timberborn\Mods\examples

важно:
- активная версия мода лежит в папке version-1.0, игру надо ориентировать именно на нее
- основная документация теперь лежит в папке `docs`
- точка входа по документации: `docs/README.md`
- `DEVELOPMENT.md` и `МЕХАНИКИ.md` теперь это короткие индексные файлы, а не основное место хранения знаний
- перед добавлением нового электрического производственного здания сначала сверяться с
  `docs/electricity/consumers-and-workshops.md`
  и `docs/electricity/ui-and-selection.md`
- при диагностике текущих ошибок сначала смотреть Player.log:
  C:\Users\dima2\AppData\LocalLow\Mechanistry\Timberborn\Player.log
- вся практика по range overlay и сеткам радиуса теперь хранится в
  `docs/electricity/range-overlays.md`;
  перед добавлением новых ranged-зданий сначала сверяться с ним
