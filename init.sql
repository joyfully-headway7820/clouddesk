CREATE TABLE tickets (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title VARCHAR(120) NOT NULL,
    description TEXT NOT NULL,
    priority INTEGER NOT NULL CHECK (priority BETWEEN 1 AND 3),
    status VARCHAR(20) NOT NULL DEFAULT 'new' CHECK (status IN ('new', 'in_progress', 'done')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE ticket_events (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ticket_id INTEGER NOT NULL REFERENCES tickets (id) ON DELETE CASCADE,
    old_status VARCHAR(20),
    new_status VARCHAR(20) NOT NULL,
    changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (new_status IN ('new', 'in_progress', 'done')),
    CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'done'))
);

CREATE INDEX ticket_events_ticket_id_idx ON ticket_events (ticket_id);

INSERT INTO tickets (title, description, priority, status, created_at)
VALUES
    ('Не печатает принтер', 'Принтер на третьем этаже не берёт задания из очереди.', 2, 'new', TIMESTAMPTZ '2026-11-01 09:00:00+00'),
    ('Нет доступа к VPN', 'Клиент VPN обрывается через минуту после подключения.', 1, 'new', TIMESTAMPTZ '2026-11-02 10:15:00+00'),
    ('Завис 1С', 'База 1С не открывается у бухгалтерии с утра.', 3, 'in_progress', TIMESTAMPTZ '2026-11-03 08:40:00+00'),
    ('Заменить клавиатуру', 'На клавиатуре не работают клавиши в правом блоке.', 1, 'in_progress', TIMESTAMPTZ '2026-11-04 12:05:00+00'),
    ('Настроить почту', 'Новому сотруднику нужна корпоративная почта.', 2, 'done', TIMESTAMPTZ '2026-11-05 14:20:00+00'),
    ('Пропал интернет в переговорной', 'В переговорной B нет сети на всех розетках.', 3, 'done', TIMESTAMPTZ '2026-11-06 16:45:00+00');

INSERT INTO ticket_events (ticket_id, old_status, new_status, changed_at)
SELECT id, NULL, status, created_at
FROM tickets;
