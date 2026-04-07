import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { WordkiApiError, type GroupCard, type UserCardGroup } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import { CardFormModal } from '../components/CardFormModal'
import { useWordkiBackend } from '../hooks/useWordkiBackend'
import { filterCardsByLabels } from '../lib/listSearch'
import trashIconSvg from '@wordki/shared/icons/trash.svg?raw'
import './GroupDetailPage.css'
import './RegisterPage.css'

type LocationState = { group?: UserCardGroup } | null

export function GroupDetailPage() {
  const { groupId: groupIdParam } = useParams<{ groupId: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, user } = useAuth()
  const api = useWordkiBackend()

  const groupId = Number(groupIdParam)

  const [groupMeta, setGroupMeta] = useState<UserCardGroup | null>(null)
  const [cards, setCards] = useState<GroupCard[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dataVersion, setDataVersion] = useState(0)

  const [modalOpen, setModalOpen] = useState(false)
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create')
  const [editingCard, setEditingCard] = useState<GroupCard | null>(null)
  const [wordSearch, setWordSearch] = useState('')
  const [deletingCardId, setDeletingCardId] = useState<number | null>(null)

  useEffect(() => {
    setWordSearch('')
  }, [groupId])

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', {
        replace: true,
        state: { from: location.pathname },
      })
    }
  }, [isAuthenticated, location.pathname, navigate])

  useEffect(() => {
    if (!user?.id || !Number.isFinite(groupId) || groupId <= 0) {
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)
    setCards([])

    const fromNav = (location.state as LocationState)?.group
    const prefilled = fromNav?.id === groupId ? fromNav : null

    void (async () => {
      if (prefilled) {
        if (!cancelled) {
          setGroupMeta(prefilled)
        }
      } else {
        try {
          const groups = await api.getUserCardGroups(user.id)
          if (cancelled) return
          setGroupMeta(groups.find((g) => g.id === groupId) ?? null)
        } catch {
          if (!cancelled) {
            setGroupMeta(null)
          }
        }
      }

      try {
        const list = await api.getGroupCards(user.id, groupId)
        if (!cancelled) {
          setCards(list)
          setError(null)
        }
      } catch (err) {
        if (cancelled) return
        if (err instanceof WordkiApiError) {
          setError(
            err.errors[0]?.message ??
              `Could not load cards (${err.status || 'error'}).`,
          )
          setCards([])
        } else {
          setError(err instanceof Error ? err.message : 'Something went wrong.')
          setCards([])
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [api, groupId, location.state, user?.id, dataVersion])

  function openCreateCard() {
    setModalMode('create')
    setEditingCard(null)
    setModalOpen(true)
  }

  function openEditCard(c: GroupCard) {
    setModalMode('edit')
    setEditingCard(c)
    setModalOpen(true)
  }

  function closeCardModal() {
    setModalOpen(false)
    setEditingCard(null)
  }

  function refetchCards() {
    setDataVersion((v) => v + 1)
  }

  async function handleDeleteCard(c: GroupCard) {
    if (
      !user?.id ||
      !window.confirm(
        'Delete this card? This cannot be undone.',
      )
    ) {
      return
    }
    setError(null)
    setDeletingCardId(c.id)
    try {
      await api.deleteCard(user.id, c.id)
      refetchCards()
    } catch (err) {
      if (err instanceof WordkiApiError) {
        setError(
          err.errors[0]?.message ??
            `Could not delete card (${err.status ?? 'error'}).`,
        )
      } else {
        setError(err instanceof Error ? err.message : 'Could not delete card.')
      }
    } finally {
      setDeletingCardId(null)
    }
  }

  if (!isAuthenticated || !user) {
    return null
  }

  if (!Number.isFinite(groupId) || groupId <= 0) {
    return (
      <main className="register-page">
        <div className="register-page__card">
          <p className="register-page__lead">Invalid group.</p>
          <Link to="/groups" className="register-page__back">
            ← Back to groups
          </Link>
        </div>
      </main>
    )
  }

  const title = groupMeta?.name ?? 'Group'
  const subtitle =
    groupMeta != null
      ? `${groupMeta.frontSideType} · ${groupMeta.backSideType}`
      : null

  const filteredCards = useMemo(
    () => filterCardsByLabels(cards, wordSearch),
    [cards, wordSearch],
  )

  return (
    <main className="register-page group-detail-page">
      <div className="register-page__card group-detail-page__inner">
        <nav className="group-detail-page__back-nav" aria-label="Group navigation">
          <Link to="/groups" className="register-page__back group-detail-page__back-link">
            ← Back to groups
          </Link>
        </nav>
        <div className="group-detail-page__toolbar">
          <div>
            <h1 className="register-page__title group-detail-page__title-inline">
              {title}
            </h1>
            {subtitle ? (
              <p className="group-detail-page__subtitle group-detail-page__subtitle-tight">
                {subtitle}
              </p>
            ) : null}
          </div>
          <button
            type="button"
            className="group-detail-page__add-card-btn"
            onClick={openCreateCard}
          >
            Add card
          </button>
        </div>

        {error && (
          <div className="register-page__banner" role="alert">
            {error}
          </div>
        )}

        {loading ? (
          <p className="group-detail-page__status" aria-live="polite">
            Loading words…
          </p>
        ) : cards.length === 0 && !error ? (
          <p className="group-detail-page__empty">
            No words in this group yet. Use &quot;Add card&quot; to create one.
          </p>
        ) : cards.length > 0 ? (
          <>
            <div className="group-detail-page__search">
              <input
                id="group-detail-words-search"
                type="search"
                className="group-detail-page__search-input"
                placeholder="Search…"
                value={wordSearch}
                onChange={(e) => setWordSearch(e.target.value)}
                autoComplete="off"
                spellCheck={false}
              />
            </div>
            {filteredCards.length === 0 ? (
              <p className="group-detail-page__search-empty" role="status">
                No words match your search.
              </p>
            ) : (
              <ul
                className="group-detail-page__list"
                aria-label="Words in this group. Select a card to edit."
              >
                {filteredCards.map((c) => (
                  <li key={c.id} className="group-detail-page__row-wrap">
                    <div className="group-detail-page__row-card">
                      <button
                        type="button"
                        className="group-detail-page__row"
                        onClick={() => openEditCard(c)}
                        aria-label={`Edit card, front: ${c.front.label}. Back: ${c.back.label}.`}
                      >
                        <span className="group-detail-page__row-content">
                          <span className="group-detail-page__word">
                            <span className="group-detail-page__label">Front</span>
                            <span className="group-detail-page__text">
                              {c.front.label}
                            </span>
                            {c.front.example ? (
                              <span className="group-detail-page__sub">
                                {c.front.example}
                              </span>
                            ) : null}
                          </span>
                          <span className="group-detail-page__word">
                            <span className="group-detail-page__label">Back</span>
                            <span className="group-detail-page__text">
                              {c.back.label}
                            </span>
                            {c.back.example ? (
                              <span className="group-detail-page__sub">
                                {c.back.example}
                              </span>
                            ) : null}
                          </span>
                        </span>
                      </button>
                      <button
                        type="button"
                        className="group-detail-page__delete-btn"
                        disabled={deletingCardId === c.id}
                        onClick={(e) => {
                          e.stopPropagation()
                          void handleDeleteCard(c)
                        }}
                        aria-label={`Delete card: ${c.front.label}`}
                      >
                        {deletingCardId === c.id ? (
                          <span
                            className="group-detail-page__delete-loading"
                            aria-hidden
                          >
                            …
                          </span>
                        ) : (
                          <span
                            className="group-detail-page__delete-icon-wrap"
                            aria-hidden
                            dangerouslySetInnerHTML={{ __html: trashIconSvg }}
                          />
                        )}
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </>
        ) : null}
      </div>

      <CardFormModal
        open={modalOpen}
        mode={modalMode}
        groupId={groupId}
        card={editingCard}
        userId={user.id}
        frontSideType={groupMeta?.frontSideType}
        backSideType={groupMeta?.backSideType}
        onClose={closeCardModal}
        onSaved={refetchCards}
      />
    </main>
  )
}
