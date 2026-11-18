# CloudDesk

API заявок технической поддержки. ASP.NET Core Minimal API и PostgreSQL запускаются одной командой Docker Compose. Снаружи опубликован только порт API `8080`. Порт базы не публикуется: API ходит в неё по имени сервиса `db`.

Пароль в `.env.example` нужен только локальной базе в контейнере. Файл `.env` в репозиторий не коммитится.

## Запуск приложения

Нужны Docker и Docker Compose v2.

```bash
cp .env.example .env
docker compose up -d --build
```

`POSTGRES_PASSWORD` и пароль в `ConnectionStrings__Default` должны совпадать. Если в пароле есть символ `$`, в `.env` его нужно записать как `$$`: Compose подставляет переменные из этого файла.

Проверка:

```bash
curl -i http://localhost:8080/health
docker compose ps
docker compose logs -f api
```

`/health` отвечает `200` и `{"status":"ok"}`, когда API и база доступны. В логах `api` каждая строка запроса содержит метод, путь, query и код ответа.

Остановка без удаления данных: `docker compose down`. Полный сброс базы, вместе с томом `pgdata`: `docker compose down -v`.

### GitHub Codespaces

В репозитории есть `.devcontainer`. Новый Codespace ставит Docker, копирует `.env.example` в `.env`, если своего `.env` ещё нет, и выполняет `docker compose up -d --build`. Сборка образов занимает несколько минут: до её окончания порт `8080` не откроется.

Дальше:

1. Дождаться окончания post-create в логе создания Codespace.
2. Проверить `docker compose ps`: сервисы `api` и `db` в состоянии running, у `db` статус healthy.
3. На вкладке Ports открыть порт `8080`.
4. В терминале Codespace вызвать `curl -i http://localhost:8080/health` и `curl http://localhost:8080/api/tickets`. В браузере к адресу порта добавить путь `/health` или `/api/tickets`.

Если контейнеры не запущены:

```bash
cp -n .env.example .env
docker compose up -d --build
```

Секреты из другого окружения не нужны.

## HTTP-маршруты

Тело запросов и ответов — JSON. Поля в camelCase.

| Метод | Путь | Успех | Ошибка |
| --- | --- | --- | --- |
| GET | `/health` | `200`, база доступна | `503`, база недоступна |
| GET | `/api/tickets` | `200`, страница заявок | `400`, неверный query |
| GET | `/api/tickets/{id}` | `200`, одна заявка | `404`, нет такой заявки |
| POST | `/api/tickets` | `201`, созданная заявка и заголовок `Location` | `400`, неверное тело |
| PUT | `/api/tickets/{id}` | `200`, заявка после изменения | `400` или `404` |
| DELETE | `/api/tickets/{id}` | `204`, пустое тело | `404` |
| GET | `/api/stats` | `200`, общее число и число по каждому статусу | `503`, база недоступна |

`title` — от 3 до 120 символов. `description` — непустая строка. `priority` — целое число от 1 до 3. `status` — только `new`, `in_progress` или `done`. В POST поле `status` можно не передавать, тогда записывается `new`. В PUT статус обязателен.

Query `GET /api/tickets`:

- `status` — один из трёх статусов.
- `priority` — от 1 до 3.
- `q` — подстрока `title`, без учёта регистра. Символы `%` и `_` ищутся как обычный текст.
- `sort` — `asc` или `desc` по `createdAt`. По умолчанию `desc`.
- `page` — номер страницы с 1. По умолчанию `1`.
- `pageSize` — размер страницы от 1 до 100. По умолчанию `20`.

`total` в ответе — число заявок после фильтров, до нарезки на страницы.

```bash
curl "http://localhost:8080/api/tickets?status=new&priority=1&q=vpn&sort=asc&page=1&pageSize=20"

curl -i -X POST http://localhost:8080/api/tickets \
  -H "Content-Type: application/json" \
  -d '{"title":"Не открывается почта","description":"Outlook просит пароль по кругу.","priority":2}'

curl -i -X PUT http://localhost:8080/api/tickets/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"Не печатает принтер","description":"Очередь зависла.","priority":2,"status":"in_progress"}'

curl -i -X DELETE http://localhost:8080/api/tickets/2
```

Смена статуса пишется в `ticket_events`: id заявки, прежний статус, новый статус, время. При создании прежний статус пустой. Удаление заявки удаляет и её события.

```bash
docker compose exec db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "SELECT id, ticket_id, old_status, new_status, changed_at FROM ticket_events ORDER BY id;"'
```

## Проверка сохранности данных

Схема и шесть тестовых заявок создаются из `init.sql` только при первом запуске пустого тома `pgdata`.

1. Создать заявку и запомнить `id` из ответа `201`.
2. Остановить стек без удаления тома: `docker compose down`.
3. Запустить снова: `docker compose up -d`.
4. `GET /api/tickets/{id}` возвращает ту же заявку.

Остановка только базы. API остаётся запущенным, `/health` отвечает `503`, пока база не поднимется. После старта базы тот же процесс API снова отвечает `200`, перезапускать его не нужно. Ответ может идти несколько секунд: драйвер ждёт соединение.

```bash
docker compose stop db
curl -i http://localhost:8080/health
docker compose start db
curl -i http://localhost:8080/health
```

Пересборка API не трогает том базы. После неё старые заявки остаются на месте.

```bash
docker compose up -d --build api
curl http://localhost:8080/api/tickets
```
