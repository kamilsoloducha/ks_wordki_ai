-- =============================================================================
-- Wordki — dane testowe dla WSZYSTKICH użytkowników w cards.users
-- =============================================================================
-- Dla każdego użytkownika modułu Cards:
--   • 10 grup (Test data group 1 … 10, EN/PL, UserOwned)
--   • w każdej grupie losowo 20–30 kart
--   • etykiety: front "test {typ_front} {n}", back "test {typ_back} {n}" (np. test en 1, test pl 1;
--     typ jak w grupie, małymi literami; numeracja od 1 osobno na użytkownika)
--   • dla każdej strony karty: wiersz w cards.results z losowym drawer (0–10),
--     counter (0–50), next_repeat_utc losowo w [początek dnia UTC wczoraj,
--     koniec dnia UTC za 10 dni]
--
-- Wymaga istniejących tabel (po migracjach). Nie usuwa danych — tylko INSERT.
--
-- Uruchomienie:
--   psql -h localhost -U wordki -d wordki_dev -v ON_ERROR_STOP=1 \
--     -f Documentation/sql/seed-test-data-all-users.sql
-- (ścieżkę do pliku dostosuj do katalogu, z którego wywołujesz psql)
--
-- Gdy cards.users jest puste, skrypt kończy się komunikatem i nic nie wstawia.
-- =============================================================================

DO $$
DECLARE
  r_user       RECORD;
  g            int;
  c            int;
  n_cards      int;
  v_group_id   bigint;
  v_front_id   bigint;
  v_back_id    bigint;
  word_idx     int;
  v_lo         timestamptz;
  v_hi         timestamptz;
  v_next_front timestamptz;
  v_next_back  timestamptz;
  v_front_type text := 'EN';
  v_back_type  text := 'PL';
  v_label_front text;
  v_label_back  text;
  user_count   int;
BEGIN
  SELECT count(*)::int INTO user_count FROM cards.users;

  IF user_count = 0 THEN
    RAISE NOTICE 'cards.users jest puste — brak użytkowników do zasilenia.';
    RETURN;
  END IF;

  RAISE NOTICE 'Zasilanie danymi testowymi dla % użytkowników (cards.users)...', user_count;

  -- Zakres czasu jak w skrypcie Node: od początku „wczoraj” UTC do końca dnia za 10 dni UTC
  v_lo :=
    (date_trunc('day', (clock_timestamp() AT TIME ZONE 'utc')) - interval '1 day')
    AT TIME ZONE 'utc';
  v_hi :=
    (
      date_trunc('day', (clock_timestamp() AT TIME ZONE 'utc'))
      + interval '10 days'
      + interval '1 day'
      - interval '1 microsecond'
    ) AT TIME ZONE 'utc';

  FOR r_user IN
    SELECT id FROM cards.users ORDER BY id
  LOOP
    word_idx := 0;

    FOR g IN 1 .. 10
    LOOP
      INSERT INTO cards.groups (
        user_id,
        name,
        front_side_type,
        back_side_type,
        type
      )
      VALUES (
        r_user.id,
        'Test data group ' || g,
        v_front_type,
        v_back_type,
        'UserOwned'
      )
      RETURNING id INTO v_group_id;

      n_cards := 20 + floor(random() * 11)::int;

      FOR c IN 1 .. n_cards
      LOOP
        word_idx := word_idx + 1;
        v_label_front := 'test ' || lower(v_front_type) || ' ' || word_idx::text;
        v_label_back := 'test ' || lower(v_back_type) || ' ' || word_idx::text;

        INSERT INTO cards.card_sides (label, example, comment)
        VALUES (v_label_front, '', '')
        RETURNING id INTO v_front_id;

        INSERT INTO cards.card_sides (label, example, comment)
        VALUES (v_label_back, '', '')
        RETURNING id INTO v_back_id;

        INSERT INTO cards.cards (group_id, front_side_id, back_side_id)
        VALUES (v_group_id, v_front_id, v_back_id);

        v_next_front := v_lo + random() * (v_hi - v_lo);
        v_next_back := v_lo + random() * (v_hi - v_lo);

        INSERT INTO cards.results (
          user_id,
          group_id,
          card_side_id,
          drawer,
          next_repeat_utc,
          counter
        )
        VALUES (
          r_user.id,
          v_group_id,
          v_front_id,
          floor(random() * 11)::int,
          v_next_front,
          floor(random() * 51)::int
        );

        INSERT INTO cards.results (
          user_id,
          group_id,
          card_side_id,
          drawer,
          next_repeat_utc,
          counter
        )
        VALUES (
          r_user.id,
          v_group_id,
          v_back_id,
          floor(random() * 11)::int,
          v_next_back,
          floor(random() * 51)::int
        );
      END LOOP;
    END LOOP;

    RAISE NOTICE 'Użytkownik cards.users.id=%: dodano 10 grup, łącznie % kart (etykiety: test en/pl 1 … %).',
      r_user.id, word_idx, word_idx;
  END LOOP;

  RAISE NOTICE 'Gotowe.';
END;
$$;
