import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import type { UserCardGroup } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import { GroupFormModal } from '../components/GroupFormModal'
import { useUserCardGroups } from '../hooks/useUserCardGroups'
import { filterGroupsByName } from '../lib/listSearch'
import editIconSvg from '@wordki/shared/icons/edit.svg?raw'
import './GroupsPage.css'
import './RegisterPage.css'

function cardLabel(count: number): string {
  return count === 1 ? '1 card' : `${count} cards`
}

export function GroupsPage() {
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()
  const { groups, loading, error, refetch } = useUserCardGroups(user?.id)

  const [modalOpen, setModalOpen] = useState(false)
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create')
  const [editingGroup, setEditingGroup] = useState<UserCardGroup | null>(null)
  const [groupSearch, setGroupSearch] = useState('')

  const filteredGroups = useMemo(
    () => filterGroupsByName(groups, groupSearch),
    [groups, groupSearch],
  )

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: '/groups' } })
    }
  }, [isAuthenticated, navigate])

  function openCreate() {
    setModalMode('create')
    setEditingGroup(null)
    setModalOpen(true)
  }

  function openEdit(g: UserCardGroup) {
    setModalMode('edit')
    setEditingGroup(g)
    setModalOpen(true)
  }

  function closeModal() {
    setModalOpen(false)
    setEditingGroup(null)
  }

  if (!isAuthenticated || !user) {
    return null
  }

  return (
    <main className="register-page groups-page">
      <div className="register-page__card groups-page__inner">
        <div className="groups-page__toolbar">
          <div>
            <h1 className="register-page__title groups-page__title-inline">
              Groups
            </h1>
            <p className="register-page__lead groups-page__lead-tight">
              Your card groups.
            </p>
          </div>
          <div className="groups-page__toolbar-actions">
            <Link
              to="/groups/import"
              className="groups-page__import-link"
            >
              Import CSV
            </Link>
            <button
              type="button"
              className="groups-page__add-btn"
              onClick={openCreate}
            >
              Add group
            </button>
          </div>
        </div>

        {error && (
          <div className="register-page__banner" role="alert">
            {error}
          </div>
        )}

        {loading ? (
          <p className="groups-page__status" aria-live="polite">
            Loading groups…
          </p>
        ) : groups.length === 0 && !error ? (
          <p className="groups-page__empty">
            You do not have any groups yet. Use &quot;Add group&quot; to create
            one.
          </p>
        ) : groups.length > 0 ? (
          <>
            <div className="groups-page__search">
              <input
                id="groups-page-search"
                type="search"
                className="groups-page__search-input"
                placeholder="Search…"
                value={groupSearch}
                onChange={(e) => setGroupSearch(e.target.value)}
                autoComplete="off"
                spellCheck={false}
              />
            </div>
            {filteredGroups.length === 0 ? (
              <p className="groups-page__search-empty" role="status">
                No groups match your search.
              </p>
            ) : (
              <ul className="groups-page__list" aria-label="Your card groups">
                {filteredGroups.map((g) => (
                  <li key={g.id} className="groups-page__item">
                    <Link
                      to={`/groups/${g.id}`}
                      state={{ group: g }}
                      className="groups-page__item-main groups-page__item-link"
                    >
                      <div className="groups-page__item-head">
                        <span className="groups-page__item-name">{g.name}</span>
                      </div>
                      <div className="groups-page__item-types">
                        <span className="groups-page__type">
                          {g.frontSideType}
                        </span>
                        <span className="groups-page__type-sep" aria-hidden>
                          /
                        </span>
                        <span className="groups-page__type">
                          {g.backSideType}
                        </span>
                      </div>
                    </Link>
                    <div className="groups-page__item-actions">
                      <span className="groups-page__item-count">
                        {cardLabel(g.cardCount)}
                      </span>
                      <button
                        type="button"
                        className="groups-page__edit-btn"
                        onClick={() => openEdit(g)}
                        aria-label={`Edit group: ${g.name}`}
                      >
                        <span
                          className="groups-page__edit-icon-wrap"
                          aria-hidden
                          dangerouslySetInnerHTML={{ __html: editIconSvg }}
                        />
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </>
        ) : null}

        <Link to="/" className="register-page__back">
          ← Back to home
        </Link>
      </div>

      <GroupFormModal
        open={modalOpen}
        mode={modalMode}
        group={editingGroup}
        allGroups={groups}
        userId={user.id}
        onClose={closeModal}
        onSaved={refetch}
      />
    </main>
  )
}
